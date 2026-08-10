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

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$targetingSourcePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionTargetingSource.cs'
$targetingSourceText = Read-Source $targetingSourcePath

# 契约:无人接管时,远程候选目标必须回落到原版 Verb 的“现在能否命中”判定(射程+必要 LOS),
# 恢复原版“不可直视即不可选”的悬停事实;近战保持原版“先选中再接近”语义,豁免命中检查。
Assert-True (
    $targetingSourceText -match 'IsMeleeAttack\s*\|\|\s*context\.Verb\.CanHitTarget\(target\)'
) 'Unmanaged ranged targets must fall back to vanilla Verb.CanHitTarget (range + required LOS) at the neutral targeting boundary, while melee stays exempt.'

# 契约:确认边界同样回落原版 Verb 的点击校验(意识形态等),不再由适配层自造目标裁决。
Assert-True (
    $targetingSourceText -match 'IsMeleeAttack\s*\|\|\s*context\.Verb\.ValidateTarget\(target,\s*showMessages\)'
) 'Unmanaged ranged targets must fall back to vanilla Verb.ValidateTarget at the neutral confirmation boundary, while melee stays exempt.'

# 契约:模块显式接管(如毒蛇路线引导的绕行裁定)必须优先于原版回落,且只在接管时生效。
Assert-True (
    $targetingSourceText -match 'public bool CanHitTarget\(LocalTargetInfo target\)[\s\S]*?TryEvaluateCurrentTargetLegality\(context, target, false, out currentTargetLegality\)[\s\S]*?return currentTargetLegality;[\s\S]*?CanEnterNeutralTargetingBoundary\(context, target\);'
) 'Module-explicit targeting legality must take precedence over the vanilla fallback in CanHitTarget.'

Assert-True (
    $targetingSourceText -match 'public bool ValidateTarget\(LocalTargetInfo target, bool showMessages = true\)[\s\S]*?TryEvaluateCurrentTargetLegality\(context, target, showMessages, out currentTargetLegality\)[\s\S]*?CanValidateTargetAtNeutralBoundary\(context, target, showMessages\);'
) 'Module-explicit targeting legality must take precedence over the vanilla fallback in ValidateTarget.'

# 契约:dual 复合结果仍走逐侧准入,不得落入单武器回落路径(避免把单侧 LOS 误当全体真值)。
Assert-True (
    $targetingSourceText -match 'public bool CanHitTarget\(LocalTargetInfo target\)[\s\S]*?TryEvaluateDualWeaponTargetLegality\(context, target, false, false, out currentTargetLegality\)[\s\S]*?return currentTargetLegality;'
) 'Dual composite results must keep per-side legality before the neutral fallback in CanHitTarget.'

Write-Output 'TargetingNeutralBoundaryVanillaFallbackSmokeTests PASS'
