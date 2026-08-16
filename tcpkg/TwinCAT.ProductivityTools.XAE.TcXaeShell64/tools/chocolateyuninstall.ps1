$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'IdeIntegration.psm1') -Force

Assert-IdeNotRunning -ProcessName @('TcXaeShell')

Uninstall-Extension -Installation @(Get-TcXaeShellInstallation -Architecture 'x64')