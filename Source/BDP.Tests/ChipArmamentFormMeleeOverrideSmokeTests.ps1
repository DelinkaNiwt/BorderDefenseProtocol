# 武装型近战覆盖冒烟测试。

$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

function New-TypedList {
    param([Type]$ElementType, [object[]]$Items = @())
    $openListType = [System.Collections.Generic.List``1]
    $listType = $openListType.MakeGenericType([Type[]]@($ElementType))
    $list = [Activator]::CreateInstance($listType)
    foreach ($item in $Items) { [void]$list.Add($item) }
    return ,$list
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$visualPath = Join-Path $modRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$actionPath = Join-Path $modRoot '1.6\Content\Defs\ChipActionPresetDef\Presets.xml'
$formPath = Join-Path $modRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'
$servicePath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipArmamentFormExpressionService.cs'

$visualXml = [xml](Get-Content -LiteralPath $visualPath -Raw -Encoding utf8)
$actionXml = [xml](Get-Content -LiteralPath $actionPath -Raw -Encoding utf8)
$formXml = [xml](Get-Content -LiteralPath $formPath -Raw -Encoding utf8)
$tempVisual = @($visualXml.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef') |
    Where-Object { $_.defName -eq 'BDP_Visual_Temporary_BreachAxe' } |
    Select-Object -First 1
$tempForm = @($formXml.Defs.'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef') |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_Temporary' } |
    Select-Object -First 1
$kogetsu = @($actionXml.Defs.'BDP.Content.Assembly.ChipManufacturing.Defs.ChipActionPresetDef') |
    Where-Object { $_.defName -eq 'BDP_Preset_Kogetsu' } |
    Select-Object -First 1
$kogetsuEntry = @($kogetsu.config.Expression.Entries.li) |
    Where-Object { $_.Id -eq 'kogetsu_primary' } |
    Select-Object -First 1

Assert-True ($null -ne $tempVisual) '缺少临时破墙斧视觉预设。'
Assert-True (($null -eq $tempVisual.ParentName) -and
    ($null -eq $tempVisual.SouthNorthPose) -and
    ($null -eq $tempVisual.EastWestPose)) `
    '临时破墙斧视觉当前必须保持纯贴图预设，不得伪装成弧月姿态预设。'
Assert-True ($null -ne $tempForm) '缺少临时攻击手型。'
Assert-True ($tempForm.compatibleProfessions.li -contains 'BDP_ChipProfession_Attacker') `
    '临时型必须属于攻击手体系。'
Assert-True ($tempForm.compatibleActionPresetDefNames.li -contains 'BDP_Preset_Kogetsu') `
    '临时型必须只允许弧月动作。'
Assert-True (($null -eq $tempForm.overrides.visualPresetDefName) -and
    ($tempForm.overrides.visualGraphicOverrideDefName -eq 'BDP_Visual_Temporary_BreachAxe')) `
    '临时型必须使用主视觉贴图局部覆盖，不得替换弧月基础视觉预设。'
Assert-True ($null -ne $kogetsuEntry -and $kogetsuEntry.WeaponMode -eq 'Melee') `
    '弧月动作必须是近战模式。'
Assert-True ($null -eq $tempForm.overrides.range -and
    $null -eq $tempForm.overrides.accuracyTouch -and
    $null -eq $tempForm.overrides.defaultCooldownTime) `
    '临时型不得声明远程 VerbProperties 属性。'

$managedRoot = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.CoreModule.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'UnityEngine.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot 'Assembly-CSharp.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Core.dll'))
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Content.dll'))

$entryType = $contentAssembly.GetType('BDP.Core.Expressions.ChipExpressionEntryConfig', $false)
if ($null -eq $entryType) {
    $entryType = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Core.dll')).GetType(
        'BDP.Core.Expressions.ChipExpressionEntryConfig', $true)
}
$entry = [Activator]::CreateInstance($entryType)
$entry.Id = 'temporary_melee_entry'
$entry.Kind = [Enum]::Parse($entryType.GetField('Kind').FieldType, 'PrimaryVerb')
$entry.WeaponMode = [Enum]::Parse($entryType.GetField('WeaponMode').FieldType, 'Melee')
$presentationType = $contentAssembly.GetType(
    'BDP.Core.Expressions.ExpressionPresentationConfig', $false)
if ($null -eq $presentationType) {
    $presentationType = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Core.dll')).GetType(
        'BDP.Core.Expressions.ExpressionPresentationConfig', $true)
}
$presentation = [Activator]::CreateInstance($presentationType)
$presentation.VisualPresetDefName = 'BDP_Visual_Kogetsu'
$entry.Presentation = $presentation

$formType = $contentAssembly.GetType(
    'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef', $true)
$overrideType = $contentAssembly.GetType(
    'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormOverrides', $true)
$form = [Activator]::CreateInstance($formType)
$overrides = [Activator]::CreateInstance($overrideType)
$overrides.visualGraphicOverrideDefName = 'BDP_Visual_Temporary_BreachAxe'
$form.overrides = $overrides
$applyType = $contentAssembly.GetType(
    'BDP.Content.Assembly.ChipManufacturing.Resolution.ChipArmamentFormExpressionService', $true)
$merged = $applyType.GetMethod('MergeEntries').Invoke(
    $null,
    @((New-TypedList $entryType @($entry)), $form, [string]$null))

Assert-True ($merged.Count -eq 1) '临时型应用后必须保留弧月近战表达条目。'
Assert-True ($merged[0].WeaponMode.ToString() -eq 'Melee') `
    '临时型应用不得改变弧月的近战模式。'
Assert-True ($null -eq $merged[0].VerbProps) `
    '只换贴图的临时型不得凭空创建默认 VerbProperties。'
Assert-True ($merged[0].Presentation.VisualPresetDefName -eq 'BDP_Visual_Kogetsu') `
    '局部贴图覆盖不得替换弧月基础视觉预设。'
Assert-True ($merged[0].Presentation.VisualGraphicOverrideDefName -eq 'BDP_Visual_Temporary_BreachAxe') `
    '局部贴图覆盖必须随表达条目保留。'

$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
Assert-True ($serviceText -match 'HasVerbPropertiesOverrides') `
    '武装型覆盖服务必须显式区分 VerbProperties 覆盖与视觉覆盖。'
Assert-True ($serviceText -match 'visualGraphicOverrideDefName') `
    '武装型覆盖服务必须支持主视觉贴图局部覆盖。'

$projectionPath = Join-Path $modRoot 'Source\\BDP\\Core\\Expressions\\Projection\\DefaultVisualProjectionBuilder.cs'
$poseResolverPath = Join-Path $modRoot 'Source\\BDP\\Core\\Trigger\\Visual\\VisualPoseResolver.cs'
$renderPatchPath = Join-Path $modRoot 'Source\\BDP\\Patches\\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$projectionText = Get-Content -LiteralPath $projectionPath -Raw -Encoding utf8
$poseResolverText = Get-Content -LiteralPath $poseResolverPath -Raw -Encoding utf8
$renderPatchText = Get-Content -LiteralPath $renderPatchPath -Raw -Encoding utf8
Assert-True (($projectionText -match 'HostEquipmentRenderMode.ReplaceTextureOnly') -and
    ($projectionText -match 'HasExplicitPose') -and
    ($projectionText -match 'HasGraphicOverride')) `
    '无显式姿态的视觉预设必须进入贴图替换模式，而不是隐式继承动作姿态。'
Assert-True (($poseResolverText -match 'GraphicOverridePreset \?\? request\.Preset') -and
    ($poseResolverText -match 'ResolveMainGraphic') -and
    ($poseResolverText -match 'overlayPreset\.OverlayLayers')) `
    '局部视觉覆盖必须替换视觉图层，姿态解析必须继续使用基础动作视觉预设。'
Assert-True (($poseResolverText -match 'ResolveTextureOnly') -and
    ($poseResolverText -match 'CalculateVanillaPose')) `
    '贴图替换模式必须明确走原版持械姿态计算。'
Assert-True ($renderPatchText -match 'equipment\.def\.equippedAngleOffset') `
    '原版持械姿态必须读取当前宿主装备的角度偏移。'

Write-Output 'ChipArmamentFormMeleeOverrideSmokeTests PASS'
