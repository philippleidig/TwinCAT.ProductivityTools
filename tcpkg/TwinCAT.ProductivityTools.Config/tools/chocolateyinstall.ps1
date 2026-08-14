$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'PlcTemplates.psm1') -Force

Install-PlcTemplate -SourcePath (Join-Path $PSScriptRoot 'templates')
