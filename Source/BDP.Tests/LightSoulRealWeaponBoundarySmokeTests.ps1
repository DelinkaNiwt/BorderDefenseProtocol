$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# 读取必需 XML（可扩展标记语言）节点。
function Get-RequiredNode
{
    param([System.Xml.XmlNode]$Parent, [string]$XPath, [string]$Message)
    $node = $Parent.SelectSingleNode($XPath)
    Assert-True ($null -ne $node) $Message
    return $node
}

# 读取三维向量文本中的 X 分量。
function Get-VectorX
{
    param([string]$Value)
    return [single]($Value.Trim('(', ')').Split(',')[0])
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $modRoot 'Source\BDP\Core'
$projectionBuilderPath = Join-Path $coreRoot 'Expressions\Projection\DefaultVisualProjectionBuilder.cs'
$presetPath = Join-Path $modRoot '1.6\Content\Defs\ChipActionPresetDef\Presets.xml'
$visualPath = Join-Path $modRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$keyedPath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml'

# Core（核心层）必须恢复只认真实武器的单一判断，不保留本次已否决的通用设施。
$coreText = (Get-ChildItem -LiteralPath $coreRoot -Recurse -File -Filter '*.cs' |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 }) -join "`n"
Assert-True ($coreText -notmatch 'ParticipatesInHandheldVisualRelation') `
    'Core 不得保留非武器参与手持关系声明。'
Assert-True ($coreText -notmatch 'ActiveHandheldVisualSourceCount') `
    'Core 不得保留第二套手持视觉来源计数。'
$projectionBuilderText = Get-Content -LiteralPath $projectionBuilderPath -Raw -Encoding UTF8
Assert-True ($projectionBuilderText -match 'CountActiveWeaponChipInstances') `
    '双武器视觉必须继续以真实武器芯片实例数量为单一真值。'

[xml]$presetXml = Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8
[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding UTF8
[xml]$keyedXml = Get-Content -LiteralPath $keyedPath -Raw -Encoding UTF8
$preset = Get-RequiredNode $presetXml '/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipActionPresetDef[defName="BDP_Preset_LightSoul"]' '缺少光魂动作预设。'
$entries = $preset.config.Expression.Entries.li
$entryById = @{}
foreach ($entry in $entries) { $entryById[[string]$entry.Id] = $entry }

# 防御 Hediff 保留原结果身份和视觉；真实武器只提供武器身份，不重复绘制。
$weaponId = 'light_soul_shield_weapon'
Assert-True $entryById.ContainsKey($weaponId) '大盾形态必须只提供一条公共真实盾牌武器表达。'
Assert-True (-not $entryById.ContainsKey('light_soul_shield_mobile_weapon')) '灵活姿态不得复制专属盾牌武器。'
Assert-True (-not $entryById.ContainsKey('light_soul_shield_guard_weapon')) '举盾姿态不得复制专属盾牌武器。'
$weapon = $entryById[$weaponId]
Assert-True (($weapon.Kind -eq 'PrimaryVerb') -and ($weapon.WeaponMode -eq 'Melee')) `
    '盾牌武器必须是正式近战主攻击。'
Assert-True ([int]$weapon.Execution.HitCount -eq 1) '盾牌武器每轮必须只命中一次。'
Assert-True (@($weapon.tools.li).Count -eq 1) '盾牌武器必须只声明一个钝击 Tool。'
Assert-True ([string]$weapon.tools.li.capacities.li -eq 'Blunt') '盾牌武器必须造成钝伤。'
Assert-True ([single]$weapon.tools.li.power -eq 5) '盾牌武器伤害必须为 5。'
Assert-True ($null -eq $weapon.Presentation) '盾牌武器不得重复声明盾面视觉。'
Assert-True ([string]$weapon.ToolLabelKeys.li -eq 'BDP_Tool_LightSoulShieldBash') `
    '盾牌 Tool 必须使用语言键。'

$mobileShield = $entryById['light_soul_shield_mobile']
$guardShield = $entryById['light_soul_shield_guard']
Assert-True ([string]$mobileShield.Presentation.VisualPresetDefName -eq 'BDP_Visual_LightSoulShieldMobile') `
    '灵活盾 Hediff 必须保留单侧盾面视觉。'
Assert-True ([string]$guardShield.Presentation.VisualPresetDefName -eq 'BDP_Visual_LightSoulShieldGuard') `
    '举盾 Hediff 必须保留单侧盾面视觉。'
Assert-True ([string]$mobileShield.Presentation.CompositeVisualPresetDefName -eq 'BDP_Visual_LightSoulShieldMobile_Dual') `
    '灵活盾 Hediff 必须声明双持复合视觉。'
Assert-True ([string]$guardShield.Presentation.CompositeVisualPresetDefName -eq 'BDP_Visual_LightSoulShieldGuard_Dual') `
    '举盾 Hediff 必须声明双持复合视觉。'

$shieldMode = $preset.config.Expression.Modes.li | Where-Object { $_.ModeKey -eq 'shield' }
$mobileStance = $shieldMode.Stances.li | Where-Object { $_.StanceKey -eq 'mobile' }
$guardStance = $shieldMode.Stances.li | Where-Object { $_.StanceKey -eq 'guard' }
Assert-True (($shieldMode.ActiveEntryIds.li -join '|') -eq 'light_soul_propulsion|light_soul_shield_weapon') `
    '大盾形态必须公共激活推进与唯一真实盾牌武器。'
Assert-True (($mobileStance.ActiveEntryIds.li -join '|') -eq 'light_soul_shield_mobile') `
    '灵活姿态必须只切换灵活防御 Hediff。'
Assert-True (($guardStance.ActiveEntryIds.li -join '|') -eq 'light_soul_shield_guard') `
    '举盾姿态必须只切换举盾防御 Hediff。'

# 双持视觉的位置差只由各业务预设决定；翻转和前后景规则与单持一致。
$mobileSingleVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldMobile"]' '缺少灵活盾单侧视觉。'
$mobileDualVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldMobile_Dual"]' '缺少灵活盾双持视觉。'
$guardSingleVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldGuard"]' '缺少举盾单侧视觉。'
$guardDualVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldGuard_Dual"]' '缺少举盾双持视觉。'
Assert-True ([Math]::Abs((Get-VectorX $mobileDualVisual.SouthNorthPose.DefaultOffset)) -gt [Math]::Abs((Get-VectorX $mobileSingleVisual.SouthNorthPose.DefaultOffset))) `
    '灵活盾双持视觉必须比单侧视觉更向手侧外移。'
Assert-True ([Math]::Abs([Math]::Abs((Get-VectorX $guardDualVisual.SouthNorthPose.DefaultOffset)) - [Math]::Abs((Get-VectorX $guardSingleVisual.SouthNorthPose.DefaultOffset))) -lt 0.0001) `
    '举盾双持视觉必须与单侧视觉共用贴身位置，不再为另一把武器外移。'
Assert-True (([string]$guardDualVisual.GraphicData.graphicClass -eq 'Graphic_Multi') -and
    ([single]$guardDualVisual.SouthNorthPose.DefaultAngle -eq -68) -and
    ([string]$guardDualVisual.SouthNorthPose.HandMirrorOnlyWhenIdle -eq 'true')) `
    '举盾双持视觉必须使用多朝向贴图，并按竖向正视资源形成左右对称斜握。'
Assert-True (([single]$guardDualVisual.EastWestPose.DefaultAngle -eq -53) -and
    ([string]$guardDualVisual.EastWestPose.HandMirror -eq 'true') -and
    ([string]$guardDualVisual.EastWestPose.FinalMirrorByHandOnly -eq 'true') -and
    ([string]$guardDualVisual.EastWestPose.MainHandAlwaysFront -eq 'false')) `
    '举盾双持东西姿态必须保持侧视图竖直、最终镜像只看手位，并随朝向交换前后景。'
Assert-True ($null -ne $keyedXml.SelectSingleNode('/LanguageData/BDP_Tool_LightSoulShieldBash')) `
    '语言包必须提供盾牌钝击 Tool 名称。'

Write-Output 'LightSoulRealWeaponBoundarySmokeTests PASS'
