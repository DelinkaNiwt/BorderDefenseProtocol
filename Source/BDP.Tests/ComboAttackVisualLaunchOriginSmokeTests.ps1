$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

# 按 UTF-8 读取源码，避免中文注释和路径判断受系统代码页影响。
function Get-Utf8Text
{
    param([string]$Path)
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$coreRoot = Join-Path $sourceRoot 'BDP'
$effectiveResolverPath = Join-Path $coreRoot 'Core\Trigger\Visual\TriggerVisualEffectiveEntryResolver.cs'
$drawPatchPath = Join-Path $coreRoot 'Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'
$launchResolverPath = Join-Path $coreRoot 'Core\Trigger\Visual\TriggerVisualLaunchOriginResolver.cs'

foreach ($path in @($effectiveResolverPath, $drawPatchPath, $launchResolverPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少预期文件：$path"
}

$effectiveResolverText = Get-Utf8Text $effectiveResolverPath
$drawPatchText = Get-Utf8Text $drawPatchPath
$launchResolverText = Get-Utf8Text $launchResolverPath

Assert-True ($effectiveResolverText -match 'class TriggerVisualEffectiveEntryResolver') `
    '必须存在共享的有效视觉条目解析器。'
Assert-True (($effectiveResolverText -match 'ComboAttackVisualBindings') -and
    ($effectiveResolverText -match 'WeaponVisualActionStage\.Idle') -and
    ($effectiveResolverText -match 'ComboResultId')) `
    '共享解析器必须按非待命阶段匹配组合技绑定，并生成组合结果身份。'
Assert-True ($effectiveResolverText -match 'CreateComboAttackVisualEntry') `
    '共享解析器必须沿用绘制入口现有的组合技临时视觉条目语义。'
Assert-True ($effectiveResolverText -match 'TryResolveLaunch') `
    '发射根点必须有允许任意组合技来源复用 Owner 最终视觉位姿的解析入口。'
Assert-True ($drawPatchText -match 'TriggerVisualEffectiveEntryResolver\.TryResolve') `
    '武器绘制入口必须消费共享有效视觉条目解析器。'
Assert-True ($launchResolverText -match 'TriggerVisualEffectiveEntryResolver\.FindLaunchSourceEntry') `
    '发射根点入口必须支持从组合结果标识回溯到组合技来源绘制条目。'
Assert-True ($launchResolverText -match 'TriggerVisualEffectiveEntryResolver\.TryResolve') `
    '发射根点入口必须消费共享有效视觉条目解析器。'
Assert-True ($launchResolverText -match 'CasterDrawPosFallback') `
    '发射根点解析失败时必须保留 Pawn.DrawPos 诊断回退。'

Write-Output 'ComboAttackVisualLaunchOriginSmokeTests PASS'
