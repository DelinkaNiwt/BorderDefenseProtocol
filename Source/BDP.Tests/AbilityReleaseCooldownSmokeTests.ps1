$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$abilityPath = Join-Path $repoRoot '1.6\Content\Defs\AbilityDef\Chameleon.xml'
$hediffPath = Join-Path $repoRoot '1.6\Content\Defs\HediffDef\Chameleon.xml'

$cooldownPropsPath = Join-Path $coreRoot 'Abilities\CompProperties_AbilityReleaseCooldown.cs'
$cooldownEffectPath = Join-Path $coreRoot 'Abilities\CompAbilityEffect_AbilityReleaseCooldown.cs'
$cooldownLedgerPath = Join-Path $coreRoot 'Abilities\GameComponent_AbilityReleaseCooldownLedger.cs'
$cooldownServicePath = Join-Path $coreRoot 'Abilities\AbilityReleaseCooldownService.cs'
$cooldownPatchPath = Join-Path $coreRoot 'Abilities\Patch_Ability_CooldownTicksTotal_BdpReleaseCooldown.cs'
$applyPropsPath = Join-Path $coreRoot 'Abilities\CompProperties_AbilityEffect_ApplyExpressionHediff.cs'
$applyEffectPath = Join-Path $coreRoot 'Abilities\CompAbilityEffect_ApplyExpressionHediff.cs'
$abilitySyncPath = Join-Path $coreRoot 'Expressions\Projection\DefaultExpressionAbilityHostSynchronizer.cs'
$hostHediffPath = Join-Path $coreRoot 'Expressions\Projection\BdpExpressionHostHediff.cs'
$expressionServicePath = Join-Path $coreRoot 'Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'

foreach ($path in @(
    $cooldownPropsPath,
    $cooldownEffectPath,
    $cooldownLedgerPath,
    $cooldownServicePath,
    $cooldownPatchPath,
    $applyPropsPath,
    $applyEffectPath,
    $abilitySyncPath,
    $hostHediffPath,
    $expressionServicePath,
    $abilityPath,
    $hediffPath
)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少能力释放冷却实现文件：$path"
}

$cooldownPropsText = Get-Content -LiteralPath $cooldownPropsPath -Raw -Encoding utf8
$cooldownEffectText = Get-Content -LiteralPath $cooldownEffectPath -Raw -Encoding utf8
$cooldownLedgerText = Get-Content -LiteralPath $cooldownLedgerPath -Raw -Encoding utf8
$cooldownServiceText = Get-Content -LiteralPath $cooldownServicePath -Raw -Encoding utf8
$applyPropsText = Get-Content -LiteralPath $applyPropsPath -Raw -Encoding utf8
$applyEffectText = Get-Content -LiteralPath $applyEffectPath -Raw -Encoding utf8
$abilitySyncText = Get-Content -LiteralPath $abilitySyncPath -Raw -Encoding utf8
$hostHediffText = Get-Content -LiteralPath $hostHediffPath -Raw -Encoding utf8
$expressionServiceText = Get-Content -LiteralPath $expressionServicePath -Raw -Encoding utf8
$abilityText = Get-Content -LiteralPath $abilityPath -Raw -Encoding utf8
$hediffText = Get-Content -LiteralPath $hediffPath -Raw -Encoding utf8

# Core 只提供配置驱动的来源级冷却设施，不出现变色龙业务名或固定业务数值。
Assert-True ($cooldownPropsText -match 'cooldownKey') `
    '通用释放冷却配置必须由 AbilityDef 提供 cooldownKey。'
Assert-True ($cooldownLedgerText -match 'GameComponent') `
    '来源级释放冷却账本必须挂在原版 GameComponent 生命周期上。'
Assert-True (($cooldownLedgerText -match 'GetRemaining') -and ($cooldownLedgerText -match 'StartCooldown')) `
    '来源级释放冷却账本必须提供读取剩余时间和启动冷却的通用入口。'
Assert-True ($cooldownLedgerText -match 'CooldownDurationTicks') `
    '来源级释放冷却账本必须保存释放时的实际总冷却时长。'
Assert-True ($cooldownLedgerText -match 'Scribe_') `
    '来源级释放冷却账本必须保留跨存档的时间状态。'
Assert-True ($cooldownServiceText -match 'CooldownTicksRemaining') `
    '通用释放冷却组件必须复用原版 Ability 实际冷却时长，而不是复制业务数值。'
Assert-True ($cooldownServiceText -match 'CooldownTicksTotal') `
    '通用释放冷却登记必须读取原版 Ability 的实际总冷却时长。'
Assert-True ($cooldownEffectText -match 'CanCast') `
    '通用释放冷却组件必须在来源级冷却仍存在时阻止再次释放。'
Assert-True (($cooldownPatchText = Get-Content -LiteralPath $cooldownPatchPath -Raw -Encoding utf8) -match 'CooldownTicksTotal') `
    '通用冷却必须为 Ability 进度条提供账本中的原始总时长。'
Assert-True ($cooldownPatchText -match 'AbilityReleaseCooldownService') `
    'Ability 进度条总时长覆盖必须通过通用冷却服务解析，而不是引用具体业务。'

# Hediff 应用也必须是通用 Core 组件；变色龙只负责填目标 Def。
Assert-True ($applyPropsText -match 'hediffDefName') `
    '通用表达 Hediff 应用组件必须从 AbilityDef 读取目标 HediffDefName。'
Assert-True ($applyEffectText -match 'AddHediff') `
    '通用表达 Hediff 应用组件必须通过原版 Pawn Health 入口添加 Hediff。'
Assert-True ($applyEffectText -match 'SynchronizePublishedExpressionHostsImmediately') `
    '通用表达 Hediff 应用组件必须在实际添加后触发表达宿主与持续消耗对账。'

# Ability 被表达宿主移除后重新补入时，必须恢复来源级冷却。
Assert-True ($abilitySyncText -match 'RestoreAbilityCooldown') `
    'Ability 宿主同步器必须在 Ability 重新挂载时恢复来源级冷却。'
Assert-True ($hostHediffText -match 'PostRemoved') `
    '表达宿主 Hediff 必须在原版移除完成后触发统一生命周期通知。'
Assert-True ($expressionServiceText -match 'synchronizingProjectedHosts') `
    '表达宿主同步必须防止 Hediff 移除回调造成递归同步。'

# 变色龙只声明业务参数，不再使用“关闭后追加冷却”的切换语义。
Assert-True ($abilityText -match '<cooldownTicksRange>2400</cooldownTicksRange>') `
    '变色龙 Ability 必须使用原版 40 秒冷却。'
Assert-True ($abilityText -match 'CompProperties_AbilityEffect_ApplyExpressionHediff') `
    '变色龙必须使用一次性应用表达 Hediff 的通用组件。'
Assert-True ($abilityText -match 'CompProperties_AbilityReleaseCooldown') `
    '变色龙必须声明通用来源级释放冷却组件。'
Assert-True ($abilityText -match '<cooldownKey>BDP_AbilityCooldown_Chameleon</cooldownKey>') `
    '变色龙必须通过配置声明自己的共享冷却键。'
Assert-True ($abilityText -notmatch 'cooldownAfterHediffRemovedTicks') `
    '变色龙不得继续使用 Hediff 移除后追加冷却的旧语义。'
Assert-True ($hediffText -match 'HediffCompProperties_Disappears') `
    '变色龙 Hediff 必须使用原版自动消失组件。'
Assert-True ($hediffText -match '<disappearsAfterTicks>1200</disappearsAfterTicks>') `
    '变色龙 Hediff 必须配置 20 秒有效持续时间。'

Write-Output 'AbilityReleaseCooldownSmokeTests PASS'
