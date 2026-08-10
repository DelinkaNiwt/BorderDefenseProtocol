$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

$defPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedModules\Config\BdpRangedAttackModuleDef.cs'
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedModules\Runtime\RangedAttackModuleResolver.cs'

$defText = Get-Content -LiteralPath $defPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8

Assert-True (
    $defText -match 'override\s+IEnumerable<string>\s+ConfigErrors\(\)'
) 'BdpRangedAttackModuleDef must validate runtimeClass at Def load time.'

Assert-True (
    $defText -match 'runtimeClass\s*==\s*null'
) 'BdpRangedAttackModuleDef must report missing runtimeClass.'

Assert-True (
    $defText -match 'IRangedAttackModuleRuntime'
) 'BdpRangedAttackModuleDef must require runtimeClass to implement IRangedAttackModuleRuntime.'

Assert-True (
    $defText -match 'GetConstructor\(Type\.EmptyTypes\)'
) 'BdpRangedAttackModuleDef must require a public parameterless constructor.'

Assert-True (
    ($resolverText -match 'try\s*\{') -and
    ($resolverText -match 'catch\s*\(Exception ex\)')
) 'RangedAttackModuleResolver must isolate runtime create/initialize exceptions.'

Assert-True (
    ($resolverText -match 'BdpDiagnostics\.Once') -and
    ($resolverText -match 'continue;')
) 'RangedAttackModuleResolver must log once and continue when a module runtime fails.'

Write-Output 'RangedModuleRuntimeBoundary PASS'
