$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'IdeIntegration.psm1') -Force

Assert-IdeNotRunning -ProcessName @('devenv')

Uninstall-Extension -Installation @(Get-VisualStudioInstallation -MajorVersion 18)