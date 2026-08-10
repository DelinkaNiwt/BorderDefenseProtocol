$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'

$comboEntryConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Config\ComboExpressionEntryConfig.cs'
$comboResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs'
$comboResultFactoryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'

$comboEntryConfigText = Get-Content -LiteralPath $comboEntryConfigPath -Raw -Encoding utf8
$comboResolverText = Get-Content -LiteralPath $comboResolverPath -Raw -Encoding utf8
$comboResultFactoryText = if (Test-Path -LiteralPath $comboResultFactoryPath) { Get-Content -LiteralPath $comboResultFactoryPath -Raw -Encoding utf8 } else { '' }

Assert-True (
    ($comboEntryConfigText -match 'List<RangedModuleMountConfig>\s+RangedModules') -and
    ($comboEntryConfigText -match 'RangedModules\s*=\s*RangedModules\s*!=\s*null')
) 'ComboExpressionEntryConfig must expose RangedModules and forward them into the reused chip-expression interpreter input.'

Assert-True (
    ($comboResolverText -match 'RangedModules\s*=\s*entry\.RangedModules') -or
    ($comboResolverText -match 'RangedModules\s*=\s*entry\.RangedModules\s*!=\s*null') -or
    ($comboResultFactoryText -match 'RangedModules\s*=\s*entry\.RangedModules') -or
    ($comboResultFactoryText -match 'RangedModules\s*=\s*entry\.RangedModules\s*!=\s*null')
) 'Combo formal-result assembly must keep carrying combo-entry module mounts into FormalExpressionResult.'

Write-Output 'RangedModuleComboMountSmokeTests PASS'
