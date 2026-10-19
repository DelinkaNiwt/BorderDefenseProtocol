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
$presetPath = Join-Path $modRoot 'Common\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$visualPath = Join-Path $modRoot 'Common\Content\Defs\ExpressionDef\Visual.xml'
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
$preset = Get-RequiredNode $presetXml '/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef[defName="BDP_ChipFunction_LightSoul"]' '缺少光魂芯片功能模板。'
$entries = $preset.config.Expression.Entries.li
$entryById = @{}
foreach ($entry in $entries) { $entryById[[string]$entry.Id] = $entry }

# 防护 Hediff 负责盾面；冲拳作为真实武器单独发布，不伪造防护武器身份。
$weaponId = 'light_soul_punch'
Assert-True $entryById.ContainsKey($weaponId) '进攻形态必须保留迁入的冲拳真实武器表达。'
Assert-True (-not $entryById.ContainsKey('light_soul_propulsion')) '光魂推进必须从光魂公共表达中抽离为组合技。'
$weapon = $entryById[$weaponId]
Assert-True (($weapon.EntryType -eq 'PrimaryVerb') -and ($weapon.WeaponMode -eq 'Melee')) `
    '冲拳武器必须是正式近战主攻击。'
Assert-True ([int]$weapon.Execution.HitCount -eq 1) '冲拳武器每轮必须只命中一次。'
Assert-True (@($weapon.tools.li).Count -eq 1) '冲拳武器必须只声明一个钝击 Tool。'
Assert-True ([string]$weapon.tools.li.capacities.li -eq 'Blunt') '冲拳武器必须造成钝伤。'
Assert-True ([single]$weapon.tools.li.power -eq 5) '冲拳武器伤害必须为 5。'
Assert-True ($null -eq $weapon.Presentation) '冲拳武器不得重复声明盾面视觉。'
Assert-True ([string]$weapon.ToolLabelKeys.li -eq 'BDP_Tool_LightSoulPunch') `
    '冲拳 Tool 必须使用语言键。'

$defenseShield = $entryById['light_soul_defense_shield']
Assert-True ([string]$defenseShield.Presentation.VisualPresetDefName -eq 'BDP_Visual_LightSoulDefenseShield') `
    '举盾 Hediff 必须保留单侧盾面视觉。'
Assert-True ([string]$defenseShield.Presentation.CompositeVisualPresetDefName -eq 'BDP_Visual_LightSoulDefenseShield_Dual') `
    '举盾 Hediff 必须声明双持复合视觉。'

$defenseMode = $preset.config.Expression.Modes.li | Where-Object ModeKey -eq 'defense'
Assert-True (($defenseMode.ActiveEntryIds.li -join '|') -eq 'light_soul_defense_shield|light_soul_defense_state') `
    '防护形态不得发布自己的正式武器。'
$attackMode = $preset.config.Expression.Modes.li | Where-Object ModeKey -eq 'attack'
$punchStance = $attackMode.Stances.li | Where-Object StanceKey -eq 'punch'
Assert-True (($punchStance.ActiveEntryIds.li -join '|') -eq 'light_soul_punch') `
    '原盾击的真实武器身份必须只由进攻形态的冲拳姿态发布。'

# 双持视觉的位置差只由各业务预设决定；翻转和前后景规则与单持一致。
$defenseSingleVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulDefenseShield"]' '缺少举盾单侧视觉。'
$defenseDualVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulDefenseShield_Dual"]' '缺少举盾双持视觉。'
Assert-True ([Math]::Abs([Math]::Abs((Get-VectorX $defenseDualVisual.SouthNorthPose.DefaultOffset)) - [Math]::Abs((Get-VectorX $defenseSingleVisual.SouthNorthPose.DefaultOffset))) -lt 0.0001) `
    '举盾双持视觉必须与单侧视觉共用贴身位置，不再为另一把武器外移。'
Assert-True (([string]$defenseDualVisual.GraphicData.graphicClass -eq 'Graphic_Multi') -and
    ([single]$defenseDualVisual.SouthNorthPose.DefaultAngle -eq -68) -and
    ([string]$defenseDualVisual.SouthNorthPose.HandMirrorOnlyWhenIdle -eq 'true')) `
    '举盾双持视觉必须使用多朝向贴图，并按竖向正视资源形成左右对称斜握。'
Assert-True (([single]$defenseDualVisual.EastWestPose.DefaultAngle -eq -53) -and
    ([string]$defenseDualVisual.EastWestPose.HandMirror -eq 'true') -and
    ([string]$defenseDualVisual.EastWestPose.FinalMirrorByHandOnly -eq 'true') -and
    ([string]$defenseDualVisual.EastWestPose.MainHandAlwaysFront -eq 'false')) `
    '举盾双持东西姿态必须保持侧视图竖直、最终镜像只看手位，并随朝向交换前后景。'
Assert-True ($null -ne $keyedXml.SelectSingleNode('/LanguageData/BDP_Tool_LightSoulPunch')) `
    '语言包必须提供盾牌钝击 Tool 名称。'

Write-Output 'LightSoulRealWeaponBoundarySmokeTests PASS'
