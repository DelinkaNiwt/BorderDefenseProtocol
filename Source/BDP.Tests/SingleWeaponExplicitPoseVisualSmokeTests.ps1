$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

# 定位主模组视觉预设与视觉投影实现。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'
$presetPath = Join-Path $bdpSourceRoot 'Core\Expressions\Config\ExpressionVisualPresetDef.cs'
$builderPath = Join-Path $bdpSourceRoot 'Core\Expressions\Projection\DefaultVisualProjectionBuilder.cs'

Assert-True (Test-Path -LiteralPath $presetPath) 'ExpressionVisualPresetDef 必须存在。'
Assert-True (Test-Path -LiteralPath $builderPath) 'DefaultVisualProjectionBuilder 必须存在。'

$presetText = Get-Content -Raw -Encoding utf8 -LiteralPath $presetPath
$builderText = Get-Content -Raw -Encoding utf8 -LiteralPath $builderPath

# 视觉预设必须直接表达作者是否显式声明姿态，不增加重复布尔配置字段。
Assert-True (
    $presetText -match 'bool\s+HasExplicitPose\s*=>\s*SouthNorthPose\s*!=\s*null\s*\|\|\s*EastWestPose\s*!=\s*null'
) '视觉预设必须公开只读的显式姿态语义。'

# 构建视觉投影时必须只解析一次关系，并把同一结果传给绘制模式选择。
Assert-True (
    ($builderText -match 'using\s+Verse;') -and
    ($builderText -match 'VisualExpressionRelationKind\s+relationKind\s*=\s*ResolveRelationKind') -and
    ($builderText -match 'ResolveHostEquipmentRenderMode\(\s*residentEntries,\s*activeWeaponChipInstanceCount,\s*relationKind\s*\)')
) '视觉投影必须先确定关系，再用最终预设决定单武器绘制模式。'

# 普通关系读取普通预设，组合或双武器关系优先复合预设。
Assert-True (
    ($builderText -match 'ResolveVisualPresetDefName') -and
    ($builderText -match 'relationKind\s*!=\s*VisualExpressionRelationKind\.SingleSide') -and
    ($builderText -match 'CompositeVisualPresetDefName') -and
    ($builderText -match 'DefDatabase<ExpressionVisualPresetDef>\.GetNamed') -and
    ($builderText -match '\.HasExplicitPose')
) '单武器姿态判断必须按普通或复合关系选出最终 Def，并读取其显式姿态。'

# 单武器只有在最终视觉预设显式声明姿态时才升级到完整替换。
$singleWeaponBranch = [regex]::Match(
    $builderText,
    '(?s)if\s*\(\s*activeWeaponChipInstanceCount\s*==\s*1\s*\)\s*\{.*?\}'
).Value

Assert-True (
    ($singleWeaponBranch -match 'HostEquipmentRenderMode\.Replace') -and
    ($singleWeaponBranch -match 'HostEquipmentRenderMode\.ReplaceTextureOnly')
) '单武器必须只在最终预设显式声明姿态时升级为完整替换。'

# 完整姿态只改变绘制入口，不把单武器带入执行焦点或枪口跟随。
Assert-True (
    ($builderText -match 'ResolveExecutionFocusPolicy') -and
    ($builderText -match 'ResolveMuzzleFollowPolicy') -and
    ($builderText -match 'activeWeaponChipInstanceCount\s*==\s*1[\s\S]*VisualExecutionFocusPolicy\.None') -and
    ($builderText -match 'activeWeaponChipInstanceCount\s*==\s*1[\s\S]*VisualMuzzleFollowPolicy\.None')
) '单武器完整姿态不得顺带开启执行焦点或枪口跟随。'

Write-Output 'SingleWeaponExplicitPoseVisualSmokeTests PASS'
