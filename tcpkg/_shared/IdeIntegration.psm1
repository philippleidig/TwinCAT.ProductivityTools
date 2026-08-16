<#
.SYNOPSIS
    Locates the supported engineering environments and deploys the extension into them.

.DESCRIPTION
    The Beckhoff sample this package layout is based on writes the IDE path into the install script
    as a literal, which only ever works for one edition on one machine. The paths are resolved at
    install time instead:

      * Visual Studio through vswhere, which reports every edition including preview channels.
      * TcXaeShell through the registry keys the TwinCAT installer writes, because vswhere does not
        know the shell.

    A missing IDE is not an error. A TcPkg workload installs the integration for all supported
    environments, and a machine is expected to have only some of them.
#>

Set-StrictMode -Version Latest

function Get-VsWherePath {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'),
        (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\Installer\vswhere.exe')
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    return $null
}

<#
.SYNOPSIS
    Returns every Visual Studio installation whose major version matches.
#>
function Get-VisualStudioInstallation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int] $MajorVersion
    )

    $vswhere = Get-VsWherePath

    if (-not $vswhere) {
        Write-Warning 'vswhere.exe was not found, so no Visual Studio installation can be resolved.'
        return @()
    }

    $range = "[$MajorVersion.0,$($MajorVersion + 1).0)"

    $json = & $vswhere -all -prerelease -products * -version $range -format json 2>$null

    if (-not $json) {
        return @()
    }

    $instances = $json | ConvertFrom-Json

    return @(
        $instances |
            Where-Object { $_.installationPath -and (Test-Path -LiteralPath $_.installationPath) } |
            ForEach-Object {
                [pscustomobject]@{
                    Name             = $_.displayName
                    InstallationPath = $_.installationPath
                    Executable       = Join-Path $_.installationPath 'Common7\IDE\devenv.exe'
                }
            }
    )
}

<#
.SYNOPSIS
    Returns the TcXaeShell installation of the requested architecture.

.PARAMETER Architecture
    x86 for the 32 bit shell that ships with TwinCAT 4024, x64 for the shell of TwinCAT 4026.
#>
function Get-TcXaeShellInstallation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('x86', 'x64')]
        [string] $Architecture
    )

    # The 32 bit shell derives from the Visual Studio 2017 shell and registers under 15.0 in the
    # WOW node, the 64 bit shell derives from the 2022 shell and registers under 17.0 natively.
    $key = if ($Architecture -eq 'x86') {
        'HKLM:\SOFTWARE\WOW6432Node\Beckhoff\TcXaeShell\15.0'
    }
    else {
        'HKLM:\SOFTWARE\Beckhoff\TcXaeShell\17.0'
    }

    $entry = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue

    if (-not $entry -or -not $entry.InstallDir) {
        return @()
    }

    $installationPath = $entry.InstallDir.TrimEnd('\')
    $executable = Join-Path $installationPath 'Common7\IDE\TcXaeShell.exe'

    if (-not (Test-Path -LiteralPath $executable)) {
        return @()
    }

    return @(
        [pscustomobject]@{
            Name             = "TcXaeShell $Architecture $($entry.Version)"
            InstallationPath = $installationPath
            Executable       = $executable
        }
    )
}

<#
.SYNOPSIS
    Copies the extension into an IDE and registers it.

.DESCRIPTION
    The extension is deployed as an extension installed per machine, which is the only way to add
    an extension to an IDE from outside the IDE. The folder is replaced rather than merged so that
    a file of a previous version cannot survive an update.
#>
function Install-Extension {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]] $Installation,

        [Parameter(Mandatory = $true)]
        [string] $SourcePath,

        [string] $FolderName = 'TwinCAT.ProductivityTools'
    )

    if ($Installation.Count -eq 0) {
        Write-Output 'No matching engineering environment is installed, nothing to do.'
        return
    }

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        throw "The extension payload was not found at '$SourcePath'."
    }

    foreach ($ide in $Installation) {
        $destination = Join-Path $ide.InstallationPath "Common7\IDE\Extensions\$FolderName"

        if (Test-Path -LiteralPath $destination) {
            Remove-Item -LiteralPath $destination -Recurse -Force
        }

        New-Item -ItemType Directory -Force -Path $destination | Out-Null

        Copy-Item -Path (Join-Path $SourcePath '*') -Destination $destination -Recurse -Force

        Write-Output "Installed into $($ide.Name) at '$destination'."

        Invoke-IdeSetup -Executable $ide.Executable
    }
}

<#
.SYNOPSIS
    Removes the extension from an IDE and updates its extension cache.
#>
function Uninstall-Extension {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]] $Installation,

        [string] $FolderName = 'TwinCAT.ProductivityTools'
    )

    foreach ($ide in $Installation) {
        $destination = Join-Path $ide.InstallationPath "Common7\IDE\Extensions\$FolderName"

        if (-not (Test-Path -LiteralPath $destination)) {
            Write-Output "Nothing to remove from $($ide.Name)."
            continue
        }

        Remove-Item -LiteralPath $destination -Recurse -Force

        Write-Output "Removed from $($ide.Name)."

        Invoke-IdeSetup -Executable $ide.Executable
    }
}

<#
.SYNOPSIS
    Rebuilds the extension and MEF cache of an IDE.

.DESCRIPTION
    Without this the IDE keeps serving the cached command table and either ignores the new
    extension or keeps showing the menu entries of a removed one. A failure is reported but does
    not fail the package, because the cache is rebuilt on the next start anyway.
#>
function Invoke-IdeSetup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Executable
    )

    if (-not (Test-Path -LiteralPath $Executable)) {
        Write-Warning "The IDE executable '$Executable' was not found, so its cache was not updated."
        return
    }

    try {
        $process = Start-Process -FilePath $Executable -ArgumentList '/setup' -Wait -PassThru -WindowStyle Hidden

        if ($process.ExitCode -ne 0) {
            Write-Warning "'$Executable /setup' returned $($process.ExitCode)."
        }
    }
    catch {
        Write-Warning "'$Executable /setup' could not be started: $($_.Exception.Message)"
    }
}

<#
.SYNOPSIS
    Fails when an IDE is running, because its files are locked and its cache would be stale.
#>
function Assert-IdeNotRunning {
    [CmdletBinding()]
    param(
        [string[]] $ProcessName = @('devenv', 'TcXaeShell')
    )

    $running = @(Get-Process -Name $ProcessName -ErrorAction SilentlyContinue)

    if ($running.Count -gt 0) {
        $names = ($running | Select-Object -ExpandProperty ProcessName -Unique) -join ', '

        throw "Close the running engineering environment ($names) before changing this package."
    }
}

Export-ModuleMember -Function `
    Get-VisualStudioInstallation, `
    Get-TcXaeShellInstallation, `
    Install-Extension, `
    Uninstall-Extension, `
    Invoke-IdeSetup, `
    Assert-IdeNotRunning
