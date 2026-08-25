$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# 当前主模组根目录。
$modRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$shieldRoot = Join-Path $modRoot 'Source\BDP.Content\Shield'
$policyPath = Join-Path $shieldRoot 'EnergyShieldBlockPolicy.cs'
$shieldPath = Join-Path $shieldRoot 'HediffComp_EnergyShield.cs'
$visualFeedbackPath = Join-Path $modRoot 'Source\BDP\Core\Trigger\Access\Surfaces\ExpressionVisualFeedbackAccess.cs'
$visualImpulsePath = Join-Path $modRoot 'Source\BDP\Core\Trigger\Runtime\ExpressionVisualImpulse.cs'
$visualOwnerPath = Join-Path $modRoot 'Source\BDP\Core\Trigger\Runtime\TriggerVisualRuntimeStateOwner.cs'
$drawPatchPath = Join-Path $modRoot 'Source\BDP\Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$lightSoulPath = Join-Path $modRoot '1.6\Content\Defs\HediffDef\LightSoul.xml'

# 断言一个条件必须成立。
function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

foreach ($path in @(
    $policyPath,
    $shieldPath,
    $visualFeedbackPath,
    $visualImpulsePath,
    $visualOwnerPath,
    $drawPatchPath,
    $lightSoulPath
)) {
    Assert-True (Test-Path -LiteralPath $path) ('光魂抵挡反馈缺少文件：' + $path)
}

$policyText = Get-Content -Raw -Encoding UTF8 $policyPath
$shieldText = Get-Content -Raw -Encoding UTF8 $shieldPath
$feedbackText = Get-Content -Raw -Encoding UTF8 $visualFeedbackPath
$impulseText = Get-Content -Raw -Encoding UTF8 $visualImpulsePath
$ownerText = Get-Content -Raw -Encoding UTF8 $visualOwnerPath
$drawText = Get-Content -Raw -Encoding UTF8 $drawPatchPath

# 方向真值：近战必须从攻击者实际位置恢复，且一次解析同时供判定和表现使用。
Assert-True (
    ($policyText -match 'ResolveAttackTravelAngle') -and
    ($policyText -match 'damageInfo\.Instigator') -and
    ($policyText -match 'IsMeleeDamage') -and
    ($shieldText -match 'float\s+attackTravelAngle\s*=') -and
    ($shieldText -match 'CheckAngle\(attackTravelAngle\)') -and
    ($shieldText -match 'PlayBlockEffect\(damageInfo,\s*attackTravelAngle\)')
) '护盾必须只解析一次攻击行进角，并让角度判定与命中表现共用。'

# 原版火花：必须给 Effecter 有效来源和目标，并通过小数 offset 保住盾面精确位置。
Assert-True (
    ($shieldText -match 'new\s+TargetInfo\(Pawn\)') -and
    ($shieldText -match 'new\s+TargetInfo\(instigator\)') -and
    ($shieldText -notmatch 'effecter\.Trigger\([\s\S]*?TargetInfo\.Invalid') -and
    ($shieldText -match 'effecter\.offset\s*=')
) '原版偏转 Effecter 必须取得有效 A/B，并用 offset 保留小数盾面位置。'

# 中性视觉冲量：护盾事件必须按宿主自己的表达结果 ID 发布，不能依赖渲染投影。
Assert-True (
    ($feedbackText -match 'NotifyImpact') -and
    ($feedbackText -match 'BdpExpressionHostHediff') -and
    ($feedbackText -match 'hostHediff\.ExpressionResults') -and
    ($feedbackText -match 'FormalExpressionResult\s+result') -and
    ($feedbackText -match 'result\.Id') -and
    ($feedbackText -notmatch 'PublishedPresentationProjection|VisualProjection|ResidentEntries') -and
    ($ownerText -match 'PublishExpressionVisualImpulse') -and
    ($ownerText -match 'ResolveExpressionVisualImpulseOffset') -and
    ($drawText -match 'ApplyExpressionVisualImpulse') -and
    ($drawText -match 'pose\.DrawPosition\s*\+=') -and
    ($drawText -match 'overlay\.DrawPosition\s*\+=')
) '表达视觉冲量必须按结果发布，并只叠加到对应主贴图和附加层。'

# 回弹曲线必须包含受击内缩、反向小回弹和结束归零三个阶段。
Assert-True (
    ($impulseText -match 'ResolveOffset') -and
    ($impulseText -match 'Rebound') -and
    ($impulseText -match 'Vector3\.zero')
) '表达视觉冲量必须实现短促内缩、反向回弹和归零。'

# 两个光魂盾姿态都应显式启用同一组可调回弹参数。
[xml]$lightSoulXml = Get-Content -Raw -Encoding UTF8 $lightSoulPath
$hediffs = @($lightSoulXml.Defs.HediffDef)
Assert-True ($hediffs.Count -eq 2) '光魂必须保留灵活、举盾两个 Hediff。'
foreach ($hediff in $hediffs) {
    $props = @($hediff.comps.li) | Where-Object {
        $_.Class -eq 'BDP.Content.Shield.HediffCompProperties_EnergyShield'
    } | Select-Object -First 1
    Assert-True ($null -ne $props) ($hediff.defName + ' 缺少能量护盾组件。')
    Assert-True ([int]$props.blockVisualImpulseTicks -eq 8) ($hediff.defName + ' 回弹时长必须为 8 tick。')
    Assert-True ([Math]::Abs([single]$props.blockVisualImpulseDistance - 0.03) -lt 0.0001) `
        ($hediff.defName + ' 回弹距离必须提高到 0.03 格。')
}

# 装入真实程序集，验证回弹曲线关键帧：首次内缩、半程反弹、结束归零。
$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.CoreModule.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.dll')) | Out-Null
[Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'Assembly-CSharp.dll')) | Out-Null
$coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Core.dll'))
$impulseType = $coreAssembly.GetType('BDP.Core.Trigger.Runtime.ExpressionVisualImpulse', $true)
$bindingFlags = [Reflection.BindingFlags]::Instance -bor [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic
$impulse = [Activator]::CreateInstance($impulseType, $true)
$impulseType.GetProperty('StartTick', $bindingFlags).SetValue($impulse, 100)
$impulseType.GetProperty('Direction', $bindingFlags).SetValue(
    $impulse,
    [UnityEngine.Vector3]::new(1, 0, 0))
$impulseType.GetProperty('Distance', $bindingFlags).SetValue($impulse, [single]0.04)
$impulseType.GetProperty('DurationTicks', $bindingFlags).SetValue($impulse, 8)
$resolveOffset = $impulseType.GetMethod('ResolveOffset', $bindingFlags)
$initialOffset = [UnityEngine.Vector3]$resolveOffset.Invoke($impulse, @(100))
$reboundOffset = [UnityEngine.Vector3]$resolveOffset.Invoke($impulse, @(104))
$finishedOffset = [UnityEngine.Vector3]$resolveOffset.Invoke($impulse, @(108))
Assert-True ([Math]::Abs($initialOffset.x - 0.04) -lt 0.0001) '回弹首帧必须沿攻击行进方向内缩 0.04 格。'
Assert-True ([Math]::Abs($reboundOffset.x + 0.008) -lt 0.0001) '回弹半程必须反向回弹首次位移的 20%。'
Assert-True ($finishedOffset -eq [UnityEngine.Vector3]::zero) '回弹结束必须完全归零。'

Write-Output 'LightSoulBlockFeedbackSmokeTests PASS'
