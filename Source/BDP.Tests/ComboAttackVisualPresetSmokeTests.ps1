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

function Get-Utf8Text {
    param([string]$Path)
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$presentationPath = Join-Path $coreRoot 'Core\Expressions\Config\ExpressionPresentationConfig.cs'
$contractPath = Join-Path $coreRoot 'Core\Expressions\Contract\ChipExpressionEntryContract.cs'
$interpreterPath = Join-Path $coreRoot 'Core\Expressions\Contract\ChipExpressionContractInterpreter.cs'
$comboEntryPath = Join-Path $coreRoot 'Core\Combos\Config\ComboExpressionEntryConfig.cs'
$comboClonePath = Join-Path $coreRoot 'Core\Combos\Config\ComboExpressionEntryCloneService.cs'
$comboFactoryPath = Join-Path $coreRoot 'Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'
$formOverridesPath = Join-Path $contentRoot 'Assembly\ChipManufacturing\Defs\ChipArmamentFormOverrides.cs'
$formServicePath = Join-Path $contentRoot 'Assembly\ChipManufacturing\Resolution\ChipArmamentFormExpressionService.cs'
$projectionPath = Join-Path $coreRoot 'Core\Expressions\Model\VisualExpressionProjection.cs'
$bindingPath = Join-Path $coreRoot 'Core\Expressions\Model\ComboAttackVisualBinding.cs'
$projectionBuilderPath = Join-Path $coreRoot 'Core\Expressions\Projection\DefaultVisualProjectionBuilder.cs'
$effectiveResolverPath = Join-Path $coreRoot 'Core\Trigger\Visual\TriggerVisualEffectiveEntryResolver.cs'
$drawPatchPath = Join-Path $coreRoot 'Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$trionFormPath = Join-Path $repoRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'

foreach ($path in @(
        $presentationPath,
        $contractPath,
        $interpreterPath,
        $comboEntryPath,
        $comboClonePath,
        $comboFactoryPath,
        $formOverridesPath,
        $formServicePath,
        $projectionPath,
        $projectionBuilderPath,
        $effectiveResolverPath,
        $drawPatchPath,
        $trionFormPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少预期文件：$path"
}

$presentationText = Get-Utf8Text $presentationPath
$contractText = Get-Utf8Text $contractPath
$interpreterText = Get-Utf8Text $interpreterPath
$comboEntryText = Get-Utf8Text $comboEntryPath
$comboCloneText = Get-Utf8Text $comboClonePath
$comboFactoryText = Get-Utf8Text $comboFactoryPath
$formOverridesText = Get-Utf8Text $formOverridesPath
$formServiceText = Get-Utf8Text $formServicePath
$projectionText = Get-Utf8Text $projectionPath
$projectionBuilderText = Get-Utf8Text $projectionBuilderPath
$effectiveResolverText = Get-Utf8Text $effectiveResolverPath
$drawPatchText = Get-Utf8Text $drawPatchPath
$trionFormText = Get-Utf8Text $trionFormPath

Assert-True ($presentationText -match 'ComboAttackVisualPresetDefName') `
    '通用表现配置必须声明组合技攻击视觉预设字段。'
Assert-True ($contractText -match 'ComboAttackVisualPresetDefName') `
    '芯片表达契约必须携带组合技攻击视觉预设字段。'
Assert-True ($interpreterText -match 'ComboAttackVisualPresetDefName') `
    '芯片表达解释器必须传播组合技攻击视觉预设字段。'
Assert-True ($comboEntryText -match 'ComboAttackVisualPresetDefName') `
    '组合表达条目必须复制组合技攻击视觉预设字段。'
Assert-True ($comboCloneText -match 'ComboAttackVisualPresetDefName') `
    '组合表达条目克隆服务必须保留组合技攻击视觉预设字段。'
Assert-True ($comboFactoryText -match 'ComboAttackVisualPresetDefName') `
    '组合正式结果工厂必须发布组合技攻击视觉预设字段。'
Assert-True ($formOverridesText -match 'comboAttackVisualPresetDefName') `
    '武装构型覆盖必须提供组合技攻击视觉预设字段。'
Assert-True ($formServiceText -match 'comboAttackVisualPresetDefName') `
    '武装构型表达服务必须传播组合技攻击视觉预设字段。'

Assert-True (Test-Path -LiteralPath $bindingPath) `
    '视觉投影必须存在组合技攻击视觉绑定模型。'
$bindingText = Get-Utf8Text $bindingPath
Assert-True (($bindingText -match 'ComboResultId') -and
    ($bindingText -match 'VisualPresetDefName') -and
    ($bindingText -match 'OwnerSourceResultId')) `
    '组合技攻击视觉绑定必须携带组合结果、视觉预设和绘制归属来源。'
Assert-True (($projectionText -match 'ComboAttackVisualBindings') -and
    ($projectionBuilderText -match 'ComboAttackVisualBinding') -and
    ($projectionBuilderText -match 'CompositeReferences')) `
    '视觉投影必须依据复合来源引用建立组合技攻击视觉绑定。'
Assert-True (($effectiveResolverText -match 'ComboAttackVisualBindings') -and
    ($effectiveResolverText -match 'HostResultId') -and
    ($effectiveResolverText -match 'WeaponVisualActionStage\.Idle')) `
    '共享视觉条目解析器必须只在组合宿主的非待命阶段消费组合技攻击视觉绑定。'
Assert-True ($drawPatchText -match 'TriggerVisualEffectiveEntryResolver\.TryResolve') `
    '绘制入口必须消费共享有效视觉条目解析器。'

$trionXml = [xml]$trionFormText
$trionForm = @($trionXml.Defs.'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef' |
    Where-Object { [string]$_.defName -eq 'BDP_ArmamentForm_TrionCube' }) | Select-Object -First 1
Assert-True ($null -ne $trionForm) '缺少 Trion 立方体武装构型定义。'
Assert-True ([string]$trionForm.overrides.comboAttackVisualPresetDefName -eq 'BDP_Visual_TrionCube') `
    'Trion 立方体构型的组合技攻击视觉必须暂时复用 BDP_Visual_TrionCube。'

Write-Output 'ComboAttackVisualPresetSmokeTests PASS'
