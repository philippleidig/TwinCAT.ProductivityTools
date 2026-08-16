<#
.SYNOPSIS
    Deploys the "Standard PLC Project Optimized Defaults" PLC project template.

.DESCRIPTION
    TwinCAT changed both the location and the layout of the PLC project templates between 4024 and
    4026, so the target is resolved from the installed build:

      4024  C:\TwinCAT\3.1\Components\Plc\PlcTemplates\1.0.0.0\Plc Templates
            One flat folder, one .vsdir describing every template. The template is installed into
            an own sub folder so that no Beckhoff file is ever touched.

      4026  C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\<version>
            One folder per template plus a shared TemplatesDir folder holding one .vsdir per
            template. <version> is the highest installed template package version.

    The payload of the template itself is identical for both, only the .vsdir differs because the
    descriptor sits one directory level deeper under 4026.
#>

Set-StrictMode -Version Latest

$script:TemplateName = 'Standard PLC Project Optimized Defaults'
$script:LegacyFolderName = 'TwinCAT.ProductivityTools.Templates'

<#
.SYNOPSIS
    Returns the installed TwinCAT build number, or 0 when TwinCAT is not installed.
#>
function Get-TwinCatBuild {
    [CmdletBinding()]
    param()

    # TwinCAT is a 32 bit product, so its keys live in the WOW node on a 64 bit machine.
    foreach ($key in @(
            'HKLM:\SOFTWARE\WOW6432Node\Beckhoff\TwinCAT3\System',
            'HKLM:\SOFTWARE\Beckhoff\TwinCAT3\System')) {
        $entry = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue

        if ($entry -and $entry.PSObject.Properties['Build'] -and $entry.Build) {
            return [int] $entry.Build
        }
    }

    return 0
}

<#
.SYNOPSIS
    Returns the template root directory of the installed TwinCAT version.
#>
function Get-PlcTemplateRoot {
    [CmdletBinding()]
    param()

    if ((Get-TwinCatBuild) -ge 4026) {
        $base = Join-Path $env:ProgramData 'Beckhoff\TwinCAT\PlcEngineering\PlcTemplates'

        if (-not (Test-Path -LiteralPath $base)) {
            return $null
        }

        # Several template package versions can be installed side by side. The engineering only
        # reads the highest one, so that is the one to write into.
        $newest = Get-ChildItem -LiteralPath $base -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -as [version] } |
            Sort-Object { [version] $_.Name } |
            Select-Object -Last 1

        if (-not $newest) {
            return $null
        }

        return $newest.FullName
    }

    $legacy = 'C:\TwinCAT\3.1\Components\Plc\PlcTemplates\1.0.0.0\Plc Templates'

    if (Test-Path -LiteralPath $legacy) {
        return $legacy
    }

    return $null
}

<#
.SYNOPSIS
    Copies the template payload and the matching .vsdir into the template root.
#>
function Install-PlcTemplate {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $SourcePath
    )

    Assert-EngineeringNotRunning

    $root = Get-PlcTemplateRoot

    if (-not $root) {
        Write-Warning 'No PLC template directory was found, so the template was not installed.'
        return
    }

    $payload = Join-Path $SourcePath $script:TemplateName

    if (-not (Test-Path -LiteralPath $payload)) {
        throw "The template payload was not found at '$payload'."
    }

    if ((Get-TwinCatBuild) -ge 4026) {
        $templateDestination = Join-Path $root $script:TemplateName
        $descriptorDestination = Join-Path $root 'TemplatesDir'
        $descriptor = Join-Path $SourcePath "vsdir\4026\$($script:TemplateName).vsdir"
    }
    else {
        $container = Join-Path $root $script:LegacyFolderName
        $templateDestination = Join-Path $container $script:TemplateName
        $descriptorDestination = $container
        $descriptor = Join-Path $SourcePath "vsdir\4024\$($script:LegacyFolderName).vsdir"
    }

    if (Test-Path -LiteralPath $templateDestination) {
        Remove-Item -LiteralPath $templateDestination -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $templateDestination | Out-Null
    New-Item -ItemType Directory -Force -Path $descriptorDestination | Out-Null

    Copy-Item -Path (Join-Path $payload '*') -Destination $templateDestination -Recurse -Force
    Copy-Item -LiteralPath $descriptor -Destination $descriptorDestination -Force

    Write-Output "Installed the PLC template into '$templateDestination'."
}

<#
.SYNOPSIS
    Removes the template again, leaving the Beckhoff templates untouched.
#>
function Uninstall-PlcTemplate {
    [CmdletBinding()]
    param()

    Assert-EngineeringNotRunning

    $root = Get-PlcTemplateRoot

    if (-not $root) {
        Write-Output 'No PLC template directory was found, nothing to remove.'
        return
    }

    $paths = @(
        (Join-Path $root $script:TemplateName),
        (Join-Path $root "TemplatesDir\$($script:TemplateName).vsdir"),
        (Join-Path $root $script:LegacyFolderName)
    )

    foreach ($path in $paths) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
            Write-Output "Removed '$path'."
        }
    }
}

<#
.SYNOPSIS
    Fails when an engineering environment is running and would keep the template directory open.
#>
function Assert-EngineeringNotRunning {
    [CmdletBinding()]
    param()

    $running = @(Get-Process -Name 'devenv', 'TcXaeShell' -ErrorAction SilentlyContinue)

    if ($running.Count -gt 0) {
        $names = ($running | Select-Object -ExpandProperty ProcessName -Unique) -join ', '

        throw "Close the running engineering environment ($names) before changing this package."
    }
}

Export-ModuleMember -Function `
    Get-TwinCatBuild, `
    Get-PlcTemplateRoot, `
    Install-PlcTemplate, `
    Uninstall-PlcTemplate, `
    Assert-EngineeringNotRunning
