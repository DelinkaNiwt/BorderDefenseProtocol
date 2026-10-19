# BDP 近战连续多击修复回归测试。
# 这些测试锁定原版 Verb 生命周期接入边界，防止“配置显示为三击、运行时仍只打一击”再次出现。

$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$meleeVerbPath = Join-Path $sourceRoot 'BDP\Core\Verbs\BdpVerb_MeleeAttackDamage.cs'
$formalHostMeleePath = Join-Path $sourceRoot 'BDP\Core\Verbs\BdpVerb_FormalHostMelee.cs'
$burstPatchPath = Join-Path $sourceRoot 'BDP\Patches\Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess.cs'
$meleeActionPatchPath = Join-Path $sourceRoot 'BDP\Patches\Patch_Verb_MeleeAttack_TryCastShot_BdpAttackActionSuccess.cs'

foreach ($path in @($meleeVerbPath, $formalHostMeleePath, $burstPatchPath, $meleeActionPatchPath)) {
    Assert-True (Test-Path -LiteralPath $path) "缺少近战多击修复相关文件：$path"
}

$meleeVerbText = Get-Content -LiteralPath $meleeVerbPath -Raw -Encoding UTF8
$formalHostMeleeText = Get-Content -LiteralPath $formalHostMeleePath -Raw -Encoding UTF8
$burstPatchText = Get-Content -LiteralPath $burstPatchPath -Raw -Encoding UTF8
$meleeActionPatchText = Get-Content -LiteralPath $meleeActionPatchPath -Raw -Encoding UTF8

# 原版近战默认 ShotsPerBurst 为 1；BDP 必须显式接到当前 Verb 的 BurstShotCount。
Assert-True (
    $meleeVerbText -match 'protected override int ShotsPerBurst\s*=>\s*BurstShotCount'
) 'BDP 近战必须把原版 ShotsPerBurst 接到 BurstShotCount。'

# miss/dodge（未命中/闪避）要继续消耗 burst，但真实命中结果必须单独保存。
Assert-True ($meleeVerbText -match 'LastMeleeBurstShotHit') 'BDP 近战必须保存本次实际命中结果。'
Assert-True ($meleeVerbText -match 'LastMeleeBurstShotCompleted') 'BDP 近战必须区分攻击是否真正完成。'
Assert-True (
    $meleeVerbText -match 'return\s+hitResult\s*\|\|\s*LastMeleeBurstShotCompleted'
) '合法的未命中/闪避不能让原版 burst 提前结束。'
Assert-True ($meleeVerbText -match 'FullBodyBusy') '近战继续 burst 前必须排除身体被占用的硬失败。'
Assert-True ($meleeVerbText -match 'IsOwnBurstCooldownStance') '近战必须识别属于当前 Verb 的连击间隔冷却姿态。'
Assert-True ($meleeVerbText -match 'FullBodyBusy\s*&&\s*!ownBurstCooldown') '只有当前 Verb 自己的间隔冷却可以放行，其他身体忙碌状态必须拒绝。'
Assert-True ($meleeVerbText -match 'temporaryStance\s*=\s*new Stance_Mobile') '调用原版近战判定时必须准备临时移动姿态。'
Assert-True ($meleeVerbText -match 'curStance\s*=\s*temporaryStance') '调用原版近战判定时必须临时解除自有冷却姿态的忙碌标记。'
Assert-True ($meleeVerbText -match 'stunner\.Stunned') '近战不得在眩晕状态下放行自有冷却姿态。'

# 为了不把“继续 burst”误报为“命中”，统一 burst 通知必须读取真实命中结果。
Assert-True ($burstPatchText -match 'LastMeleeBurstShotHit') 'burst 通知补丁必须读取 BDP 近战真实命中结果。'
Assert-True ($burstPatchText -match 'LastMeleeBurstShotCompleted') 'burst 通知补丁必须读取 BDP 近战攻击完成状态。'
Assert-True ($meleeActionPatchText -match 'FromWeapon\(__instance,\s*false\)') '近战未命中/闪避通知必须保留 IsHit=false 语义。'

# formal host（正式宿主壳）复用同一个 Verb，切换表面后必须清理原版两项惰性缓存。
Assert-True ($formalHostMeleeText -match 'cachedTicksBetweenBurstShots') '近战 formal host 必须清理 burst 间隔缓存。'
Assert-True ($formalHostMeleeText -match 'cachedBurstShotCount') '近战 formal host 必须清理 burst 次数缓存。'
Assert-True ($formalHostMeleeText -match 'InvalidateVanillaVerbPropertyCaches\(') '近战 formal host 必须集中处理原版 Verb 缓存失效。'

Write-Output 'MeleeBurstContinuationFixSmokeTests PASS'
