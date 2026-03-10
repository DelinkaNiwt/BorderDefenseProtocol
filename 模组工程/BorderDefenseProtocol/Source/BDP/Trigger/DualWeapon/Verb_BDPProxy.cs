using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 代理Verb（v14.0自动攻击支持）。
    ///
    /// 用途：使引擎自动攻击系统（Pawn.TryGetAttackVerb）能找到芯片远程Verb。
    ///
    /// 设计：
    /// - hasStandardCommand=false：不生成Gizmo（芯片Gizmo由Command_BDPChipAttack生成）
    /// - Available()：检查ownerTriggerComp有无非近战的主攻击芯片Verb
    /// - TryStartCastOn()：委托给芯片Verb的TryStartCastOn，进入Stance_Warmup
    /// - 不重写TryCastShot：永远不会被调用（TryStartCastOn已委托，真正射击在芯片Verb中）
    ///
    /// 委托规则（见方案文档）：
    /// - 无武器芯片 → Available=false → 引擎回退到"柄"近战
    /// - 仅近战芯片 → Available=false → 引擎回退到"柄"近战
    /// - 1远程 → 委托到leftHandAttackVerb或rightHandAttackVerb
    /// - 2远程 → 委托到dualAttackVerb
    /// - 1近战+1远程 → 委托到远程侧Verb（isPrimary=true）
    ///
    /// VerbTick：
    /// - ProxyVerb本身不需要tick（无warmup/cooldown状态）
    /// - 芯片Verb的tick由Patch_VerbTracker_VerbsTick驱动
    /// </summary>
    public class Verb_BDPProxy : Verb_Shoot
    {
        // ── 运行时引用（SyncFrom设置） ──
        private CompTriggerBody ownerTriggerComp;

        /// <summary>
        /// 从芯片Verb同步verbProps（v14.0）。
        /// 复制range、projectile、warmup等属性，使引擎射程判断正确。
        /// </summary>
        public void SyncFrom(Verb chipVerb, CompTriggerBody triggerComp)
        {
            ownerTriggerComp = triggerComp;

            if (chipVerb?.verbProps == null) return;

            // 复制关键属性（引擎AI用于射程判断）
            if (verbProps == null)
                verbProps = new VerbProperties();

            verbProps.range = chipVerb.verbProps.range;
            verbProps.minRange = chipVerb.verbProps.minRange;
            verbProps.burstShotCount = chipVerb.verbProps.burstShotCount;
            verbProps.ticksBetweenBurstShots = chipVerb.verbProps.ticksBetweenBurstShots;
            verbProps.warmupTime = chipVerb.verbProps.warmupTime;
            verbProps.defaultCooldownTime = chipVerb.verbProps.defaultCooldownTime;
            verbProps.defaultProjectile = chipVerb.verbProps.defaultProjectile;
            verbProps.soundCast = chipVerb.verbProps.soundCast;
            verbProps.muzzleFlashScale = chipVerb.verbProps.muzzleFlashScale;

            // 关键标志
            verbProps.isPrimary = false; // ProxyVerb不是主Verb（避免被VerbTracker.PrimaryVerb拾取）
            verbProps.hasStandardCommand = false; // 不生成Gizmo
        }

        /// <summary>
        /// 可用性检查（v14.0）。
        /// 返回false时引擎回退到"柄"近战Verb。
        /// </summary>
        public override bool Available()
        {
            if (ownerTriggerComp == null) return false;

            var primary = ownerTriggerComp.GetPrimaryChipVerb();
            // 无武器芯片或仅近战芯片 → 不可用
            if (primary == null || primary.IsMeleeAttack) return false;

            // 委托给芯片Verb的Available检查
            return primary.Available();
        }

        /// <summary>
        /// 委托TryStartCastOn到芯片Verb（v14.0）。
        /// 芯片Verb进入Stance_Warmup，warmup结束后触发TryCastShot射击。
        /// </summary>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            if (ownerTriggerComp == null) return false;

            var primary = ownerTriggerComp.GetPrimaryChipVerb();
            if (primary == null || primary.IsMeleeAttack) return false;

            // 直接委托给芯片Verb
            return primary.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }

        // 不重写TryCastShot：永远不会被调用（TryStartCastOn已委托）
    }
}

