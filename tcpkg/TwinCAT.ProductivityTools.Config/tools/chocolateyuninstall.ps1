$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'PlcTemplates.psm1') -Force

Uninstall-PlcTemplate
