# 芯片表达能力判定与正规化契约读取面的边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$resolverPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Capability\ChipExpressionCapabilityResolver.cs"
$surfacePath = Join-Path $modRoot "Source\BDP\Core\Expressions\Access\Surfaces\ChipExpressionContractSurfaceAccess.cs"
$interpreterPath = Join-Path $modRoot "Source\BDP\Core\Expressions\Contract\ChipExpressionContractInterpreter.cs"
$validatorPath = Join-Path $modRoot "Source\BDP\Core\Chips\Validation\ChipDefinitionValidator.cs"
$informationAdapterPath = Join-Path $modRoot "Source\BDP\Core\Information\Input\ChipInformationDefinitionInputAdapter.cs"
$assemblyBuilderPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Window\ChipAssemblyDetailModelBuilder.cs"
$manufacturingLookupPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipManufacturingDefLookup.cs"

foreach ($path in @(
    $resolverPath,
    $surfacePath,
    $interpreterPath,
    $validatorPath,
    $informationAdapterPath,
    $assemblyBuilderPath,
    $manufacturingLookupPath
)) {
    Assert-True (Test-Path -LiteralPath $path) ("缺少路线 C 需要的文件：" + $path)
}

$resolverText = Get-Utf8Text $resolverPath
$surfaceText = Get-Utf8Text $surfacePath
$interpreterText = Get-Utf8Text $interpreterPath
$validatorText = Get-Utf8Text $validatorPath
$informationAdapterText = Get-Utf8Text $informationAdapterPath
$assemblyBuilderText = Get-Utf8Text $assemblyBuilderPath
$manufacturingLookupText = Get-Utf8Text $manufacturingLookupPath

foreach ($member in @(
    "IsVerbEntry",
    "ResolveWeaponMode",
    "HasExecutableMeleeSurface",
    "HasExecutableRangedSurface",
    "IsAugmentationOnly"
)) {
    Assert-True ($resolverText -match ("public\s+static\s+.*" + $member + "\s*\(")) `
        ("能力解析器缺少统一成员：" + $member)
}

Assert-True ($resolverText -match "RangedModuleAugmentations") `
    "能力解析器必须明确读取远程增强声明。"
Assert-True ($resolverText -match "defaultProjectile") `
    "能力解析器必须以投射物事实判定可执行远程表面。"
Assert-True ($resolverText -match "DeclaredMeleeToolSurfaces|DeclaredTools|\.Tool") `
    "能力解析器必须以 Tool（近战工具）事实判定可执行近战表面。"
Assert-True ($resolverText -notmatch "BDP_ChipCategory_|Profession|defName|Label") `
    "能力解析器不得依赖芯片分类、职业、名称或标签。"

Assert-True ($surfaceText -match "public\s+static\s+class\s+ChipExpressionContractSurfaceAccess") `
    "必须存在公开的表达契约只读读取面。"
Assert-True ($surfaceText -match "new\s+ChipExpressionContractInterpreter\(\)") `
    "读取面必须由内部解释器提供正规化结果。"
Assert-True ($surfaceText -match "ResolveWithConfig") `
    "读取面必须支持制造期合并配置的读取。"
Assert-True ($surfaceText -notmatch "ExpressionContractCache|ExpressionRuntimeRepository|Window_|Panel_") `
    "公开读取面不得泄漏缓存、内部运行时仓库或界面类型。"

Assert-True ($interpreterText -match "ChipExpressionCapabilityResolver\.ResolveWeaponMode") `
    "表达解释器必须复用统一武器模式解析。"
Assert-True ($validatorText -match "ChipExpressionCapabilityResolver\.ResolveWeaponMode") `
    "芯片定义验证器必须复用统一武器模式解析。"
Assert-True ($informationAdapterText -match "ChipExpressionCapabilityResolver\.ResolveWeaponMode") `
    "信息输入适配器必须复用统一武器模式解析。"
Assert-True ($assemblyBuilderText -match "ChipExpressionCapabilityResolver\.HasExecutableRangedSurface") `
    "装配台基线选择必须使用可执行远程能力，而不是只看分类字段。"
Assert-True ($manufacturingLookupText -match "ChipExpressionCapabilityResolver\.HasExecutableRangedSurface") `
    "制造查找必须使用可执行远程能力，而不是只看显式模式。"

Write-Output 'ChipExpressionCapabilityBoundarySmokeTests PASS'
