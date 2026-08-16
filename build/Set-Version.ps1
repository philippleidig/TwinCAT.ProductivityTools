<#
.SYNOPSIS
    Writes a version into every file that carries one.

.DESCRIPTION
    The repository does not keep a version under source control. The release pipeline determines it
    from the commit history with GitVersion and stamps it right before the build, so that the VSIX
    manifests, the TcPkg packages and the installer all report exactly the same number.

.PARAMETER Version
    Four part version, for example 2.1.0.0. A shorter version is padded with trailing zeros,
    because a VSIX manifest rejects anything else.

.PARAMETER Root
    Repository root. Defaults to the parent of the directory this script lives in.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Version,

    [string] $Root = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

function Format-Version {
    param([string] $Value)

    $parts = @($Value.Split('.') | Select-Object -First 4)

    while ($parts.Count -lt 4) {
        $parts += '0'
    }

    foreach ($part in $parts) {
        if ($part -notmatch '^\d+$') {
            throw "The version '$Value' is not a numeric version."
        }
    }

    return ($parts -join '.')
}

$normalized = Format-Version -Value $Version
$threePart = ($normalized.Split('.') | Select-Object -First 3) -join '.'

Write-Host "Stamping version $normalized"

$manifests = @(Get-ChildItem -Path (Join-Path $Root 'src') -Filter 'source.extension.vsixmanifest' -Recurse -File)

if ($manifests.Count -eq 0) {
    throw "No VSIX manifest was found below $Root\src."
}

foreach ($manifest in $manifests) {
    $content = Get-Content -LiteralPath $manifest.FullName -Raw

    # The attribute is replaced in place rather than through an XML round trip. Saving the parsed
    # document reindents the whole manifest and turns empty elements into elements holding a line
    # break, which produces a large and pointless diff on every release.
    $updated = [regex]::Replace(
        $content,
        '(?<prefix><Identity\b[^>]*?\bVersion=")(?<version>[^"]*)(?<suffix>")',
        { param($match) $match.Groups['prefix'].Value + $normalized + $match.Groups['suffix'].Value },
        1
    )

    if ($updated -eq $content) {
        throw "The manifest $($manifest.FullName) has no Identity element with a Version attribute."
    }

    [System.IO.File]::WriteAllText($manifest.FullName, $updated, (New-Object System.Text.UTF8Encoding $true))

    Write-Host "  $($manifest.FullName.Substring($Root.Length + 1))"
}

$nuspecs = @(
    Get-ChildItem -Path $Root -Filter '*.nuspec' -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|packages)\\' }
)

foreach ($nuspec in $nuspecs) {
    $xml = [xml](Get-Content -LiteralPath $nuspec.FullName -Raw)
    $metadata = $xml.package.metadata

    if (-not $metadata) {
        continue
    }

    # TcPkg compares three part versions. A fourth part would make the feed treat the package as a
    # different, non comparable release.
    $metadata.version = $threePart

    foreach ($dependency in @($metadata.dependencies.dependency)) {
        if ($dependency -and $dependency.id -like 'TwinCAT.ProductivityTools*') {
            $dependency.version = "[$threePart]"
        }
    }

    $xml.Save($nuspec.FullName)

    Write-Host "  $($nuspec.FullName.Substring($Root.Length + 1))"
}

if ($env:GITHUB_ENV) {
    "PRODUCTIVITYTOOLS_VERSION=$normalized" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
}

if ($env:GITHUB_OUTPUT) {
    "version=$normalized" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
}
