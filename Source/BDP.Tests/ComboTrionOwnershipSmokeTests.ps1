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
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$devHarnessDefsRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6\Defs'
$mainContentDefsRoot = Join-Path $repoRoot '1.6\Content\Defs'

$comboDefPath = Join-Path $coreRoot 'Combos\Defs\ComboDef.cs'
$comboConfigPath = Join-Path $coreRoot 'Combos\Config\ComboDefinitionConfig.cs'
$comboContractPath = Join-Path $coreRoot 'Combos\Contract\ComboDefinitionContract.cs'
$comboResolverPath = Join-Path $coreRoot 'Combos\Contract\ComboDefinitionContractResolver.cs'
$comboTrionConfigPath = Join-Path $coreRoot 'Combos\Config\ComboTrionConfig.cs'
$comboTrionContractPath = Join-Path $coreRoot 'Combos\Contract\ComboTrionContract.cs'
$comboExpressionConfigPath = Join-Path $coreRoot 'Combos\Config\ComboExpressionConfig.cs'
$comboFactoryPath = Join-Path $coreRoot 'Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'
$comboDefsPath = Join-Path $devHarnessDefsRoot 'Pawn\Combos\Test\ComboDefs_TestCombos.xml'
$senkuComboDefsPath = Join-Path $mainContentDefsRoot 'Pawn\Combos\SenkuKogetsu\ComboDefs_SenkuKogetsu.xml'

$comboDefText = Get-Content -LiteralPath $comboDefPath -Raw -Encoding utf8
$comboConfigText = Get-Content -LiteralPath $comboConfigPath -Raw -Encoding utf8
$comboContractText = Get-Content -LiteralPath $comboContractPath -Raw -Encoding utf8
$comboResolverText = Get-Content -LiteralPath $comboResolverPath -Raw -Encoding utf8
$comboExpressionConfigText = Get-Content -LiteralPath $comboExpressionConfigPath -Raw -Encoding utf8
$comboFactoryText = Get-Content -LiteralPath $comboFactoryPath -Raw -Encoding utf8

# Combo 只是两枚实体芯片共同产生的表达结果，不得伪装成第三枚实体芯片再次占容量或收激活费。
Assert-True (
    (-not (Test-Path -LiteralPath $comboTrionConfigPath)) -and
    (-not (Test-Path -LiteralPath $comboTrionContractPath))
) 'Combo top-level Trion config/contract files must be removed.'

Assert-True (
    ($comboDefText -notmatch 'ComboTrionConfig\s+Trion') -and
    ($comboConfigText -notmatch 'ComboTrionConfig\s+Trion') -and
    ($comboContractText -notmatch 'ComboTrionContract\s+Trion') -and
    ($comboResolverText -notmatch '\bResolveTrion\s*\(')
) 'Combo definition, config, contract, and resolver must not retain a top-level Trion pipeline.'

# Combo 费用只能由具体表达条目显式声明或显式求值。
Assert-True (
    ($comboExpressionConfigText -match 'UseCostResolve') -and
    ($comboExpressionConfigText -match 'MinimumRequiredResolve')
) 'Combo expression entries must retain explicit UseCost/MinimumRequired resolution.'

Assert-True (
    ($comboFactoryText -match 'entry\s*!=\s*null\s*&&\s*entry\.Trion\s*!=\s*null') -and
    ($comboFactoryText -match 'ResolveTrionFromSourceMaterials') -and
    ($comboFactoryText -notmatch 'return\s+CloneTrion\(mainSourceMaterial\.Trion\)') -and
    ($comboFactoryText -notmatch 'return\s+CloneTrion\(subSourceMaterial\.Trion\)') -and
    ($comboFactoryText -notmatch 'return\s+CloneTrion\(mainSourceResult\.Trion\)') -and
    ($comboFactoryText -notmatch 'return\s+subSourceResult\s*!=\s*null\s*\?\s*CloneTrion\(subSourceResult\.Trion\)')
) 'Combo results without Trion/TrionResolve must be free instead of silently inheriting source costs.'

# 候选样例不得继续写无效顶层费用；正式旋空弧月业务使用条目费用求值规则。
[xml]$comboDefs = Get-Content -LiteralPath $comboDefsPath -Raw -Encoding utf8
[xml]$senkuComboDefs = Get-Content -LiteralPath $senkuComboDefsPath -Raw -Encoding utf8
$comboNodes = @($comboDefs.SelectNodes('//BDP.Core.Combos.ComboDef'))
$senkuComboNodes = @($senkuComboDefs.SelectNodes('//BDP.Core.Combos.ComboDef'))
$allComboNodes = @($comboNodes + $senkuComboNodes)

Assert-True (
    @($allComboNodes | Where-Object { $null -ne $_.SelectSingleNode('./Trion') }).Count -eq 0
) '候选与正式 ComboDefs 都不得声明顶层 Trion 块。'

Assert-True (
    @($allComboNodes | Where-Object { $null -ne $_.SelectSingleNode('./Expression/Entries/li/TrionResolve') }).Count -ge 1
) '候选与正式 ComboDefs 必须保留至少一个显式 combo-entry TrionResolve 示例。'

Write-Output 'ComboTrionOwnershipSmokeTests PASS'
