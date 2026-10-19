# 光魂推进与中性地面冲刺设施源码边界冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$contentRoot = Join-Path $modRoot "Source\BDP.Content"
$coreRoot = Join-Path $modRoot "Source\BDP"
$groundDashRoot = Join-Path $contentRoot "GroundDash"
$lightSoulPropulsionRoot = Join-Path $contentRoot "LightSoul\Propulsion"

$groundDashFiles = @(
    "PawnFlyer_GroundDash.cs",
    "GroundDashUtility.cs",
    "Verb_CastAbilityGroundDash.cs"
)
foreach ($fileName in $groundDashFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $groundDashRoot $fileName)) "缺少中性地面冲刺设施：$fileName"
}

$lightSoulFiles = @(
    "CompAbilityEffect_LightSoulPropulsion.cs",
    "PawnFlyer_LightSoulPropulsion.cs",
    "Verb_CastAbilityLightSoulPropulsion.cs"
)
foreach ($fileName in $lightSoulFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $lightSoulPropulsionRoot $fileName)) "缺少光魂推进源码：$fileName"
}

$groundDashText = ($groundDashFiles | ForEach-Object {
    Get-Utf8Text (Join-Path $groundDashRoot $_)
}) -join "`n"
$grasshopperFlyerText = Get-Utf8Text (Join-Path $contentRoot "Grasshopper\PawnFlyer_Grasshopper.cs")
$grasshopperUtilityText = Get-Utf8Text (Join-Path $contentRoot "Grasshopper\GrasshopperUtility.cs")
$grasshopperVerbText = Get-Utf8Text (Join-Path $contentRoot "Grasshopper\Verb_CastAbilityGrasshopper.cs")
$bounceSlashText = Get-Utf8Text (Join-Path $contentRoot "BounceSlash\PawnFlyer_BounceSlash.cs")
$lightSoulVerbText = Get-Utf8Text (Join-Path $lightSoulPropulsionRoot "Verb_CastAbilityLightSoulPropulsion.cs")
$lightSoulText = ($lightSoulFiles | ForEach-Object {
    Get-Utf8Text (Join-Path $lightSoulPropulsionRoot $_)
}) -join "`n"
$coreSourceText = (Get-ChildItem -LiteralPath $coreRoot -Recurse -File -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "[\\/](obj|bin)[\\/]" } |
    ForEach-Object { Get-Utf8Text $_.FullName }) -join "`n"

Assert-True ($groundDashText -match 'class\s+PawnFlyer_GroundDash\s*:\s*PawnFlyer') "中性飞行器必须直接建立在原版 PawnFlyer 上。"
Assert-True ($groundDashText -match 'class\s+Verb_CastAbilityGroundDash\s*:\s*BdpVerb_CastAbility') "中性地面冲刺 Verb 必须复用 BDP 能力基础设施。"
Assert-True ($groundDashText -match 'SimpleCurve|progressCurve') "中性飞行器必须保留单调进度曲线处理。"
Assert-True ($groundDashText -match 'OnDashSegment') "中性飞行器必须提供逐帧线段扩展入口。"
Assert-True ($groundDashText -match 'DoDash') "中性设施必须提供统一地面冲刺生成入口。"
Assert-True ($groundDashText -match 'Impassable|Walkable|Building_Door') "中性路径解析必须识别不可通行几何物。"
Assert-True ($groundDashText -match 'DrawTargetHighlight') "中性目标入口必须保留目标高亮。"

Assert-True ($grasshopperFlyerText -match 'PawnFlyer_GroundDash') "蚱蜢飞行器必须复用中性地面冲刺飞行器。"
Assert-True ($grasshopperUtilityText -match 'GroundDashUtility\.DoDash') "蚱蜢生成工具必须复用中性地面冲刺生成入口。"
Assert-True ($grasshopperVerbText -match 'Verb_CastAbilityGroundDash') "蚱蜢 Verb 必须复用中性地面冲刺目标基础。"
Assert-True ($bounceSlashText -match 'PawnFlyer_GroundDash') "弹射砍击必须直接复用中性地面冲刺基类。"
Assert-True ($bounceSlashText -notmatch 'Grasshopper\.PawnFlyer_Grasshopper') "弹射砍击不得继续绑定蚱蜢专属飞行器。"

Assert-True ($lightSoulText -notmatch 'Grasshopper') "光魂推进源码不得依赖蚱蜢业务命名空间或类型。"
Assert-True ($lightSoulText -match 'Bresenham|BresenhamCellsBetween') "光魂推进必须逐格扫描高速移动线段。"
Assert-True ($lightSoulText -match 'HashSet\s*<\s*Pawn\s*>') "光魂推进必须按 Pawn 去重。"
Assert-True ($lightSoulText -match 'DamageDefOf\.Blunt|damageDef') "光魂推进必须造成钝伤。"
Assert-True ($lightSoulText -match 'BodySize\s*<=\s*[^;]+\+\s*0\.001') "光魂推进体型判定必须对齐投射物停止力比较。"
Assert-True ($lightSoulText -match 'StunFor') "光魂推进必须使用原版 StunHandler 处理撞墙/撞大体型目标后的晕眩。"
Assert-True ($lightSoulText -notmatch 'StaggerFor') "光魂推进不得把踉跄误当作眩晕。"
Assert-True ($lightSoulText -match 'Walkable|Impassable|CanFit|Occupied') "光魂推进推挤必须检查前方可用格，禁止穿墙或重叠。"
Assert-True ($lightSoulText -match 'TryMove|Position\s*=|Notify_Teleported') "光魂推进必须使用原版位置移动/通知路径。"
Assert-True ($lightSoulText -match 'target\.Position\s*=\s*cell;\s*target\.Notify_Teleported\(\);') "被推动 Pawn 必须结束原有工作后再重新进入 AI，不能保留已被清空路径的旧工作。"
Assert-True ($lightSoulText -notmatch 'target\.Notify_Teleported\(endCurrentJob:\s*false') "被推动 Pawn 不得使用保留旧工作的传送通知，否则可能清空路径后永久停滞。"
Assert-True ($lightSoulText -match 'DrawEffectPreview|DrawFieldEdges') "光魂推进必须提供一格宽的直线区域预览。"
Assert-True ($lightSoulText -match 'Color') "光魂推进必须支持大型目标后的红色预览区域。"
Assert-True ($lightSoulText -match 'pushableTargets\.Count.*\+\s*1|targets\.Count.*\+\s*1') "光魂推进玩家落点必须为被推动队列预留位置，禁止穿过目标。"
Assert-True ($lightSoulText -match 'DrawEffectPreviews') "光魂推进专用 Verb 必须调用 Ability 效果预览。"
Assert-True ($lightSoulText -match 'Verb_CastAbilityLightSoulPropulsion\s*:\s*Verb_CastAbilityGroundDash') "光魂推进专用 Verb 必须复用中性地面冲刺目标基础。"
Assert-True ($lightSoulVerbText -match 'DrawRadiusRing') "光魂推进必须保留最大射程圆圈。"
Assert-True ($lightSoulVerbText -match 'override\s+bool\s+CanHitTarget') "光魂推进必须允许射程外或遮挡后的目标进入预览流程。"
Assert-True ($lightSoulVerbText -match 'TryResolvePath') "光魂推进预览可见性必须复用实际路径解析。"
Assert-True ($lightSoulVerbText -match 'override\s+bool\s+ValidateTarget') "光魂推进必须允许已预览的射程外或遮挡后终点确认。"
Assert-True ($lightSoulVerbText -match 'override\s+bool\s+TryStartCastOn') "光魂推进必须在原版暖机二次校验前规范为实际推进终点。"
Assert-True ($lightSoulVerbText -match 'TryCastShot') "光魂推进必须在能力成功启动后补齐原版 Verb 冷却时序。"
Assert-True ($lightSoulVerbText -match 'AdjustedCooldownTicks') "光魂推进释放者冷却必须使用原版 Verb 的修正冷却计算。"
Assert-True ($lightSoulVerbText -match 'Stance_Cooldown') "光魂推进释放者必须使用原版 Stance_Cooldown。"
Assert-True ($lightSoulText -match 'SemanticRuntimeScope\.Push') "光魂推进伤害必须进入伤害语义作用域。"
Assert-True ($lightSoulText -match 'SemanticContext') "光魂推进伤害必须提供伤口来源语义。"
Assert-True ($lightSoulText -match 'DisplayLabel') "光魂推进伤害来源必须使用能力显示名称。"
Assert-True ($lightSoulText -match 'SourceKind\.AttackAction') "光魂推进伤害来源必须标记为攻击行为。"
Assert-True ($lightSoulText -match 'parent\.def\.label') "光魂推进伤口来源必须传入本地化能力名称。"
Assert-True ($lightSoulText -match 'BuildInjurySourceLabel') "光魂推进伤口来源必须包含释放者标签构造。"

Assert-True ($coreSourceText -notmatch 'BDP\.Content|BDP_Content') "Core 源码不得依赖 Content 业务命名空间。"

Write-Host "PASS: 中性地面冲刺、蚱蜢复用关系和光魂推进独立碰撞边界契约正确。"
