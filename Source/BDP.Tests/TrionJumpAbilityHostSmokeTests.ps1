$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$coreRoot = Join-Path $sourceRoot 'BDP\Core'
$contentRoot = Join-Path $sourceRoot 'BDP.Content'
$markerPath = Join-Path $coreRoot 'Abilities\IBdpExpressionAbilityVerb.cs'
$committerPath = Join-Path $coreRoot 'Abilities\BdpAbilityTrionCostCommitter.cs'
$abilityVerbPath = Join-Path $coreRoot 'Abilities\BdpVerb_CastAbility.cs'
$jumpVerbPath = Join-Path $coreRoot 'Abilities\BdpVerb_CastAbilityJump.cs'
$synchronizerPath = Join-Path $coreRoot 'Expressions\Projection\DefaultExpressionAbilityHostSynchronizer.cs'
$shortJumpPath = Join-Path $contentRoot 'CombatBody\ShortJump\Verb_CastAbilityCombatBodyShortJump.cs'

foreach ($path in @($markerPath, $committerPath, $abilityVerbPath, $jumpVerbPath, $synchronizerPath, $shortJumpPath)) {
    Assert-True (Test-Path -LiteralPath $path) ('跳跃能力扣费扩展缺少文件：' + $path)
}

$markerText = Get-Content -LiteralPath $markerPath -Raw -Encoding UTF8
$committerText = Get-Content -LiteralPath $committerPath -Raw -Encoding UTF8
$abilityVerbText = Get-Content -LiteralPath $abilityVerbPath -Raw -Encoding UTF8
$jumpVerbText = Get-Content -LiteralPath $jumpVerbPath -Raw -Encoding UTF8
$synchronizerText = Get-Content -LiteralPath $synchronizerPath -Raw -Encoding UTF8
$shortJumpText = Get-Content -LiteralPath $shortJumpPath -Raw -Encoding UTF8

Assert-True ($markerText -match 'interface\s+IBdpExpressionAbilityVerb') `
    '表达 Ability（能力）宿主必须使用中性标记接口，而不是绑定单一继承树。'

Assert-True (
    ($committerText -match 'TryCommit\(Ability ability\)') -and
    ($committerText -match 'CompAbilityEffect_BdpTrionCost') -and
    ($committerText -match 'TryCommitCastCost')
) '通用扣费提交器必须遍历现有 BDP Trion 成本组件。'

Assert-True (
    ($abilityVerbText -match 'BdpVerb_CastAbility\s*:\s*Verb_CastAbility,\s*IBdpExpressionAbilityVerb') -and
    ($abilityVerbText -match 'BdpAbilityTrionCostCommitter\.TryCommit\(ability\)')
) '现有普通能力动词必须改用通用标记和扣费提交器。'

Assert-True (
    ($jumpVerbText -match 'BdpVerb_CastAbilityJump\s*:\s*Verb_CastAbilityJump,\s*IBdpExpressionAbilityVerb') -and
    ($jumpVerbText -match 'protected\s+override\s+bool\s+TryCastShot\(\)') -and
    ($jumpVerbText -match 'BdpAbilityTrionCostCommitter\.TryCommit\(ability\)') -and
    ($jumpVerbText -match 'return\s+base\.TryCastShot\(\)')
) '跳跃分支必须只在原版跳跃执行前提交费用，然后返回原版流程。'

Assert-True (
    ($synchronizerText -match 'IBdpExpressionAbilityVerb') -and
    ($synchronizerText -notmatch 'typeof\(BDP\.Core\.Abilities\.BdpVerb_CastAbility\)\.IsAssignableFrom')
) '表达宿主识别必须同时支持普通与跳跃能力继承树。'

Assert-True (
    ($shortJumpText -match 'Verb_CastAbilityCombatBodyShortJump\s*:\s*BdpVerb_CastAbilityJump') -and
    ($shortJumpText -match 'JumpFlyerDef')
) '现有短距跳跃必须继承新扣费扩展点，同时继续只替换专用飞行器。'

Write-Output 'TrionJumpAbilityHostSmokeTests PASS'
