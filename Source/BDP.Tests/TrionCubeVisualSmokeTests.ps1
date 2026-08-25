$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$visualPath = Join-Path $modRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$actionPath = Join-Path $modRoot '1.6\Content\Defs\ChipActionPresetDef\Presets.xml'
$armamentFormPath = Join-Path $modRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'
$textureRoot = Join-Path $modRoot '1.6\Textures\Things\Trigger\Chip\Trion'

$baseTexturePath = Join-Path $textureRoot 'trion_cube_base.png'
$glowTexturePath = Join-Path $textureRoot 'trion_cube_glow.png'
$warmupBaseTexturePath = Join-Path $textureRoot 'trion_cube_warmup_base.png'
$warmupGlowTexturePath = Join-Path $textureRoot 'trion_cube_warmup_glow.png'

Assert-True (Test-Path -LiteralPath $visualPath) '视觉预设 XML 必须存在。'
Assert-True (Test-Path -LiteralPath $actionPath) '动作预设 XML 必须存在。'
Assert-True (Test-Path -LiteralPath $armamentFormPath) '武装型 XML 必须存在。'
Assert-True (Test-Path -LiteralPath $baseTexturePath) 'Trion 立方体主贴图必须存在。'
Assert-True (Test-Path -LiteralPath $glowTexturePath) 'Trion 立方体发光贴图必须存在。'
Assert-True (Test-Path -LiteralPath $warmupBaseTexturePath) 'Trion 立方体预热主贴图必须存在。'
Assert-True (Test-Path -LiteralPath $warmupGlowTexturePath) 'Trion 立方体预热发光贴图必须存在。'

$visualXml = [xml](Get-Content -LiteralPath $visualPath -Raw -Encoding utf8)
$actionXml = [xml](Get-Content -LiteralPath $actionPath -Raw -Encoding utf8)
$armamentFormXml = [xml](Get-Content -LiteralPath $armamentFormPath -Raw -Encoding utf8)

$preset = @($visualXml.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef') |
    Where-Object { $_.defName -eq 'BDP_Visual_TrionCube' } |
    Select-Object -First 1
Assert-True ($null -ne $preset) '必须声明共享 Trion 立方体视觉预设。'
Assert-True ($preset.GraphicData.texPath -eq 'Things/Trigger/Chip/Trion/trion_cube_base') `
    '主层必须使用 Trion 立方体不发光贴图。'
Assert-True ($preset.GraphicData.shaderType -eq 'Cutout') `
    '主层必须使用 Cutout 材质。'
Assert-True ($null -eq $preset.GraphicData.drawSize) '本需求不得声明新的主视觉尺寸。'
Assert-True ($null -eq $preset.SouthNorthPose -and $null -eq $preset.EastWestPose) `
    '本需求不得声明新的南北或东西姿态。'
Assert-True ($null -ne $preset.Muzzle) '单武器 Trion 视觉必须声明枪口锚点。'
Assert-True ($preset.Muzzle.IsRangedWeapon -eq 'true') `
    '单武器 Trion 枪口锚点必须标记为远程发射点。'
Assert-True ($preset.Muzzle.MuzzleOffset -eq '(0, 0, 0)') `
    '单武器 Trion 枪口锚点必须落在最终武器绘制位。'

$dualPreset = @($visualXml.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef') |
    Where-Object { $_.defName -eq 'BDP_Visual_TrionCube_Dual' } |
    Select-Object -First 1
Assert-True ($null -ne $dualPreset) '必须声明双武器 Trion 立方体视觉预设。'
Assert-True ($dualPreset.ParentName -eq 'BDP_VisualBase_RangedMedium_Dual') `
    '双武器预设必须沿用标准远程双武器视觉基类。'
Assert-True ($dualPreset.GraphicData.texPath -eq 'Things/Trigger/Chip/Trion/trion_cube_base') `
    '双武器主层必须使用同一张 Trion 立方体不发光贴图。'
Assert-True ($dualPreset.GraphicData.shaderType -eq 'Cutout') `
    '双武器主层必须使用 Cutout 材质。'
Assert-True ($null -eq $dualPreset.GraphicData.drawSize) `
    '双武器预设不得声明新的主视觉尺寸。'
Assert-True ($null -ne $dualPreset.Muzzle) '双武器 Trion 视觉必须显式声明枪口锚点。'
Assert-True ($dualPreset.Muzzle.IsRangedWeapon -eq 'true') `
    '双武器 Trion 枪口锚点必须标记为远程发射点。'
Assert-True ($dualPreset.Muzzle.MuzzleOffset -eq '(0, 0, 0)') `
    '双武器 Trion 枪口锚点必须落在各自最终武器绘制位。'

function Assert-TrionStageVisual {
    param([object]$VisualPreset, [string]$PresetLabel)

    Assert-True ($null -ne $VisualPreset.StageVisuals) "$PresetLabel 必须声明动作阶段视觉覆盖。"
    $stageNodes = @($VisualPreset.StageVisuals.li)
    $warmup = $stageNodes | Where-Object { $_.Stage -eq 'Warmup' } | Select-Object -First 1
    $firing = $stageNodes | Where-Object { $_.Stage -eq 'Firing' } | Select-Object -First 1
    $cooldown = $stageNodes | Where-Object { $_.Stage -eq 'Cooldown' } | Select-Object -First 1

    Assert-True ($null -ne $warmup) "$PresetLabel 必须声明预热阶段视觉。"
    Assert-True ($warmup.GraphicData.texPath -eq 'Things/Trigger/Chip/Trion/trion_cube_warmup_base') `
        "$PresetLabel 预热主层必须使用 Trion 立方体切割不发光贴图。"
    Assert-True ($warmup.GraphicData.shaderType -eq 'Cutout') `
        "$PresetLabel 预热主层必须使用 Cutout 材质。"
    Assert-True ($null -eq $warmup.GraphicData.drawSize) `
        "$PresetLabel 预热主层不得声明新的视觉尺寸。"

    $warmupOverlay = @($warmup.OverlayLayers.li) | Select-Object -First 1
    Assert-True ($null -ne $warmupOverlay) "$PresetLabel 预热阶段必须声明发光叠层。"
    Assert-True ($warmupOverlay.GraphicData.texPath -eq 'Things/Trigger/Chip/Trion/trion_cube_warmup_glow') `
        "$PresetLabel 预热叠层必须使用 Trion 立方体切割带发光贴图。"
    Assert-True ($warmupOverlay.GraphicData.shaderType -eq 'MoteGlow') `
        "$PresetLabel 预热叠层必须使用 MoteGlow 材质。"
    Assert-True ($warmupOverlay.GraphicData.color -eq '(0.72, 1.0, 0.76)') `
        "$PresetLabel 预热叠层必须沿用 Trion 代表性青绿色。"
    Assert-True ($null -eq $warmupOverlay.GraphicData.drawSize) `
        "$PresetLabel 预热叠层不得声明新的视觉尺寸。"

    Assert-True ($null -ne $firing -and $firing.Visible -eq 'false') `
        "$PresetLabel 射击阶段必须隐藏 Trion 立方体视觉。"
    Assert-True ($null -ne $cooldown -and $cooldown.Visible -eq 'false') `
        "$PresetLabel 冷却阶段必须隐藏 Trion 立方体视觉。"
}

Assert-TrionStageVisual $preset '单武器 Trion 视觉预设'
Assert-TrionStageVisual $dualPreset '双武器 Trion 视觉预设'

$overlay = @($preset.OverlayLayers.li) | Select-Object -First 1
Assert-True ($null -ne $overlay) '必须声明 Trion 立方体发光叠层。'
Assert-True ($overlay.GraphicData.texPath -eq 'Things/Trigger/Chip/Trion/trion_cube_glow') `
    '发光叠层必须使用 Trion 立方体带发光贴图。'
Assert-True ($overlay.GraphicData.shaderType -eq 'MoteGlow') `
    '发光叠层必须使用 MoteGlow 材质。'
Assert-True ($overlay.GraphicData.color -eq '(0.72, 1.0, 0.76)') `
    '发光叠层必须使用 Trion 贴图代表性的青绿色，且不额外声明透明度。'
Assert-True ($null -eq $overlay.GraphicData.drawSize) '本需求不得声明新的发光层尺寸。'

$dualOverlay = @($dualPreset.OverlayLayers.li) | Select-Object -First 1
Assert-True ($null -ne $dualOverlay) '双武器预设必须声明 Trion 立方体发光叠层。'
Assert-True ($dualOverlay.GraphicData.texPath -eq 'Things/Trigger/Chip/Trion/trion_cube_glow') `
    '双武器发光叠层必须使用同一张 Trion 立方体带发光贴图。'
Assert-True ($dualOverlay.GraphicData.shaderType -eq 'MoteGlow') `
    '双武器发光叠层必须使用 MoteGlow 材质。'
Assert-True ($dualOverlay.GraphicData.color -eq '(0.72, 1.0, 0.76)') `
    '双武器发光叠层必须使用同一组 Trion 贴图代表性青绿色。'
Assert-True ($null -eq $dualOverlay.GraphicData.drawSize) `
    '双武器预设不得声明新的发光层尺寸。'

$actionDefs = @($actionXml.Defs.'BDP.Content.Assembly.ChipManufacturing.Defs.ChipActionPresetDef')
$expectedEntries = @{
    'BDP_Preset_Asteroid' = 'sequential_primary'
    'BDP_Preset_Viper' = 'viper_primary'
    'BDP_Preset_Hound' = 'tracking_primary'
    'BDP_Preset_Meteora' = 'meteora_primary'
}

$trionForm = @($armamentFormXml.Defs.'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef') |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_TrionCube' } |
    Select-Object -First 1
Assert-True ($null -ne $trionForm) '必须声明 Trion 立方体隐藏默认型。'
Assert-True ($trionForm.overrides.visualPresetDefName -eq 'BDP_Visual_TrionCube') `
    'Trion 立方体型必须接管单武器贴图预设。'
Assert-True ($trionForm.overrides.compositeVisualPresetDefName -eq 'BDP_Visual_TrionCube_Dual') `
    'Trion 立方体型必须接管双武器贴图预设。'
Assert-True ([bool]$trionForm.implicitDefault) 'Trion 立方体型必须作为逻辑默认型参与自动解析。'
Assert-True ($trionForm.showInManufacturing -eq 'false') 'Trion 立方体型必须保持制造台不可见。'
Assert-True ($trionForm.includeInProductLabel -eq 'false') 'Trion 立方体型必须保持成品名称不可见。'
Assert-True ($trionForm.compatibleProfessions.li -contains 'BDP_ChipProfession_Shooter') `
    'Trion 立方体型必须只作为射手体系的默认远程武装型。'
Assert-True ([string]$trionForm.overrides.muzzleFlashScale -eq '1.8') `
    'Trion 立方体型的原版枪口闪光尺寸必须显式增强到 1.8。'
$originSpread = $trionForm.overrides.originSpread
Assert-True ($null -ne $originSpread) 'Trion 立方体型必须声明真实发射点随机散布。'
Assert-True ($null -eq $trionForm.SelectSingleNode('./overrides/OriginSpread')) `
    'Trion 散布字段必须使用 C# 字段的小写 originSpread 标签。'
Assert-True ($originSpread.LateralMin -eq '-0.3') 'Trion 随机散布的左右下限必须为 -0.3。'
Assert-True ($originSpread.LateralMax -eq '0.3') 'Trion 随机散布的左右上限必须为 0.3。'
Assert-True ($originSpread.ForwardMin -eq '0') 'Trion 随机散布的前后下限必须为 0。'
Assert-True ($originSpread.ForwardMax -eq '0.105') 'Trion 随机散布的前后上限必须为 0.105。'

foreach ($presetName in $expectedEntries.Keys) {
    $action = $actionDefs | Where-Object { $_.defName -eq $presetName } | Select-Object -First 1
    Assert-True ($null -ne $action) "缺少远程动作预设：$presetName"
    $entryId = $expectedEntries[$presetName]
    $entry = @($action.config.Expression.Entries.li) |
        Where-Object { $_.Id -eq $entryId } |
        Select-Object -First 1
    Assert-True ($null -ne $entry) "缺少远程表达条目：$presetName/$entryId"
    Assert-True ($action.profession -eq 'BDP_ChipProfession_Shooter') `
        "当前 Trion 立方体视觉动作必须属于射手体系：$presetName"
    Assert-True ($null -eq $entry.Presentation -or [string]::IsNullOrWhiteSpace([string]$entry.Presentation.VisualPresetDefName)) `
        "射手远程表达不得继续在动作层声明单武器贴图预设：$presetName/$entryId"
    Assert-True ($null -eq $entry.Presentation -or [string]::IsNullOrWhiteSpace([string]$entry.Presentation.CompositeVisualPresetDefName)) `
        "射手远程表达不得继续在动作层声明双武器贴图预设：$presetName/$entryId"
}

# 使用正式程序集实际执行一次武装型视觉应用，证明贴图预设已从动作层迁移到隐藏 Trion 型仍能生效。
function New-TypedList {
    param([Type]$ElementType, [object[]]$Items = @())
    $openListType = [System.Collections.Generic.List``1]
    $listType = $openListType.MakeGenericType([Type[]]@($ElementType))
    $list = [Activator]::CreateInstance($listType)
    foreach ($item in $Items) { [void]$list.Add($item) }
    return ,$list
}

$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.CoreModule.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.dll'))
$gameAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'Assembly-CSharp.dll'))
$coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Core.dll'))
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Content.dll'))

$entryType = $coreAssembly.GetType('BDP.Core.Expressions.ChipExpressionEntryConfig', $true)
$entry = [Activator]::CreateInstance($entryType)
$entry.Id = 'trion_runtime_entry'
$entry.Kind = [Enum]::Parse($entryType.GetField('Kind').FieldType, 'PrimaryVerb')
$entry.WeaponMode = [Enum]::Parse($entryType.GetField('WeaponMode').FieldType, 'Ranged')
$formType = $contentAssembly.GetType('BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef', $true)
$form = [Activator]::CreateInstance($formType)
$overrideType = $contentAssembly.GetType('BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormOverrides', $true)
$overrides = [Activator]::CreateInstance($overrideType)
$overrides.visualPresetDefName = 'BDP_Visual_TrionCube'
$overrides.compositeVisualPresetDefName = 'BDP_Visual_TrionCube_Dual'
$form.overrides = $overrides
$applyType = $contentAssembly.GetType('BDP.Content.Assembly.ChipManufacturing.Resolution.ChipArmamentFormExpressionService', $true)
$mergedEntries = $applyType.GetMethod('MergeEntries').Invoke($null, @((New-TypedList $entryType @($entry)), $form, [string]$null))
Assert-True ($mergedEntries.Count -eq 1) 'Trion 立方体型运行时测试必须保留远程表达条目。'
Assert-True ($mergedEntries[0].Presentation.VisualPresetDefName -eq 'BDP_Visual_TrionCube') `
    '隐藏 Trion 型实际应用后必须得到单武器视觉预设。'
Assert-True ($mergedEntries[0].Presentation.CompositeVisualPresetDefName -eq 'BDP_Visual_TrionCube_Dual') `
    '隐藏 Trion 型实际应用后必须得到双武器视觉预设。'

# 同一条集中规则还必须保证“非空白名单时双动作全量匹配”，不允许半套构型覆盖。
$selectionType = $contentAssembly.GetType('BDP.Content.Assembly.ChipManufacturing.Resolution.ChipCombinationSelectionRules', $true)
$actionType = $contentAssembly.GetType('BDP.Content.Assembly.ChipManufacturing.Defs.ChipActionPresetDef', $true)
$goodAction = [Activator]::CreateInstance($actionType)
$goodAction.defName = 'BDP_Preset_Kogetsu'
$badAction = [Activator]::CreateInstance($actionType)
$badAction.defName = 'BDP_Test_NonKogetsu'
$form.compatibleActionPresetDefNames = New-TypedList ([string]) @('BDP_Preset_Kogetsu')
$singlePredicate = $selectionType.GetMethod('CanUseArmamentFormAction')
$allPredicate = $selectionType.GetMethod('CanUseArmamentForm')
Assert-True ([bool]$singlePredicate.Invoke($null, @($form, $goodAction))) `
    '临时型必须接受弧月动作。'
Assert-True (-not [bool]$singlePredicate.Invoke($null, @($form, $badAction))) `
    '临时型必须拒绝非弧月动作。'
Assert-True (-not [bool]$allPredicate.Invoke($null, @($form, (New-TypedList $actionType @($goodAction, $badAction))))) `
    '双动作中包含不匹配动作时必须整体拒绝临时型。'

# 在真实 DefDatabase 中注册最小射手远程场景，验证无显式构型时会命中隐藏 Trion 默认型。
function Add-ReflectedDef {
    param([object]$Definition)
    $databaseType = $gameAssembly.GetType('Verse.DefDatabase`1').MakeGenericType([Type[]]@($Definition.GetType()))
    $addMethod = @($databaseType.GetMethods([Reflection.BindingFlags]'Public,Static') |
        Where-Object {
            ($_.Name -eq 'Add') -and
            ($_.GetParameters().Count -eq 1) -and
            ($_.GetParameters()[0].ParameterType -eq $Definition.GetType())
        } | Select-Object -First 1)
    [void]$addMethod.Invoke($null, @($Definition))
}

$categoryType = $coreAssembly.GetType('BDP.Core.Chips.ChipCategoryDef', $true)
$professionType = $contentAssembly.GetType('BDP.Content.Assembly.ChipManufacturing.Defs.ChipProfessionDef', $true)
$configType = $coreAssembly.GetType('BDP.Core.Chips.ChipDefinitionConfig', $true)
$profileType = $coreAssembly.GetType('BDP.Core.Chips.ChipProfileConfig', $true)
$expressionType = $coreAssembly.GetType('BDP.Core.Expressions.ChipExpressionConfig', $true)
$category = [Activator]::CreateInstance($categoryType)
$category.defName = 'BDP_ChipCategory_Weapon'
$shooter = [Activator]::CreateInstance($professionType)
$shooter.defName = 'BDP_Test_Shooter_Trion'
$rangedAction = [Activator]::CreateInstance($actionType)
$rangedAction.defName = 'BDP_Test_RangedAction_Trion'
$rangedAction.profession = $shooter
$rangedConfig = [Activator]::CreateInstance($configType)
$rangedProfile = [Activator]::CreateInstance($profileType)
$rangedProfile.Category = $category
$rangedConfig.Profile = $rangedProfile
$rangedExpression = [Activator]::CreateInstance($expressionType)
$rangedExpression.Entries = New-TypedList $entryType @($entry)
$rangedConfig.Expression = $rangedExpression
$rangedAction.config = $rangedConfig
$runtimeForm = [Activator]::CreateInstance($formType)
$runtimeForm.defName = 'BDP_Test_TrionDefault'
$runtimeForm.implicitDefault = $true
$runtimeForm.compatibleProfessions = New-TypedList $professionType @($shooter)
$runtimeForm.overrides = $overrides
Add-ReflectedDef $category
Add-ReflectedDef $shooter
Add-ReflectedDef $rangedAction
Add-ReflectedDef $runtimeForm
$lookupType = $contentAssembly.GetType('BDP.Content.Assembly.ChipManufacturing.Resolution.ChipManufacturingDefLookup', $true)
$implicit = $lookupType.GetMethod('FindImplicitDefaultArmamentForm').Invoke(
    $null,
    @($category, $shooter, (New-TypedList $actionType @($rangedAction))))
Assert-True ($null -ne $implicit -and $implicit.defName -eq 'BDP_Test_TrionDefault') `
    '射手远程动作无显式构型时必须从 DefDatabase 命中隐藏 Trion 默认型。'

Write-Output 'TrionCubeVisualSmokeTests PASS'
