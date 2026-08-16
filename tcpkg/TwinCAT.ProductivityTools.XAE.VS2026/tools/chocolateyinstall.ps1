$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'IdeIntegration.psm1') -Force

Assert-IdeNotRunning -ProcessName @('devenv')

Install-Extension `
    -Installation @(Get-VisualStudioInstallation -MajorVersion 18) `
    -SourcePath (Join-Path $PSScriptRoot 'extension')