using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BDP.Trigger
{
    /// <summary>
    /// BDP远程Verb的共享基类（v4.0 B3远程修复）。
    /// 提供TryCastShotCore(Thing chipEquipment)方法，复制Verb_LaunchProjectile.TryCastShot()逻辑，
    /// 将equipmentSource替换为芯片Thing，使战斗日志显示芯片名而非触发体名。
    ///
    /// 继承链：Verb → Verb_LaunchProjectile → Verb_Shoot → Verb_BDPRangedBase
    ///   ├── Verb_BDPShoot（单发射击）
    ///   └── Verb_BDPDualRanged（双侧交替连射）
    ///
    /// 原因：Verb.EquipmentSource是非virtual属性，无法override。
    ///   原版Verb_LaunchProjectile.TryCastShot()中equipmentSource = EquipmentSource（触发体Thing），
    ///   传入Projectile.Launch()后，Projectile.equipmentDef = equipment.def（触发体ThingDef），
    ///   最终Bullet.Impact()用equipmentDef创建BattleLogEntry_RangedImpact → 显示触发体名。
    ///   本基类将equipmentSource替换为芯片Thing，使equipmentDef = 芯片ThingDef。
    /// </summary>
    public abstract class Verb_BDPRangedBase : Verb_Shoot
    {
        /// <summary>
        /// 齐射视觉偏移量（v6.1）。齐射Verb在每发循环前设置随机值，射完重置为零。
        /// 仅影响弹道视觉起点，不影响命中判定（LOS/cover用caster.Position）。
        /// </summary>
        protected Vector3 shotOriginOffset;

        /// <summary>
        /// 获取LOS检查目标（v7.0变化弹修复）。默认返回currentTarget。
        /// 引导弹子类重写此方法，返回第一个锚点而非最终目标，
        /// 因为引导弹的弹道经由锚点折线飞行，只需caster→第一锚点有LOS即可。
        /// </summary>
        protected virtual LocalTargetInfo GetLosCheckTarget()
        {
            return currentTarget;
        }

        /// <summary>
        /// 复制Verb_LaunchProjectile.TryCastShot() + Verb_Shoot.TryCastShot()逻辑，
        /// 将equipmentSource替换为chipEquipment参数。
        /// chipEquipment为null时回退到base.TryCastShot()（原版逻辑）。
        /// </summary>
        protected bool TryCastShotCore(Thing chipEquipment)
        {
            // 无芯片上下文时回退到原版
            if (chipEquipment == null)
                return base.TryCastShot();

            // ── 以下复制自Verb_LaunchProjectile.TryCastShot() ──

            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
                return false;

            ThingDef projectileDef = Projectile;
            if (projectileDef == null)
                return false;

            // v7.0修复：引导弹只需检查caster→第一锚点的LOS，而非caster→最终目标
            LocalTargetInfo losTarget = GetLosCheckTarget();
            bool hasLos = TryFindShootLineFromTo(caster.Position, losTarget, out ShootLine resultingLine);
            if (verbProps.stopBurstWithoutLos && !hasLos)
                return false;

            // 通知原始装备（触发体）的组件——这些是装备级组件，不应用芯片
            if (base.EquipmentSource != null)
            {
                base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
                base.EquipmentSource.GetComp<CompApparelVerbOwner_Charged>()?.UsedOnce();
            }

            lastShotTick = Find.TickManager.TicksGame;

            Thing manningPawn = caster;
            // ── B3核心修复：使用芯片Thing作为equipmentSource ──
            Thing equipmentSource = chipEquipment;

            CompMannable compMannable = caster.TryGetComp<CompMannable>();
            if (compMannable?.ManningPawn != null)
            {
                manningPawn = compMannable.ManningPawn;
                equipmentSource = caster;
            }

            Vector3 drawPos = caster.DrawPos + shotOriginOffset;
            Projectile proj = (Projectile)GenSpawn.Spawn(projectileDef, resultingLine.Source, caster.Map);

            // CompUniqueWeapon：damageDefOverride + extraDamages（芯片通常无此组件）
            if (equipmentSource.TryGetComp(out CompUniqueWeapon uniqueWeapon))
            {
                foreach (WeaponTraitDef trait in uniqueWeapon.TraitsListForReading)
                {
                    if (trait.damageDefOverride != null)
                        proj.damageDefOverride = trait.damageDefOverride;
                    if (!trait.extraDamages.NullOrEmpty())
                    {
                        if (proj.extraDamages == null)
                            proj.extraDamages = new List<ExtraDamage>();
                        proj.extraDamages.AddRange(trait.extraDamages);
                    }
                }
            }

            // ForcedMissRadius处理
            if (verbProps.ForcedMissRadius > 0.5f)
            {
                float missRadius = verbProps.ForcedMissRadius;
                if (manningPawn is Pawn p)
                    missRadius *= verbProps.GetForceMissFactorFor(equipmentSource, p);
                float adjustedMiss = VerbUtility.CalculateAdjustedForcedMiss(missRadius, currentTarget.Cell - caster.Position);
                if (adjustedMiss > 0.5f)
                {
                    IntVec3 forcedMissTarget = GetForcedMissTarget(adjustedMiss);
                    if (forcedMissTarget != currentTarget.Cell)
                    {
                        ProjectileHitFlags flags = ProjectileHitFlags.NonTargetWorld;
                        if (Rand.Chance(0.5f))
                            flags = ProjectileHitFlags.All;
                        if (!canHitNonTargetPawnsNow)
                            flags &= ~ProjectileHitFlags.NonTargetPawns;
                        proj.Launch(manningPawn, drawPos, forcedMissTarget, currentTarget, flags, preventFriendlyFire, equipmentSource);
                        OnProjectileLaunched(proj);
                        IncrementShotsFired();
                        return true;
                    }
                }
            }

            // 命中判定
            ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
            Thing coverThing = shotReport.GetRandomCoverToMissInto();
            ThingDef targetCoverDef = coverThing?.def;

            // 偏射（wild miss）
            if (verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
            {
                bool flyOverhead = proj?.def?.projectile != null && proj.def.projectile.flyOverhead;
                resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget, flyOverhead, caster.Map);
                ProjectileHitFlags flags2 = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
                    flags2 |= ProjectileHitFlags.NonTargetPawns;
                proj.Launch(manningPawn, drawPos, resultingLine.Dest, currentTarget, flags2, preventFriendlyFire, equipmentSource, targetCoverDef);
                OnProjectileLaunched(proj);
                IncrementShotsFired();
                return true;
            }

            // 掩体命中（cover miss）
            if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
            {
                ProjectileHitFlags flags3 = ProjectileHitFlags.NonTargetWorld;
                if (canHitNonTargetPawnsNow)
                    flags3 |= ProjectileHitFlags.NonTargetPawns;
                proj.Launch(manningPawn, drawPos, coverThing, currentTarget, flags3, preventFriendlyFire, equipmentSource, targetCoverDef);
                OnProjectileLaunched(proj);
                IncrementShotsFired();
                return true;
            }

            // 命中目标
            ProjectileHitFlags flags4 = ProjectileHitFlags.IntendedTarget;
            if (canHitNonTargetPawnsNow)
                flags4 |= ProjectileHitFlags.NonTargetPawns;
            if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
                flags4 |= ProjectileHitFlags.NonTargetWorld;

            if (currentTarget.Thing != null)
                proj.Launch(manningPawn, drawPos, currentTarget, currentTarget, flags4, preventFriendlyFire, equipmentSource, targetCoverDef);
            else
                proj.Launch(manningPawn, drawPos, resultingLine.Dest, currentTarget, flags4, preventFriendlyFire, equipmentSource, targetCoverDef);

            OnProjectileLaunched(proj);
            IncrementShotsFired();
            return true;
        }

        /// <summary>
        /// 弹道发射后回调（v7.0变化弹）。子类可重写此方法对刚发射的弹道进行后处理，
        /// 例如附加 GuidedFlightController 实现引导飞行。
        /// </summary>
        protected virtual void OnProjectileLaunched(Projectile proj) { }

        /// <summary>
        /// 重写OrderForceTarget：使用BDP_ChipRangedAttack替代默认的AttackStatic。
        /// 原因：AttackStatic的JobDriver调用pawn.TryStartAttack()，
        ///   该方法通过TryGetAttackVerb重新查找verb，忽略job.verbToUse，
        ///   返回触发体的"柄"近战verb而非芯片远程verb，导致不射击。
        /// BDP_ChipRangedAttack直接调用job.verbToUse.TryStartCastOn()，
        /// 且具有与AttackStatic相同的持续攻击循环（tickIntervalAction）。
        /// </summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (CasterPawn == null) return;

            // 防御性重置：如果verb因burst中断卡在Bursting状态，先重置
            // 原因：芯片verb不在VerbTracker.AllVerbs中，BurstingTick不被调用，
            //   若上一次burst被job中断，state会永久卡在Bursting
            if (Bursting)
                Reset();

            // 最小射程检查（复制自Verb.OrderForceTarget）
            float minRange = verbProps.EffectiveMinRange(target, CasterPawn);
            if ((float)CasterPawn.Position.DistanceToSquared(target.Cell) < minRange * minRange
                && CasterPawn.Position.AdjacentTo8WayOrInside(target.Cell))
            {
                Messages.Message("MessageCantShootInMelee".Translate(), CasterPawn,
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            // BDP_ChipRangedAttack: 持续攻击循环 + 直接使用job.verbToUse
            Job job = JobMaker.MakeJob(BDP_DefOf.BDP_ChipRangedAttack);
            job.verbToUse = this;
            job.targetA = target;
            job.endIfCantShootInMelee = true;
            CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        /// <summary>增加ShotsFired记录（复制自Verb_Shoot.TryCastShot）。</summary>
        private void IncrementShotsFired()
        {
            if (CasterIsPawn)
                CasterPawn.records.Increment(RecordDefOf.ShotsFired);
        }

        /// <summary>
        /// 从芯片配置读取WeaponChipConfig（三级回退策略）。
        /// 供引导弹子类（Verb_BDPGuided/Verb_BDPGuidedVolley）共用。
        /// 回退顺序：侧别label精确定位 → ActivatingSlot → 遍历所有激活槽位。
        /// </summary>
        protected WeaponChipConfig GetChipConfig()
        {
            var pawn = CasterPawn;
            if (pawn == null) return null;
            var triggerComp = pawn.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return null;

            // 优先通过侧别label精确定位
            SlotSide? side = DualVerbCompositor.ParseSideLabel(verbProps?.label);
            if (side.HasValue)
            {
                var sideSlot = triggerComp.GetActiveSlot(side.Value);
                var cfg = sideSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg;
            }
            // 回退：ActivatingSlot
            var slot = triggerComp.ActivatingSlot;
            if (slot?.loadedChip != null)
            {
                var cfg = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg;
            }
            // 最终回退：遍历
            foreach (var activeSlot in triggerComp.AllActiveSlots())
            {
                var cfg = activeSlot.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
                if (cfg != null) return cfg;
            }
            return null;
        }
    }
}
