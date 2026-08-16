$ErrorActionPreference = 'Stop'

# An upgrade replaces the extension folder in place, which fails while the IDE holds its files.
Import-Module (Join-Path $PSScriptRoot 'IdeIntegration.psm1') -Force

Assert-IdeNotRunning -ProcessName @('TcXaeShell')