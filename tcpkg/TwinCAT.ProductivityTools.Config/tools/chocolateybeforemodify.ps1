$ErrorActionPreference = 'Stop'

# The template folder is replaced on upgrade. A running engineering environment keeps the template
# directory open, so the copy would fail halfway and leave a broken template behind.
Import-Module (Join-Path $PSScriptRoot 'PlcTemplates.psm1') -Force

Assert-EngineeringNotRunning
