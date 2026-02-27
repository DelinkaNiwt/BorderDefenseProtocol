using System.Collections.Generic;
using System.Linq;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BDP.Trigger
{
    /// <summary>
    /// 近战攻击Verb（v2.0新增，v5.0重命名自Verb_BDPDualMelee）——
    /// 单侧+双侧近战共用，每次TryCastShot只执行1击，由引擎burst机制驱动后续打击。
    /// 继承Verb_MeleeAttackDamage，重写TryCastShot()和ApplyMeleeDamageToTarget()。
    ///
    /// v6.0变更（近战Burst视觉化改造）：
    ///   · 去掉for循环同步多击，改为hitIndex状态机+引擎burst驱动
    ///   · 每次TryCastShot()只打1下，由burstShotCount+ticksBetweenBurstShots控制连击
    ///   · 新增ApplyPendingInterval()支持按芯片配置不同的burst间隔
    ///   · 清除Stance_Cooldown绕过FullBodyBusy检查（引擎时序问题）
    ///   · OrderForceTarget改用BDP_ChipMeleeAttack JobDef
    ///
    /// 引擎约束（已验证）：
    ///   · VerbsTick()先于StanceTrackerTick() → burst续击时Stance_Cooldown未过期
    ///     → Verb_MeleeAttack.TryCastShot()的FullBodyBusy检查失败 → 需手动清除Stance
    ///   · BDP Verb不在VerbTracker中 → VerbTick()不被引擎调用
    ///     → 需自定义JobDriver手动调用VerbTick()
    ///   · TicksBetweenBurstShots非virtual → 需Post-VerbTick覆盖ticksToNextBurstShot
    ///
    /// 数据获取路径：
    ///   this.caster (Pawn) → pawn.equipment.Primary (触发体)
    ///     → CompTriggerBody → GetActiveSlot(side) → WeaponChipConfig
    /// </summary>
    public class Verb_BDPMelee : Verb_MeleeAttackDamage
    {
        /// <summary>
        /// 读档时设置占位VerbProperties，防止BuggedAfterLoading判定。
        /// 原因同Verb_BDPRangedBase.ExposeData()注释。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars && verbProps == null)
                verbProps = new VerbProperties();
        }

        // ── ManeuverDef缓存（Fix-4：避免每次EnsureToolAndManeuver线性搜索DefDatabase） ──
        private static Dictionary<ToolCapacityDef, ManeuverDef> maneuverCache;

        /// <summary>
        /// 通过ToolCapacityDef查找对应的ManeuverDef（缓存版本）。
        /// 首次调用时构建缓存，后续O(1)查找。供EnsureToolAndManeuver和WeaponChipEffect共用。
        /// </summary>
        internal static ManeuverDef GetManeuverForCapacity(ToolCapacityDef capacity)
        {
            if (capacity == null) return null;
            if (maneuverCache == null)
            {
                maneuverCache = new Dictionary<ToolCapacityDef, ManeuverDef>();
                foreach (var m in DefDatabase<ManeuverDef>.AllDefs)
                    if (m.requiredCapacity != null && !maneuverCache.ContainsKey(m.requiredCapacity))
                        maneuverCache[m.requiredCapacity] = m;
            }
            maneuverCache.TryGetValue(capacity, out var result);
            return result;
        }

        /// <summary>当前攻击阶段使用的芯片ThingDef（供ApplyMeleeDamageToTarget读取）。</summary>
        private ThingDef currentChipDef;

        // ── Fix-5：CompTriggerBody缓存（避免每击TryGetComp线性搜索） ──
        private CompTriggerBody cachedTriggerComp;
        private Pawn cachedTriggerPawn;

        private CompTriggerBody GetTriggerComp()
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

        // ── v6.0 hitIndex状态机字段 ──
        private int hitIndex;              // 当前burst中第几击（0-based）
        private int pendingInterval = -1;  // 待覆盖的下一击间隔（ticks），-1=无需覆盖
        private int cachedLeftBurst;       // 缓存：左侧burst击数
        private int cachedRightBurst;      // 缓存：右侧burst击数
        private int cachedLeftInterval;    // 缓存：左侧burst间隔（ticks）
        private int cachedRightInterval;   // 缓存：右侧burst间隔（ticks）

        /// <summary>
        /// Bug9修复：覆盖ShotsPerBurst，使用verbProps.burstShotCount。
        /// 原因：Verb基类硬编码 ShotsPerBurst => 1，只有Verb_Shoot覆盖为BurstShotCount。
        ///       近战Verb继承链从未覆盖，导致WarmupComplete()永远设burstShotsLeft=1。
        /// </summary>
        protected override int ShotsPerBurst => verbProps.burstShotCount;

        /// <summary>
        /// 重写OrderForceTarget：使用BDP_ChipMeleeAttack自定义JobDriver。
        /// 原因：原版AttackMelee的JobDriver不调用VerbTick()，BDP Verb的burst计时器不推进。
        /// </summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (CasterPawn == null) return;
            Job job = JobMaker.MakeJob(BDP_DefOf.BDP_ChipMeleeAttack, target);
            job.verbToUse = this;
            job.playerForced = true;
            if (target.Thing is Pawn p && p.Downed)
                job.killIncappedTarget = true;
            CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        /// <summary>
        /// 重写TryCastShot（v6.0）：每次只打1击，由引擎burst机制驱动后续打击。
        /// hitIndex状态机追踪当前是第几击、该用哪侧芯片。
        /// </summary>
        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn == null) return false;

            var triggerComp = GetTriggerComp();
            if (triggerComp == null)
                return base.TryCastShot();

            // 首击时初始化burst状态
            if (hitIndex == 0)
            {
                InitBurst(triggerComp);
                if (Prefs.DevMode)
                    Log.Message($"[BDP Melee] Burst开始: burstShotsLeft={burstShotsLeft}, " +
                        $"leftBurst={cachedLeftBurst}(interval={cachedLeftInterval}), " +
                        $"rightBurst={cachedRightBurst}(interval={cachedRightInterval}), " +
                        $"verbProps.burstShotCount={verbProps.burstShotCount}");
            }

            // Bug6修复+增强：目标无效时中止burst（死亡、消失、null）
            var targetThing = currentTarget.Thing;
            if (targetThing == null || targetThing.Destroyed || !targetThing.Spawned)
            {
                hitIndex = 0;
                currentChipDef = null;
                return false;
            }

            // 引擎时序修复：清除burst间的Stance_Cooldown
            // 原因：VerbsTick()先于StanceTrackerTick()，Stance_Cooldown还剩1tick未过期
            if (pawn.stances.curStance is Stance_Cooldown)
                pawn.stances.SetStance(new Stance_Mobile());

            // 根据hitIndex确定当前侧别和芯片
            SlotSide side = GetSideForHitIndex();
            currentChipDef = GetChipDef(triggerComp, side);

            if (Prefs.DevMode)
                Log.Message($"[BDP Melee] Hit #{hitIndex}: side={side}, chip={currentChipDef?.defName ?? "null"}, " +
                    $"burstShotsLeft={burstShotsLeft}, state={state}");

            // Bug11修复：设置tool和maneuver（BDP Verb不经过VerbTracker.InitVerb，这两个字段为null）
            // 原因：Verb_MeleeAttack.TryCastShot()中CreateCombatLog访问maneuver字段 → NullRef
            //       tool也被用于战斗日志的bodyPartGroup和label
            EnsureToolAndManeuver(triggerComp, side);

            base.TryCastShot();

            // Bug7修复：burst期间始终返回true，防止miss/dodge取消整个burst。
            // 原因：base.TryCastShot()在miss/dodge时返回false，
            //       TryCastNextBurstShot收到false后设burstShotsLeft=0取消整个burst。
            //       miss/dodge的伤害和日志已由base处理完毕，这里只需告诉burst系统"继续下一击"。

            // 计算下一击的interval
            int nextIndex = hitIndex + 1;
            int totalHits = cachedLeftBurst + cachedRightBurst;
            if (nextIndex < totalHits)
            {
                bool nextIsLeft = nextIndex < cachedLeftBurst;
                pendingInterval = nextIsLeft ? cachedLeftInterval : cachedRightInterval;
            }

            hitIndex++;

            // burst结束时重置
            if (burstShotsLeft <= 1)
            {
                hitIndex = 0;
                currentChipDef = null;
            }

            return true;
        }

        /// <summary>
        /// 由JobDriver_BDPChipMeleeAttack在VerbTick()之后调用。
        /// 覆盖引擎设置的固定ticksToNextBurstShot为芯片自定义间隔。
        /// </summary>
        public void ApplyPendingInterval()
        {
            if (pendingInterval < 0 || state != VerbState.Bursting) return;

            ticksToNextBurstShot = pendingInterval;

            // 防御性检查：CasterPawn和currentTarget可能在burst期间变无效
            var pawn = CasterPawn;
            if (pawn?.stances != null && currentTarget.IsValid)
                pawn.stances.SetStance(
                    new Stance_Cooldown(pendingInterval + 1, currentTarget, this));

            pendingInterval = -1;
        }

        /// <summary>
        /// 安全中止burst：重置所有BDP状态字段+调用基类Reset()。
        /// 与直接调用Reset()的区别：同时清理hitIndex等BDP自有字段，防止状态残留。
        /// </summary>
        public void SafeAbortBurst()
        {
            hitIndex = 0;
            currentChipDef = null;
            pendingInterval = -1;
            cachedLeftBurst = 0;
            cachedRightBurst = 0;
            Reset(); // 基类：state=Idle, burstShotsLeft=0, currentTarget=null
        }

        // ═══════════════════════════════════════════
        //  Burst状态机内部方法
        // ═══════════════════════════════════════════

        /// <summary>首击时初始化burst状态：缓存两侧的burst数和interval。</summary>
        private void InitBurst(CompTriggerBody triggerComp)
        {
            SlotSide? singleSide = DualVerbCompositor.ParseSideLabel(verbProps.label);
            if (singleSide == SlotSide.RightHand)
            {
                cachedLeftBurst = 0;
                cachedLeftInterval = 0;
                cachedRightBurst = GetMeleeBurstCount(triggerComp, SlotSide.RightHand);
                cachedRightInterval = GetMeleeBurstInterval(triggerComp, SlotSide.RightHand);
            }
            else if (singleSide == SlotSide.LeftHand)
            {
                cachedLeftBurst = GetMeleeBurstCount(triggerComp, SlotSide.LeftHand);
                cachedLeftInterval = GetMeleeBurstInterval(triggerComp, SlotSide.LeftHand);
                cachedRightBurst = 0;
                cachedRightInterval = 0;
            }
            else
            {
                cachedLeftBurst = GetMeleeBurstCount(triggerComp, SlotSide.LeftHand);
                cachedLeftInterval = GetMeleeBurstInterval(triggerComp, SlotSide.LeftHand);
                cachedRightBurst = GetMeleeBurstCount(triggerComp, SlotSide.RightHand);
                cachedRightInterval = GetMeleeBurstInterval(triggerComp, SlotSide.RightHand);
            }
        }

        /// <summary>根据hitIndex确定当前击应使用哪一侧。前cachedLeftBurst击为左手，之后为右手。</summary>
        private SlotSide GetSideForHitIndex()
        {
            return hitIndex < cachedLeftBurst ? SlotSide.LeftHand : SlotSide.RightHand;
        }

        /// <summary>
        /// Bug11修复：从当前芯片配置设置tool和maneuver字段。
        /// 原因：BDP Verb不经过VerbTracker.InitVerb()，tool和maneuver始终为null。
        ///       Verb_MeleeAttack.TryCastShot()中CreateCombatLog访问maneuver → NullRef。
        /// 每击调用（非仅首击），因为左右芯片可能有不同的tool/maneuver。
        /// </summary>
        private void EnsureToolAndManeuver(CompTriggerBody triggerComp, SlotSide side)
        {
            var slot = triggerComp.GetActiveSlot(side);
            var cfg = slot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var firstTool = cfg?.tools?.FirstOrDefault();

            // 设置tool（供战斗日志的bodyPartGroup和label使用）
            tool = firstTool;

            // 设置maneuver（供CreateCombatLog使用，使用缓存避免线性搜索）
            if (firstTool?.capacities != null && firstTool.capacities.Count > 0)
                maneuver = GetManeuverForCapacity(firstTool.capacities[0]);

            // 兜底：确保maneuver不为null
            if (maneuver == null)
                maneuver = DefDatabase<ManeuverDef>.AllDefs.FirstOrDefault();
        }

        // ═══════════════════════════════════════════
        //  伤害和数据读取
        // ═══════════════════════════════════════════

        /// <summary>
        /// 重写ApplyMeleeDamageToTarget（v4.0 B3修复）：
        /// 使用currentChipDef作为DamageInfo的weapon参数。
        /// </summary>
        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            if (currentChipDef == null)
                return base.ApplyMeleeDamageToTarget(target);

            var result = new DamageWorker.DamageResult();
            float damage = verbProps.AdjustedMeleeDamageAmount(this, CasterPawn);
            float armorPen = verbProps.AdjustedArmorPenetration(this, CasterPawn);
            DamageDef damageDef = verbProps.meleeDamageDef;
            BodyPartGroupDef bodyPartGroup = null;
            HediffDef hediffDef = null;

            damage = Rand.Range(damage * 0.8f, damage * 1.2f);
            if (CasterIsPawn)
            {
                bodyPartGroup = verbProps.AdjustedLinkedBodyPartsGroup(tool);
                if (damage >= 1f)
                {
                    if (HediffCompSource != null)
                        hediffDef = HediffCompSource.Def;
                }
                else
                {
                    damage = 1f;
                    damageDef = DamageDefOf.Blunt;
                }
            }

            // 有意省略SetWeaponQuality：芯片是ThingDef而非Thing实例，没有品质属性。
            // weaponQuality保持默认值Normal(1.0x)。仅影响DamageWorker_Nerve的眩晕时长倍率，
            // 对普通物理伤害(Cut/Blunt/Stab)无影响。若将来芯片引入品质系统，需在此补上。
            ThingDef weaponDef = currentChipDef;
            Vector3 direction = (target.Thing.Position - CasterPawn.Position).ToVector3();
            bool instigatorGuilty = !(caster is Pawn pp) || !pp.Drafted;

            var dinfo = new DamageInfo(damageDef, damage, armorPen, -1f, caster,
                null, weaponDef, DamageInfo.SourceCategory.ThingOrUnknown, null, instigatorGuilty);
            dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
            dinfo.SetWeaponBodyPartGroup(bodyPartGroup);
            dinfo.SetWeaponHediff(hediffDef);
            dinfo.SetAngle(direction);
            dinfo.SetTool(tool);

            if (!target.ThingDestroyed)
                result = target.Thing.TakeDamage(dinfo);

            if (tool?.extraMeleeDamages != null)
            {
                foreach (var extra in tool.extraMeleeDamages)
                {
                    if (target.ThingDestroyed) break;
                    if (!Rand.Chance(extra.chance)) continue;
                    float extraDmg = Rand.Range(extra.amount * 0.8f, extra.amount * 1.2f);
                    var extraInfo = new DamageInfo(extra.def, extraDmg,
                        extra.AdjustedArmorPenetration(this, CasterPawn),
                        -1f, caster, null, weaponDef);
                    extraInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                    extraInfo.SetWeaponBodyPartGroup(bodyPartGroup);
                    extraInfo.SetWeaponHediff(hediffDef);
                    extraInfo.SetAngle(direction);
                    result = target.Thing.TakeDamage(extraInfo);
                }
            }
            return result;
        }

        /// <summary>获取指定侧芯片的ThingDef。</summary>
        private static ThingDef GetChipDef(CompTriggerBody triggerComp, SlotSide side)
        {
            return triggerComp.GetActiveSlot(side)?.loadedChip?.def;
        }

        /// <summary>获取指定侧芯片的近战连击数。</summary>
        private static int GetMeleeBurstCount(CompTriggerBody triggerComp, SlotSide side)
        {
            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) return 1;
            var ext = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            return ext?.meleeBurstCount ?? 1;
        }

        /// <summary>获取指定侧芯片的近战连击间隔（ticks）。</summary>
        private static int GetMeleeBurstInterval(CompTriggerBody triggerComp, SlotSide side)
        {
            var slot = triggerComp.GetActiveSlot(side);
            if (slot?.loadedChip == null) return 12;
            var ext = slot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            return ext?.meleeBurstInterval ?? 12;
        }
    }
}