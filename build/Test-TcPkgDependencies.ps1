<#
.SYNOPSIS
    Checks the dependency graph of the TcPkg packages for cycles.

.DESCRIPTION
    TcPkg does not build its dependency graph from the nuspec alone. A package whose id ends in the
    name of an engineering environment - TcXaeShell, TcXaeShell64, VS2017, VS2019, VS2022, VS2026 -
    is read as the integration of the package that remains when that suffix is removed, and TcPkg
    adds an edge from the integration to that base package on its own. Measured against TcPkg 2.4.77
    with a local folder feed:

        base -> base.TcXaeShell declared in the nuspec  =>  "Circular dependency detected"
        base with no dependency, workload -> base and
        workload -> base.TcXaeShell                     =>  resolves
        base -> base.Widget (no environment suffix)     =>  resolves
        base.TcXaeShell without a base package          =>  "Unable to resolve dependency"

    That is the bug that made "tcpkg install TwinCAT.ProductivityTools" fail for 1.1.11. A plain XML
    review does not catch it, because the nuspec files themselves are acyclic, so the rule is
    checked here instead.

.PARAMETER Root
    Repository root. Defaults to the parent of the directory this script lives in.

.EXAMPLE
    ./build/Test-TcPkgDependencies.ps1
#>
[CmdletBinding()]
param(
    [string] $Root = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# The environments TcPkg knows. The list is matched case insensitively against the last segment of
# a package id.
$environments = @(
    'TcXaeShell'
    'TcXaeShell64'
    'VS2017'
    'VS2019'
    'VS2022'
    'VS2026'
)

$tcpkgRoot = Join-Path $Root 'tcpkg'

if (-not (Test-Path -LiteralPath $tcpkgRoot)) {
    throw "The directory '$tcpkgRoot' does not exist."
}

$nuspecs = @(Get-ChildItem -LiteralPath $tcpkgRoot -Recurse -Filter '*.nuspec' -File)

if ($nuspecs.Count -eq 0) {
    throw "No nuspec was found below '$tcpkgRoot'."
}

$packages = [ordered]@{}

foreach ($nuspec in $nuspecs) {
    $xml = [xml](Get-Content -LiteralPath $nuspec.FullName -Raw)
    $metadata = $xml.package.metadata
    $id = [string] $metadata.id

    $declared = @(
        $metadata.SelectNodes('*[local-name()="dependencies"]/*[local-name()="dependency"]') |
            ForEach-Object { [string] $_.GetAttribute('id') } |
            Where-Object { $_ }
    )

    $packages[$id] = [pscustomobject]@{
        Id       = $id
        Nuspec   = $nuspec.FullName.Substring($Root.Length + 1)
        Declared = $declared
    }
}

$problems = New-Object System.Collections.Generic.List[string]
$edges = [ordered]@{}

foreach ($package in $packages.Values) {
    $targets = New-Object System.Collections.Generic.List[string]

    foreach ($dependency in $package.Declared) {
        # Dependencies on packages outside this repository - TwinCAT.XAE.Base and friends - are
        # resolved from the Beckhoff feeds and cannot take part in a cycle of our own making.
        if ($packages.Contains($dependency)) {
            $targets.Add($dependency) | Out-Null
        }
        elseif ($dependency -like 'TwinCAT.ProductivityTools*') {
            $problems.Add("$($package.Nuspec): depends on '$dependency', which no nuspec in this repository produces.") | Out-Null
        }
    }

    # The edge TcPkg adds by itself.
    foreach ($environment in $environments) {
        if ($package.Id -like "*.$environment") {
            $base = $package.Id.Substring(0, $package.Id.Length - $environment.Length - 1)

            if (-not $packages.Contains($base)) {
                $problems.Add("$($package.Nuspec): '$($package.Id)' is read by TcPkg as the $environment integration of '$base', but no nuspec in this repository produces that package.") | Out-Null
            }
            elseif (-not $targets.Contains($base)) {
                $targets.Add($base) | Out-Null
            }

            break
        }
    }

    $edges[$package.Id] = $targets
}

# Depth first search. The stack doubles as the path that is reported when an edge closes a loop.
$state = @{}
$path = New-Object System.Collections.Generic.List[string]

function Test-Node {
    param([string] $Id)

    $state[$Id] = 'visiting'
    $path.Add($Id) | Out-Null

    foreach ($target in $edges[$Id]) {
        if (-not $state.ContainsKey($target)) {
            Test-Node -Id $target
        }
        elseif ($state[$target] -eq 'visiting') {
            $start = $path.IndexOf($target)
            $cycle = @($path[$start..($path.Count - 1)]) + $target
            $problems.Add("Circular dependency: $($cycle -join ' => ').") | Out-Null
        }
    }

    $path.RemoveAt($path.Count - 1)
    $state[$Id] = 'done'
}

foreach ($id in $edges.Keys) {
    if (-not $state.ContainsKey($id)) {
        Test-Node -Id $id
    }
}

Write-Host 'TcPkg dependency graph, including the edges TcPkg derives from the package ids:'

foreach ($id in $edges.Keys) {
    $targets = $edges[$id]

    if ($targets.Count -eq 0) {
        Write-Host "  $id"
    }
    else {
        Write-Host "  $id -> $($targets -join ', ')"
    }
}

if ($problems.Count -gt 0) {
    Write-Host ''

    foreach ($problem in $problems) {
        Write-Host "  $problem"
    }

    throw "The TcPkg dependency graph is not valid: $($problems.Count) problem(s) found."
}

Write-Host ''
Write-Host "The graph is acyclic. $($packages.Count) package(s) checked."
