$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'IdeIntegration.psm1') -Force

Assert-IdeNotRunning -ProcessName @('TcXaeShell')

Install-Extension `
    -Installation @(Get-TcXaeShellInstallation -Architecture 'x64') `
    -SourcePath (Join-Path $PSScriptRoot 'extension')