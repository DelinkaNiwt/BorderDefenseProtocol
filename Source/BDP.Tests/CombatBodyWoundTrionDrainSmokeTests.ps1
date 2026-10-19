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

$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'
$utilityPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionDrainUtility.cs'
$runtimePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundRuntime.cs'
$trionRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion'
$trionText = (Get-ChildItem -LiteralPath $trionRoot -Filter '*.cs' -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

Assert-True (Test-Path -LiteralPath $bindingPath) 'CombatBodyWoundTrionBinding.cs must exist.'
Assert-True (Test-Path -LiteralPath $utilityPath) 'CombatBodyWoundTrionDrainUtility.cs must exist.'
Assert-True (Test-Path -LiteralPath $runtimePath) 'CombatBodyWoundRuntime.cs must exist.'

$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8
$utilityText = Get-Content -LiteralPath $utilityPath -Raw -Encoding utf8
$runtimeText = Get-Content -LiteralPath $runtimePath -Raw -Encoding utf8

Assert-True ($utilityText -match 'new\s+TrionDrainKey\s*\(\s*"CombatBody"\s*,\s*"Wound"') 'Wound drain key must use CombatBody/Wound domain.'
Assert-True ($bindingText -match 'CombatBodyWoundTrionDrainUtility\.BuildDrainKey') 'Binding must use the shared wound drain key builder.'
Assert-True ($bindingText -match 'CombatBodyWoundTrionDrainUtility\.TryResolveWoundDrainPerSecond') '流失绑定必须读取最终伤口有效损伤计算。'
Assert-True ($utilityText -match 'trionDrainEnabled') 'Drain must honor enabled setting.'
Assert-True ($utilityText -match 'trionDrainPerDamagePerSecond') '部位流失必须使用配置倍率。'
Assert-True ($utilityText -match 'GetPartHealth|HasMissingAncestor') '流失只能读取真实部位损伤与断口。'
Assert-True ($bindingText -match 'RegisterDrain') 'Binding must register drain when enabled.'
Assert-True ($bindingText -match 'UnregisterDrain') 'Binding must unregister drain.'
Assert-True ($runtimeText -match 'RebuildActiveWounds') 'Runtime must support full rebuild.'
Assert-True ($runtimeText -match 'ClearActiveRuntime') 'Runtime must support clear.'
Assert-True ($trionText -notmatch 'BDP\.Core\.CombatBody\.Wounds|\bCombatBodyWound\w+') '资源模块不能引用伤口业务类型；来源显示语言键不属于类型依赖。'

Write-Output 'CombatBodyWoundTrionDrainSmokeTests PASS'
