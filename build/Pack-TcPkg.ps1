<#
.SYNOPSIS
    Packs the TcPkg packages of the TwinCAT Productivity Tools.

.DESCRIPTION
    A TcPkg package is a NuGet package, so nuget pack is all that is needed. The only preparation
    step is unpacking the built VSIX files, because an IDE extension that is deployed from outside
    the IDE has to be copied as a plain folder, not as a VSIX.

    The extension payload is staged under artifacts/vsix/<architecture>, which is the path the
    nuspec files reference.

.PARAMETER Version
    Three part version stamped into every package.

.PARAMETER Configuration
    Build configuration the VSIX files were built in.

.PARAMETER OutputPath
    Directory that receives the .nupkg files.

.EXAMPLE
    ./build/Pack-TcPkg.ps1 -Version 2.1.0
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,

    [string] $Configuration = 'Release',

    [string] $OutputPath = 'artifacts/tcpkg'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$tcpkgRoot = Join-Path $repositoryRoot 'tcpkg'
$stagingRoot = Join-Path $repositoryRoot 'artifacts/vsix'

# The 32 bit shell is served by the .15 project, everything else by the .17 project.
$vsixSources = @(
    [pscustomobject]@{
        Architecture = 'x86'
        Path         = "src/TwinCAT.ProductivityTools.15/bin/$Configuration/TwinCAT.ProductivityTools.15.vsix"
    },
    [pscustomobject]@{
        Architecture = 'x64'
        Path         = "src/TwinCAT.ProductivityTools.17/bin/$Configuration/TwinCAT.ProductivityTools.17.vsix"
    }
)

function Resolve-NuGet {
    $command = Get-Command 'nuget' -ErrorAction SilentlyContinue

    if ($command) {
        return $command.Source
    }

    throw 'nuget.exe was not found on PATH. Install it or use the NuGet/setup-nuget action.'
}

function Expand-Vsix {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null

    # A VSIX is a zip, but Expand-Archive only accepts the .zip extension.
    $temporary = Join-Path ([System.IO.Path]::GetTempPath()) "$([guid]::NewGuid()).zip"

    try {
        Copy-Item -LiteralPath $Path -Destination $temporary -Force
        Expand-Archive -LiteralPath $temporary -DestinationPath $Destination -Force
    }
    finally {
        Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue
    }

    # The OPC parts of the package format are meaningless for a folder deployment and confuse the
    # extension scanner of the IDE.
    foreach ($noise in @('[Content_Types].xml', '_rels', 'package')) {
        $path = Join-Path $Destination $noise

        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

Push-Location $repositoryRoot

try {
    foreach ($source in $vsixSources) {
        $vsix = Join-Path $repositoryRoot $source.Path

        if (-not (Test-Path -LiteralPath $vsix)) {
            throw "The VSIX '$($source.Path)' was not found. Build the solution in $Configuration first."
        }

        $destination = Join-Path $stagingRoot $source.Architecture

        Expand-Vsix -Path $vsix -Destination $destination

        Write-Host "Staged $($source.Architecture) extension from '$($source.Path)'."
    }

    $nuget = Resolve-NuGet
    $output = Join-Path $repositoryRoot $OutputPath

    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $output | Out-Null

    $nuspecs = Get-ChildItem -LiteralPath $tcpkgRoot -Recurse -Filter '*.nuspec' | Sort-Object FullName

    if ($nuspecs.Count -eq 0) {
        throw "No nuspec was found below '$tcpkgRoot'."
    }

    foreach ($nuspec in $nuspecs) {
        Write-Host "Packing $($nuspec.BaseName) $Version"

        # Set-Version.ps1 already wrote the exact version and pinned the dependency ranges. The
        # switch below only matters when the script is run on its own for a local test.
        & $nuget pack $nuspec.FullName `
            -Version $Version `
            -OutputDirectory $output `
            -NoDefaultExcludes `
            -NonInteractive

        if ($LASTEXITCODE -ne 0) {
            throw "nuget pack failed for '$($nuspec.Name)' with exit code $LASTEXITCODE."
        }
    }

    Write-Host ''
    Get-ChildItem -LiteralPath $output -Filter '*.nupkg' | Format-Table Name, Length
}
finally {
    Pop-Location
}
