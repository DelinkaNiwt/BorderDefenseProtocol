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
$modRoot = Split-Path -Parent $sourceRoot
$oldCompPath = Join-Path $sourceRoot 'BDP\Core\Hediffs\HediffComp_CombatBodyScan.cs'
$oldPropsPath = Join-Path $sourceRoot 'BDP\Core\Hediffs\HediffCompProperties_CombatBodyScan.cs'
$oldInterfacePath = Join-Path $sourceRoot 'BDP\Core\Hediffs\ICombatBodyScanMote.cs'
$oldApparelCapturePath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyApparelCapture.cs'
$oldApparelRecordPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyApparelDrawRecord.cs'
$oldApparelSuppressionPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\CombatBodyApparelRenderSuppression.cs'
$oldApparelPatchPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Transform\Patch_PawnRenderNodeWorker_Apparel_CombatBodyScan.cs'
$combatBodyDefPath = Join-Path $modRoot '1.6\Defs\HediffDef\CombatBody.xml'
$coreRoot = Join-Path $sourceRoot 'BDP'

Assert-True -Condition (-not (Test-Path -LiteralPath $oldCompPath)) -Message '旧 HediffComp 扫描触发器仍存在。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldPropsPath)) -Message '旧 HediffComp 扫描属性仍存在。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldInterfacePath)) -Message '旧 Core Mote 配置接口仍存在。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldApparelCapturePath)) -Message '旧衣物捕获器仍存在。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldApparelRecordPath)) -Message '旧衣物绘制记录仍存在。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldApparelSuppressionPath)) -Message '旧衣物抑制状态仍存在。'
Assert-True -Condition (-not (Test-Path -LiteralPath $oldApparelPatchPath)) -Message '旧衣物节点补丁仍存在。'

$combatBodyDefText = Get-Content -LiteralPath $combatBodyDefPath -Raw -Encoding utf8
$coreText = (Get-ChildItem -LiteralPath $coreRoot -Recurse -Filter '*.cs' | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

Assert-True -Condition ($combatBodyDefText -notmatch 'HediffCompProperties_CombatBodyScan|scanDurationTicks|scanBandThickness|trailAlpha') -Message '战斗体 Hediff Def 仍引用旧扫描组件。'
Assert-True -Condition ($coreText -notmatch 'BDP_Mote_CombatBodyScan|PlayFlashEffect|ICombatBodyScanMote') -Message 'Core 仍包含具体扫描表现知识。'
Assert-True -Condition ($combatBodyDefText -match '<defName>BDP_CombatBodyActive</defName>') -Message '战斗体激活态 Hediff 不得被误删。'

Write-Output 'CombatBodyTransformScanRetirement PASS'
