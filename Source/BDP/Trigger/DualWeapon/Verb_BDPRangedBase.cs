using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BDP.Trigger
{
    /// <summary>
    /// BDP远程Verb的共享基类（v4.0 B3远程修复，v8.0 PMS重构）。
    /// 提供TryCastShotCore(Thing chipEquipment)方法，复制Verb_LaunchProjectile.TryCastShot()逻辑，
    /// 将equipmentSource替换为芯片Thing，使战斗日志显示芯片名而非触发体名。
    ///
    /// PMS重构：引导弹逻辑从Verb_BDPGuided/Verb_BDPGuidedVolley上提到基类，
    /// 通过SupportsGuided属性条件化启用，消除引导弹专用Verb子类。
    ///
    /// 继承链：Verb → Verb_LaunchProjectile → Verb_Shoot → Verb_BDPRangedBase
    ///   ├── Verb_BDPShoot（单发射击）
    ///   ├── Verb_BDPVolley（单侧齐射）
    ///   ├── Verb_BDPDualRanged（双侧交替连射）
    ///   └── Verb_BDPDualVolley（双侧齐射）
    /// </summary>
    public abstract class Verb_BDPRangedBase : Verb_Shoot
    {
        // ── Fix-5：CompTriggerBody缓存（避免每发子弹多次TryGetComp线性搜索） ──
        private CompTriggerBody cachedTriggerComp;
        private Pawn cachedTriggerPawn;

        /// <summary>
        /// 获取CasterPawn装备的触发体CompTriggerBody（缓存版本）。
        /// Pawn变化时自动刷新缓存。
        /// </summary>
        /// <summary>
        /// 序列化BDP Verb扩展状态：
        /// 1. 占位VerbProperties防止BuggedAfterLoading判定
        /// 2. VerbFlightState引导弹状态（锚点、目标、双侧标记）
        ///    原因：引导弹的LOS检查重定向到第一锚点而非最终目标，
        ///    若ManualAnchorsActive丢失，读档后verb直接对最终目标做LOS→失败→无法射击。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars && verbProps == null)
                verbProps = new VerbProperties();

            // ── VerbFlightState序列化 ──
            Scribe_Values.Look(ref gs.ManualAnchorsActive, "gs_manualAnchorsActive");
            Scribe_Values.Look(ref gs.ManualTargetCell, "gs_manualTargetCell");
            Scribe_Values.Look(ref gs.CachedAnchorSpread, "gs_anchorSpread");
            Scribe_Collections.Look(ref gs.RawAnchors, "gs_rawAnchors", LookMode.Value);
            // LocalTargetInfo需要Scribe_TargetInfo
            var rawFinal = gs.RawFinalTarget;
            Scribe_TargetInfo.Look(ref rawFinal, "gs_rawFinalTarget");
            gs.RawFinalTarget = rawFinal;
            // 双侧专用
            var savedThing = gs.SavedThingTarget;
            Scribe_TargetInfo.Look(ref savedThing, "gs_savedThingTarget");
            gs.SavedThingTarget = savedThing;
            Scribe_Values.Look(ref gs.LeftHasPath, "gs_leftHasPath");
            Scribe_Values.Look(ref gs.RightHasPath, "gs_rightHasPath");
            Scribe_Values.Look(ref gs.CurrentShotHasPath, "gs_currentShotHasPath");
        }

        protected CompTriggerBody GetTriggerComp()
        {
            var pawn = CasterPawn;
            if (pawn == null) { cachedTriggerComp = null; cachedTriggerPawn = null; return null; }
            if (pawn != cachedTriggerPawn || cachedTriggerComp == null)
            {
                cachedTriggerPawn = pawn;
                cachedTriggerComp = pawn.equipment?.Primary?.TryGetComp<CompTriggerBody>();
            }
            return cachedTriggerComp;
        }

        /// <summary>
        /// 齐射视觉偏移量（v6.1）。齐射Verb在每发循环前设置随机值，射完重置为零。
        /// 仅影响弹道视觉起点，不影响命中判定（LOS/cover用caster.Position）。
        /// </summary>
        protected Vector3 shotOriginOffset;

        /// <summary>
        /// 获取LOS检查目标。引导模式下返回第一个锚点而非最终目标，
        /// 因为引导弹的弹道经由锚点折线飞行，只需caster→第一锚点有LOS即可。
        /// </summary>
        protected virtual LocalTargetInfo GetLosCheckTarget()
        {
            return gs.GetLosCheckTarget(currentTarget);
        }

        /// <summary>引导模式下用第一个锚点替代最终目标进行LOS检查。</summary>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            ThingDef routeProjectile = GetAutoRouteProjectileDef();
            gs.PrepareAutoRouteForCast(caster.Position, castTarg, caster.Map, routeProjectile);
            gs.InterceptCastTarget(ref castTarg, caster.Position, caster.Map);

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            if (result)
                gs.PostCastOn(ref currentTarget);

            return result;
        }

        /// <summary>
        /// 自动绕行判定用的投射物Def。默认取当前芯片的首个投射物，子类可按双侧规则重写。
        /// </summary>
        protected virtual ThingDef GetAutoRouteProjectileDef()
            => GetChipConfig()?.GetFirstProjectileDef() ?? Projectile;

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
        /// 弹道发射后回调（v7.0变化弹）。子类可重写此方法对刚发射的弹道进行后处理。
        /// 基类默认行为：引导模式下通过GuidedModule附加折线路径。
        /// </summary>
        protected virtual void OnProjectileLaunched(Projectile proj)
        {
            if (gs.ManualAnchorsActive)
                gs.AttachManualFlight(proj);
            else
                gs.AttachAutoRouteFlight(proj, gs.ResolveAutoRouteFinalTarget(currentTarget),
                    GetChipConfig()?.anchorSpread ?? 0.3f);
        }

        // ── PMS重构：引导弹支持（从Verb_BDPGuided上提） ──

        /// <summary>引导弹共享状态（PMS重构：从子类上提到基类）。</summary>
        protected readonly VerbFlightState gs = new VerbFlightState();

        /// <summary>当前芯片是否支持变化弹（引导飞行）。</summary>
        public virtual bool SupportsGuided => GetChipConfig()?.supportsGuided == true;

        /// <summary>
        /// 启动多步锚点瞄准（由Command_BDPChipAttack.GizmoOnGUIInt调用）。
        /// 不支持引导时回退到普通targeting。
        /// </summary>
        public virtual void StartAnchorTargeting()
        {
            var cfg = GetChipConfig();
            if (cfg == null || !cfg.supportsGuided)
            {
                Find.Targeter.BeginTargeting(this);
                return;
            }

            AnchorTargetingHelper.BeginAnchorTargeting(
                this, CasterPawn, cfg.maxAnchors, verbProps.range,
                (anchors, finalTarget) =>
                {
                    gs.StoreTargetingResult(anchors, finalTarget, cfg.anchorSpread);
                    // 直接调用OrderForceTargetCore创建BDP_ChipRangedAttack job，
                    // 而非BaseOrderForceTarget（原版Verb.OrderForceTarget创建的标准Job
                    // 无法驱动脱离VerbTracker的芯片Verb）
                    OrderForceTargetCore(finalTarget);
                });
        }

        /// <summary>
        /// 重写OrderForceTarget：引导弹时启动锚点瞄准，否则使用BDP_ChipRangedAttack job。
        /// </summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (CasterPawn == null) return;

            // 引导弹：启动多步锚点瞄准
            if (SupportsGuided)
            {
                StartAnchorTargeting();
                return;
            }
            gs.ManualAnchorsActive = false;

            OrderForceTargetCore(target);
        }

        /// <summary>
        /// OrderForceTarget核心逻辑（不含引导弹判断）。
        /// 供子类在已处理引导逻辑后直接调用。
        /// </summary>
        protected void OrderForceTargetCore(LocalTargetInfo target)
        {
            if (CasterPawn == null) return;

            // 防御性重置：如果verb因burst中断卡在Bursting状态，先重置
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

        // ── v9.0 FireMode辅助 ──

        /// <summary>获取芯片上的CompFireMode（无则返回null）。burst注入仍需要此方法。</summary>
        protected static CompFireMode GetFireMode(Thing chipThing)
            => chipThing?.TryGetComp<CompFireMode>();

        /// <summary>增加ShotsFired记录（复制自Verb_Shoot.TryCastShot）。</summary>
        private void IncrementShotsFired()
        {
            if (CasterIsPawn)
                CasterPawn.records.Increment(RecordDefOf.ShotsFired);
        }

        /// <summary>
        /// 通过侧别label定位当前芯片Thing（Fix-14：从Verb_BDPShoot/Verb_BDPVolley提升到基类）。
        /// 三级回退：侧别label精确定位 → ActivatingSlot → 遍历所有激活槽位。
        /// </summary>
        protected Thing GetCurrentChipThing(CompTriggerBody triggerComp)
        {
            if (triggerComp == null) return null;

            // 优先通过侧别label精确定位芯片（独立Gizmo场景）
            SlotSide? side = DualVerbCompositor.ParseSideLabel(verbProps?.label);
            if (side.HasValue)
            {
                var sideSlot = triggerComp.GetActiveSlot(side.Value);
                if (sideSlot?.loadedChip != null) return sideSlot.loadedChip;
            }

            // 回退：从ActivatingSlot读取（芯片激活上下文）
            var slot = triggerComp.ActivatingSlot;
            if (slot?.loadedChip != null) return slot.loadedChip;

            // 最终回退：遍历所有激活槽位找第一个有WeaponChipConfig的
            foreach (var activeSlot in triggerComp.AllActiveSlots())
            {
                if (activeSlot.loadedChip?.def?.GetModExtension<WeaponChipConfig>() != null)
                    return activeSlot.loadedChip;
            }
            return null;
        }

        /// <summary>
        /// 从芯片配置读取WeaponChipConfig（三级回退策略）。
        /// 供引导弹判断（SupportsGuided）和参数读取共用。
        /// </summary>
        protected WeaponChipConfig GetChipConfig()
        {
            var pawn = CasterPawn;
            if (pawn == null) return null;
            var triggerComp = GetTriggerComp();
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
