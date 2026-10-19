# 统一远程目标合法性回归测试。
# 目标是防止原版 Targeter（目标器）对 Pawn 的特殊路径绕过 BDP 的视线许可判断。

$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$targetingPath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\AttackExecutionTargetingSource.cs'
$routePath = Join-Path $sourceRoot 'BDP.Content\RangedModules\RoutePath\RoutePathModule.cs'
$permissionPath = Join-Path $sourceRoot 'BDP\Core\Permissions\DirectTargetLineOfSightPermissionAccess.cs'

foreach ($path in @($targetingPath, $routePath, $permissionPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少统一目标合法性相关文件：$path"
}

$targeting = Get-Content -Raw -LiteralPath $targetingPath -Encoding UTF8
$route = Get-Content -Raw -LiteralPath $routePath -Encoding UTF8
$permission = Get-Content -Raw -LiteralPath $permissionPath -Encoding UTF8

$canHitBlock = [regex]::Match($targeting, '(?s)public bool CanHitTarget\(LocalTargetInfo target\).*?(?=public bool ValidateTarget\()')
$validateBlock = [regex]::Match($targeting, '(?s)public bool ValidateTarget\(LocalTargetInfo target, bool showMessages = true\).*?(?=///\s*绘制当前目标高亮)')
$onGuiBlock = [regex]::Match($targeting, '(?s)public void OnGUI\(LocalTargetInfo target\).*?(?=///\s*解析当前读取时刻)')
$unifiedMethod = [regex]::Match($targeting, '(?s)private bool TryEvaluateUnifiedTargetLegality\(.*?\n\s*}\n\n\s*///')

Assert-True ($unifiedMethod.Success) '目标源必须提供一个统一远程目标合法性判断方法。'
Assert-True ($canHitBlock.Success -and $canHitBlock.Value -match 'TryEvaluateUnifiedTargetLegality') 'CanHitTarget 必须调用统一目标合法性判断方法。'
Assert-True ($validateBlock.Success -and $validateBlock.Value -match 'TryEvaluateUnifiedTargetLegality') 'ValidateTarget 必须调用统一目标合法性判断方法。'
Assert-True ($onGuiBlock.Success -and $onGuiBlock.Value -match 'TryEvaluateUnifiedTargetLegality') 'OnGUI 必须调用统一目标合法性判断方法，不能依赖原版跳过 Pawn 的路径。'
Assert-True ($unifiedMethod.Value -match 'TryEvaluateCurrentTargetLegality' -and $unifiedMethod.Value -match 'CanEnterNeutralTargetingBoundary') '统一方法必须先保留模块显式裁定，再回落中性远程目标规则。'
Assert-True ($unifiedMethod.Value -match 'showMessages') '统一方法必须保留确认阶段的拒绝消息语义。'
Assert-True ($permission -match 'ShouldRelaxDirectTargetLineOfSight') '统一目标判断必须继续读取动态直视许可。'
Assert-True ($route -match 'DirectTargetLineOfSight|TryValidateCurrentSegmentLineOfSight|TryResolveAutoRouteForFinalTarget') '路线模块必须继续保留自己的路线/段级合法性，不得被普通直视规则取代。'

Write-Host 'UnifiedRangedTargetLegality PASS'
