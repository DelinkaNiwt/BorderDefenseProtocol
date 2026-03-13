using System.Collections.Generic;
using BDP.Core;
using BDP.Projectiles;
using BDP.Projectiles.Config;
using BDP.FireMode;
using BDP.Trigger.ShotPipeline;
using BDP.Trigger.WeaponDraw;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

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
    ///   ├── Verb_BDPSingle（单侧攻击，通过FiringPattern区分逐发/齐射）
    ///   ├── Verb_BDPDual（双侧攻击，每侧独立FiringPattern）
    ///   ├── Verb_BDPCombo（组合技）
    ///   ├── Verb_BDPMelee（近战）
    ///   └── Verb_BDPProxy（自动攻击代理）
    /// </summary>
    public abstract class Verb_BDPRangedBase : Verb_Shoot
    {
        // ── 芯片侧别标识（创建时设置，运行时直接查找） ──

        /// <summary>
        /// 此Verb所属的芯片侧别（创建时设置）。
        /// 单侧Verb: LeftHand/RightHand。双侧/组合技Verb: null。
        /// 无需序列化——RebuildVerbs在读档时重新设置。
        /// </summary>
        internal SlotSide? chipSide;

        // ── Fix-5：CompTriggerBody缓存（避免每发子弹多次TryGetComp线性搜索） ──
        private CompTriggerBody cachedTriggerComp;
        private Pawn cachedTriggerPawn;

        // ── ShotPipeline 集成（v16.0 管线重构） ──

        /// <summary>管线配置（首次使用时初始化）</summary>
        private ShotPipeline.ShotPipeline.PipelineConfig shotPipeline;

        /// <summary>当前射击会话（TryCastShot 期间有效）</summary>
        protected ShotSession activeSession;

        /// <summary>自动绕行路径交替索引（用于在左右路径间轮换）</summary>
        private int autoRouteIndex = 0;

        /// <summary>
        /// 获取CasterPawn装备的触发体CompTriggerBody（缓存版本）。
        /// Pawn变化时自动刷新缓存。
        /// </summary>
        /// <summary>
        /// 序列化BDP Verb扩展状态：
        /// 占位VerbProperties防止BuggedAfterLoading判定。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars && verbProps == null)
                verbProps = new VerbProperties();
        }

        /// <summary>
        /// Verb重置时清理射击会话。
        /// 触发场景：burst中断、Job结束、目标切换等。
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            activeSession = null;
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

        // ── ShotPipeline 管线方法 ──

        /// <summary>
        /// 初始化射击管线（延迟初始化，首次使用时调用）。
        /// v18.0：改为internal，允许CompTriggerBody在Verb创建后立即初始化。
        /// </summary>
        internal void InitShotPipeline()
        {
            if (shotPipeline == null)
                shotPipeline = ShotPipeline.ShotPipeline.Build();
        }

        /// <summary>
        /// 在瞄准开始时创建 ShotSession（Task 19）。
        /// 由 Command_BDPChipAttack 在用户点击 Gizmo 时调用。
        /// </summary>
        public void BeginTargetingSession()
        {
            // 初始化射击管线
            InitShotPipeline();

            // 构建射击上下文（使用占位符 target）
            var context = BuildContext();

            // 创建射击会话
            activeSession = new ShotSession(context);
        }

        /// <summary>
        /// 构建射击上下文（子类可重写以提供特定侧别的芯片信息）
        /// </summary>
        /// <returns>射击上下文快照</returns>
        protected virtual ShotContext BuildContext()
        {
            var triggerComp = GetTriggerComp();
            var chipThing = GetCurrentChipThing(triggerComp);
            var chipConfig = GetChipConfig();
            var projectileDef = GetContextProjectileDef();

            return new ShotContext(
                caster: CasterPawn,
                triggerComp: triggerComp,
                target: currentTarget,
                verb: this,
                chipConfig: chipConfig,
                chipSide: chipSide,
                chipThing: chipThing,
                projectileDef: projectileDef
            );
        }

        /// <summary>
        /// 获取上下文用的投射物定义（虚方法，子类可重写）。
        /// 默认实现：从VerbProps或ChipConfig读取。
        /// </summary>
        protected virtual ThingDef GetContextProjectileDef()
        {
            var fromVerbProps = verbProps?.defaultProjectile;
            var fromChipConfig = GetChipConfig()?.GetPrimaryProjectileDef();
            return fromVerbProps ?? fromChipConfig;
        }

        /// <summary>
        /// 执行射击（虚方法，由子类重写具体的射击逻辑）。
        /// 默认实现：回退到 TryCastShotCore。
        /// </summary>
        /// <param name="session">射击会话</param>
        /// <returns>是否成功射击</returns>
        protected virtual bool ExecuteFire(ShotSession session)
        {
            // 默认回退到旧逻辑
            var chipThing = GetCurrentChipThing(GetTriggerComp());
            return TryCastShotCore(chipThing);
        }

        /// <summary>
        /// 统一发射方法：从 TryCastShotCore 迁移的核心发射逻辑。
        /// 处理投射物生成、命中判定、掩体计算、弹道发射。
        /// </summary>
        /// <param name="chipEquipment">芯片Thing（用于战斗日志）</param>
        /// <param name="originOffset">视觉起点偏移（齐射散布）</param>
        /// <returns>是否成功发射</returns>
        protected bool LaunchProjectile(Thing chipEquipment, Vector3 originOffset = default)
        {
            // 无芯片上下文时回退到原版
            if (chipEquipment == null)
                return base.TryCastShot();

            // ── 以下复制自 Verb_LaunchProjectile.TryCastShot() ──

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

            // ── 计算发射位置（优先使用枪口位置） ──
            Vector3 drawPos = caster.DrawPos + originOffset;

            // 尝试从枪口发射（仅远程武器）
            if (Prefs.DevMode)
                Log.Message($"[BDP.Muzzle.Debug] chipSide={chipSide}, HasValue={chipSide.HasValue}");

            if (chipSide.HasValue)
            {
                var chipThing = GetCurrentChipThing(GetTriggerComp());
                if (Prefs.DevMode)
                    Log.Message($"[BDP.Muzzle.Debug] chipThing={chipThing?.def?.defName ?? "null"}");

                if (chipThing != null)
                {
                    var drawConfig = chipThing.def.GetModExtension<WeaponDrawChipConfig>();
                    if (Prefs.DevMode)
                        Log.Message($"[BDP.Muzzle.Debug] drawConfig={drawConfig != null}, isRangedWeapon={drawConfig?.isRangedWeapon ?? false}");

                    if (drawConfig?.isRangedWeapon == true)
                    {
                        // 计算瞄准角度
                        float aimAngle = (currentTarget.CenterVector3 - caster.DrawPos).AngleFlat();

                        // 获取枪口位置
                        var muzzlePos = GetMuzzlePosition(drawConfig, chipSide.Value, aimAngle);
                        if (muzzlePos.HasValue)
                        {
                            drawPos = muzzlePos.Value;
                            // 注意：枪口位置已包含武器偏移，不再叠加originOffset
                        }
                    }
                }
            }

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
            // 从管线系统读取瞄准结果
            if (activeSession?.AimResult?.HasGuidedPath == true)
                return new LocalTargetInfo(activeSession.AimResult.AnchorPath[0]);
            return currentTarget;
        }

        /// <summary>
        /// 引导模式下用第一个锚点替代最终目标进行LOS检查。
        /// 迁移自 VerbFlightState.InterceptCastTarget() + PostCastOn()。
        ///
        /// 架构设计：
        /// - 手动锚点模式：用户通过锚点瞄准设置了引导路径，用首锚点做LOS检查
        /// - 自动绕行模式：无手动锚点但弹药支持引导，自动计算绕行路径
        /// - 成功后锁定currentTarget为Cell形式，防止Thing追踪导致引导弹幽灵命中
        /// </summary>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            LocalTargetInfo actualTarget = castTarg;

            // ── 手动锚点模式：用户通过锚点瞄准设置了引导路径 ──
            if (activeSession?.AnchorPath != null && activeSession.AnchorPath.Count > 0)
            {
                // 能直视目标→保持朝向目标；不能直视→朝向第一锚点
                bool canSeeTarget = GenSight.LineOfSight(
                    caster.Position, actualTarget.Cell, caster.Map, skipFirstCell: true);
                if (!canSeeTarget)
                    castTarg = new LocalTargetInfo(activeSession.AnchorPath[0]);
            }
            else
            {
                // ── 自动绕行模式：无手动锚点时，为不可视目标准备绕行路径 ──
                TryPrepareAutoRouteForCast(ref castTarg);
            }

            bool result = base.TryStartCastOn(castTarg, destTarg, surpriseAttack,
                canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);

            // 成功后锁定currentTarget为实际目标Cell（防止Thing追踪导致幽灵命中）
            if (result)
            {
                bool hasManualAnchors = activeSession?.AnchorPath != null
                    && activeSession.AnchorPath.Count > 0;
                bool hasAutoRoute = activeSession?.RouteResult != null
                    && activeSession.RouteResult.Value.IsValid;
                if (hasManualAnchors || hasAutoRoute)
                    currentTarget = new LocalTargetInfo(actualTarget.Cell);
            }

            return result;
        }

        /// <summary>
        /// 为自动绕行准备施法上下文：计算绕行路由，设置LOS首锚点。
        /// 条件：无手动锚点 + 弹药有引导配置 + 无直接LOS。
        /// 迁移自 VerbFlightState.PrepareAutoRouteForCast()。
        /// </summary>
        private void TryPrepareAutoRouteForCast(ref LocalTargetInfo castTarg)
        {
            if (!castTarg.IsValid) return;

            // 有直接LOS则不需要绕行
            if (GenSight.LineOfSight(
                caster.Position, castTarg.Cell, caster.Map, skipFirstCell: true))
                return;

            // 检查投射物是否支持引导
            var projectileDef = GetAutoRouteProjectileDef();
            if (projectileDef == null) return;
            var guidedCfg = projectileDef.GetModExtension<BDPGuidedConfig>();
            if (guidedCfg == null) return;

            // 计算左右绕行路由
            var leftAnchors = ObstacleRouter.ComputeIterativeRoute(
                caster.Position, castTarg.Cell, caster.Map,
                guidedCfg.maxRouteDepth, guidedCfg.anchorsPerWall, preferLeft: true);
            var rightAnchors = ObstacleRouter.ComputeIterativeRoute(
                caster.Position, castTarg.Cell, caster.Map,
                guidedCfg.maxRouteDepth, guidedCfg.anchorsPerWall, preferLeft: false);

            // 全路径LOS验证
            if (!ObstacleRouter.IsPathClear(caster.Position, leftAnchors, castTarg.Cell, caster.Map))
                leftAnchors = null;
            if (!ObstacleRouter.IsPathClear(caster.Position, rightAnchors, castTarg.Cell, caster.Map))
                rightAnchors = null;
            if (leftAnchors == null && rightAnchors == null) return;

            var route = new ObstacleRouteResult
            {
                LeftAnchors = leftAnchors,
                RightAnchors = rightAnchors
            };

            // 确保session存在，存储路由结果供后续模块使用
            if (activeSession == null)
            {
                InitShotPipeline();
                activeSession = new ShotSession(BuildContext());
            }
            activeSession.RouteResult = route;

            // 选择一侧路径作为锚点（交替分配）
            var anchors = SelectRouteSide(route);
            if (anchors != null && anchors.Count > 0)
            {
                // 同步到 AimResult，确保 GetLosCheckTarget() 能读取到锚点
                if (activeSession.AimResult == null)
                    activeSession.AimResult = new ShotPipeline.AimResult();
                activeSession.AimResult.AnchorPath = anchors;
                activeSession.AimResult.FinalTarget = castTarg;

                // 从芯片配置获取锚点散布参数
                var chipConfig = GetChipConfig();
                activeSession.AimResult.AnchorSpread =
                    chipConfig?.ranged?.guided?.anchorSpread ?? 0.3f;

                // 选择首锚点作为LOS检查目标
                castTarg = new LocalTargetInfo(anchors[0]);
            }
        }

        /// <summary>
        /// 从绕行路由中选择首锚点（两侧都可用时选更近的一侧）。
        /// 迁移自 VerbFlightState.TryPickLosAnchor()。
        /// </summary>
        private bool TryPickFirstAnchor(ObstacleRouteResult route, out IntVec3 anchor)
        {
            anchor = default;
            bool hasLeft = route.LeftAnchors?.Count > 0;
            bool hasRight = route.RightAnchors?.Count > 0;
            if (!hasLeft && !hasRight) return false;

            if (hasLeft && hasRight)
            {
                int leftDist = (route.LeftAnchors[0] - caster.Position).LengthHorizontalSquared;
                int rightDist = (route.RightAnchors[0] - caster.Position).LengthHorizontalSquared;
                anchor = leftDist <= rightDist ? route.LeftAnchors[0] : route.RightAnchors[0];
                return true;
            }

            anchor = hasLeft ? route.LeftAnchors[0] : route.RightAnchors[0];
            return true;
        }

        /// <summary>
        /// 从绕行路由中选择一侧锚点路径（交替分配）。
        /// 两侧都可用时交替选择，单侧可用时返回该侧。
        /// </summary>
        private List<IntVec3> SelectRouteSide(ObstacleRouteResult route)
        {
            if (route.LeftAnchors != null && route.RightAnchors != null)
                return (autoRouteIndex++ % 2 == 0) ? route.LeftAnchors : route.RightAnchors;
            return route.LeftAnchors ?? route.RightAnchors;
        }

        /// <summary>
        /// 自动绕行判定用的投射物Def。默认取当前芯片的首个投射物，子类可按双侧规则重写。
        /// </summary>
        protected virtual ThingDef GetAutoRouteProjectileDef()
            => GetChipConfig()?.GetPrimaryProjectileDef() ?? Projectile;

        /// <summary>
        /// 重写瞄准参数，根据投射物配置自动决定是否允许瞄准空地。
        ///
        /// 架构设计：
        /// - 爆炸模块（BDPExplosionConfig）：范围伤害，应该能瞄准空地
        /// - 引导模块（BDPGuidedConfig）：可以绕过障碍物，应该能瞄准空地
        /// - 普通子弹：只能瞄准实体目标
        ///
        /// 这样无论是普通瞄准还是引导瞄准，都使用统一的逻辑，避免重复。
        /// </summary>
        public override TargetingParameters targetParams
        {
            get
            {
                var tp = base.targetParams;

                // 获取投射物定义（子类可重写此方法）
                var projectileDef = GetAutoRouteProjectileDef();
                if (projectileDef != null)
                {
                    // 检查是否有爆炸模块或引导模块
                    bool hasExplosion = projectileDef.GetModExtension<BDPExplosionConfig>() != null;
                    bool hasGuided = projectileDef.GetModExtension<BDPGuidedConfig>() != null;

                    // 爆炸模块或引导模块都允许瞄准空地
                    if (hasExplosion || hasGuided)
                    {
                        tp.canTargetLocations = true;
                    }
                }

                return tp;
            }
        }

        // ── 管线驱动点：TryCastShot override ──

        /// <summary>
        /// 管线驱动入口：重写 Verb_Shoot.TryCastShot()。
        /// 将射击逻辑委托给子类的 ExecuteFire()，而非使用原版逻辑。
        ///
        /// 架构设计：
        /// - 原版 Verb_Shoot.TryCastShot() 只发射1颗子弹，不知道BDP的芯片系统
        /// - 本重写确保所有 BDP Verb 子类的自定义射击模式（齐射、双侧交替、组合技等）被正确执行
        /// - ExecuteFire() 内部调用 TryCastShotCore() 来实际发射投射物
        /// </summary>
        protected override bool TryCastShot()
        {
            // 确保管线已初始化
            InitShotPipeline();

            // 如果没有活跃会话，创建一个（非瞄准模式直接射击时）
            if (activeSession == null)
                activeSession = new ShotSession(BuildContext());

            // 委托给子类的射击逻辑
            bool result = ExecuteFire(activeSession);

            // 不在burst结束时清理会话——锚点数据需要跨burst持久化（持续攻击场景）。
            // 会话在 BeginTargetingSession() 时重建，在 Reset() 时清理。

            return result;
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
            {
                // 诊断日志：投射物为null
                if (Prefs.DevMode)
                {
                    string chipName = chipEquipment?.def?.defName ?? "null";
                    string verbProjName = verbProps?.defaultProjectile?.defName ?? "null";
                    Log.Warning($"[BDP诊断] TryCastShotCore失败: projectileDef=null, " +
                        $"chipEquipment={chipName}, verbClass={GetType().Name}, " +
                        $"verbProps.defaultProjectile={verbProjName}");
                }
                return false;
            }

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

            // ── 计算发射位置（优先使用枪口位置） ──
            // 尝试从枪口发射（仅远程武器）
            if (chipSide.HasValue)
            {
                var drawConfig = chipEquipment.def.GetModExtension<WeaponDrawChipConfig>();
                if (drawConfig?.isRangedWeapon == true)
                {
                    // 计算瞄准角度
                    float aimAngle = (currentTarget.CenterVector3 - caster.DrawPos).AngleFlat();

                    // 获取枪口位置
                    var muzzlePos = GetMuzzlePosition(drawConfig, chipSide.Value, aimAngle);
                    if (muzzlePos.HasValue)
                    {
                        drawPos = muzzlePos.Value + shotOriginOffset;
                        // 注意：枪口位置已包含武器偏移，但仍需叠加shotOriginOffset（齐射散布）

                        // 可视化调试：在枪口位置绘制彩色点标记
                        if (Prefs.DevMode && BDPModInstance.Settings.enableMuzzleDebugVisual)
                        {
                            // 获取武器位置（使用lastDrawLoc，与GetMuzzlePosition一致）
                            var triggerComp = GetTriggerComp();
                            var chipThing = triggerComp?.GetActiveSlot(chipSide.Value)?.loadedChip;
                            if (chipThing != null)
                            {
                                bool isLeft = chipSide.Value == SlotSide.LeftHand;
                                var weaponEntry = CompTriggerBody.BuildEntry(drawConfig, chipThing, CasterPawn.Rotation, isLeft);
                                // 使用lastDrawLoc而不是DrawPos（与GetMuzzlePosition保持一致）
                                Vector3 weaponWorldPos = triggerComp.lastDrawLoc + weaponEntry.drawOffset;

                                // 计算各位置之间的距离
                                float dist_pawn_weapon = Vector3.Distance(caster.DrawPos, weaponWorldPos);
                                float dist_weapon_muzzle = Vector3.Distance(weaponWorldPos, muzzlePos.Value);
                                float dist_muzzle_fire = Vector3.Distance(muzzlePos.Value, drawPos);

                                // 详细日志
                                Log.Warning($"[BDP.Muzzle.Visual] ===== {chipSide.Value} 发射位置分析 =====");
                                Log.Warning($"  小人中心: {caster.DrawPos}");
                                Log.Warning($"  武器位置: {weaponWorldPos} (距小人 {dist_pawn_weapon:F3}m)");
                                Log.Warning($"  枪口位置: {muzzlePos.Value} (距武器 {dist_weapon_muzzle:F3}m)");
                                Log.Warning($"  发射位置: {drawPos} (距枪口 {dist_muzzle_fire:F3}m)");
                                Log.Warning($"  齐射偏移: {shotOriginOffset}");
                                Log.Warning($"  配置偏移: {drawConfig.muzzleOffset}");
                                Log.Warning($"  瞄准角度: {aimAngle:F1}°");
                                Log.Warning($"  武器角度: {weaponEntry.angle:F1}°");
                                Log.Warning($"  总角度: {aimAngle + weaponEntry.angle:F1}°");
                                Log.Warning($"  defaultOffset配置: {drawConfig.defaultOffset}");
                                Log.Warning($"  muzzleOffset配置: {drawConfig.muzzleOffset}");
                                Log.Warning($"  weaponEntry.drawOffset: {weaponEntry.drawOffset}");

                                // 在地图上绘制所有关键位置标记点
                                DrawAllPositionMarkers(
                                    caster.DrawPos,      // 小人位置
                                    weaponWorldPos,      // 武器位置
                                    muzzlePos.Value,     // 枪口位置
                                    drawPos,             // 开枪位置
                                    isLeft               // 是否左手
                                );
                            }
                        }
                    }
                }
            }

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
        /// 获取本次发射应播放的音效。基类直接返回 verbProps.soundCast，由引擎正常播放。
        /// Verb_BDPDual 重写此方法，在发射前把 verbProps.soundCast 换成当前侧音效。
        /// </summary>
        protected virtual SoundDef GetShotSound() => verbProps.soundCast;

        /// <summary>
        /// 弹道发射后回调（v7.0变化弹）。子类可重写此方法对刚发射的弹道进行后处理。
        /// 基类默认行为：通过管线系统注入射击数据。
        /// </summary>
        protected virtual void OnProjectileLaunched(Projectile proj)
        {
            if (!(proj is Bullet_BDP bdp)) return;
            if (activeSession == null) return;

            // 从管线系统注入射击数据
            bdp.InjectShotData(
                activeSession.AimResult,
                activeSession.FireResult,
                activeSession.RouteResult);
        }

        // ── PMS重构：引导弹支持（从Verb_BDPGuided上提） ──

        /// <summary>
        /// 获取引导弹相关的芯片配置。
        /// 单侧Verb：返回GetChipConfig()。
        /// 双侧Verb：子类重写，查找支持引导的那一侧的config。
        /// 用于SupportsGuided、StartAnchorTargeting、OnProjectileLaunched。
        /// </summary>
        protected virtual VerbChipConfig GetGuidedConfig() => GetChipConfig();

        /// <summary>当前芯片是否支持变化弹（引导飞行）。</summary>
        public virtual bool SupportsGuided => GetGuidedConfig()?.ranged?.guided != null;

        /// <summary>
        /// 启动多步锚点瞄准（由Command_BDPChipAttack.GizmoOnGUIInt调用）。
        /// 不支持引导时回退到普通targeting。
        /// </summary>
        public virtual void StartAnchorTargeting()
        {
            var cfg = GetGuidedConfig();
            if (cfg?.ranged?.guided == null)
            {
                Find.Targeter.BeginTargeting(this);
                return;
            }

            AnchorTargetingHelper.BeginAnchorTargeting(
                this, CasterPawn, cfg.ranged.guided.maxAnchors, verbProps.range,
                (anchors, finalTarget) =>
                {
                    // 将锚点数据存储到 activeSession
                    if (activeSession != null)
                    {
                        activeSession.AnchorPath = new System.Collections.Generic.List<IntVec3>(anchors);
                        // 更新 AimResult（如果已存在）
                        if (activeSession.AimResult == null)
                            activeSession.AimResult = new ShotPipeline.AimResult();
                        activeSession.AimResult.AnchorPath = activeSession.AnchorPath;
                        activeSession.AimResult.FinalTarget = finalTarget;
                        activeSession.AimResult.AnchorSpread = cfg.ranged.guided.anchorSpread;
                    }
                    // 直接调用OrderForceTargetCore创建BDP_ChipRangedAttack job
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
        /// 通过chipSide定位当前芯片Thing。
        /// chipSide由创建时设置，运行时直接按侧别查找。
        /// </summary>
        protected Thing GetCurrentChipThing(CompTriggerBody triggerComp)
        {
            if (triggerComp == null) return null;

            // chipSide已设置：按侧别精确查找
            if (chipSide.HasValue)
                return triggerComp.GetActiveSlot(chipSide.Value)?.loadedChip;

            // chipSide为null：双侧/组合技Verb，由子类各自处理
            return null;
        }

        /// <summary>
        /// 通过chipSide定位当前芯片的VerbChipConfig。
        /// chipSide由创建时设置，运行时直接按侧别查找。
        /// chipSide为null时返回null（双侧/组合技Verb由子类各自处理）。
        /// </summary>
        protected VerbChipConfig GetChipConfig()
        {
            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return null;

            // chipSide已设置：按侧别精确查找
            if (chipSide.HasValue)
                return triggerComp.GetActiveSlot(chipSide.Value)
                    ?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

            // chipSide为null：双侧/组合技Verb
            return null;
        }

        /// <summary>
        /// 计算指定侧别芯片的枪口世界坐标。
        /// 使用四元数旋转将枪口从武器局部坐标系转换到世界坐标系。
        /// </summary>
        /// <param name="config">芯片的武器绘制配置</param>
        /// <param name="chipSide">芯片侧别（左手/右手）</param>
        /// <param name="aimAngle">瞄准角度（度）</param>
        /// <returns>枪口世界坐标，如果不是远程武器则返回null</returns>
        protected Vector3? GetMuzzlePosition(WeaponDrawChipConfig config, SlotSide chipSide, float aimAngle)
        {
            // 1. 检查是否为远程武器
            if (config == null || !config.isRangedWeapon)
                return null;

            // 2. 获取枪口局部偏移（支持左手覆盖）
            Vector3 localMuzzleOffset = config.muzzleOffset;
            if (chipSide == SlotSide.LeftHand && config.leftMuzzleOffsetOverride.HasValue)
                localMuzzleOffset = config.leftMuzzleOffsetOverride.Value;

            // 3. 获取芯片Thing（用于BuildEntry）
            var triggerComp = GetTriggerComp();
            if (triggerComp == null) return null;
            var chipThing = triggerComp.GetActiveSlot(chipSide)?.loadedChip;
            if (chipThing == null) return null;

            // 4. 计算武器世界位置（使用缓存的drawLoc，而非DrawPos）
            bool isLeft = chipSide == SlotSide.LeftHand;
            var weaponEntry = CompTriggerBody.BuildEntry(config, chipThing, CasterPawn.Rotation, isLeft);

            // 使用triggerComp.lastDrawLoc（武器实际绘制基准点，包含动画偏移）
            // 注意：lastDrawLoc ≠ CasterPawn.DrawPos，差值可达0.3m
            Vector3 weaponWorldPos = triggerComp.lastDrawLoc + weaponEntry.drawOffset;

            // 5. 同步绘制代码的翻转状态，修正枪口X偏移
            // 绘制代码在 aimAngle∈(200°,340°) 时切换 plane10Flip（贴图水平翻转）
            // 注意：手侧翻转（entry.flipHorizontal）只改变枪托朝向，不改变枪管方向，
            //       因此不影响枪口X方向，不参与此处判断。
            bool aimFlip = aimAngle > 200f && aimAngle < 340f;
            if (aimFlip)
                localMuzzleOffset.x = -localMuzzleOffset.x;

            // 6. 使用aimAngle旋转muzzleOffset（枪口沿目标方向偏移，而非武器图形朝向）
            // drawAngle = aimAngle - 90° + entry.angle，包含图形旋转偏移，不适合用于枪口计算
            Quaternion aimRotation = Quaternion.AngleAxis(aimAngle, Vector3.up);
            Vector3 muzzleWorldOffset = aimRotation * localMuzzleOffset;

            // 6. 枪口世界坐标 = 武器世界坐标 + 枪口世界偏移
            Vector3 muzzlePos = weaponWorldPos + muzzleWorldOffset;

            // 7. 开发模式下输出诊断信息
            if (Prefs.DevMode)
            {
                float drawAngle = CalculateWeaponDrawAngle(aimAngle, weaponEntry, base.EquipmentSource.def.equippedAngleOffset);
                Log.Message($"[BDP.Muzzle] {chipSide}: weaponPos={weaponWorldPos}, " +
                    $"muzzleOffset={localMuzzleOffset}, aimAngle={aimAngle:F1}, " +
                    $"aimFlip={aimFlip}, entryFlip={weaponEntry.flipHorizontal}, " +
                    $"drawAngle={drawAngle:F1}, muzzleWorldOffset={muzzleWorldOffset}, muzzlePos={muzzlePos}");
            }

            return muzzlePos;
        }

        /// <summary>
        /// 执行齐射循环发射（v15.0从双武器基类上提到远程基类）。
        /// 在单次TryCastShot内循环瞬发多颗子弹，无间隔。
        /// 散布使用局部坐标系：相对于射击方向的前后和左右偏移。
        /// </summary>
        /// <param name="volleyCount">齐射子弹数量</param>
        /// <param name="spread">散布半径（米）</param>
        /// <param name="chipEquipment">芯片Thing（用于战斗日志）</param>
        /// <returns>是否至少有一发命中</returns>
        protected bool FireVolleyLoop(int volleyCount, float spread, Thing chipEquipment)
        {
            bool anyHit = false;

            // 计算射击方向（局部坐标系基准）
            Vector3 shootDir = Vector3.zero;
            Vector3 rightDir = Vector3.zero;
            if (spread > 0f && currentTarget.IsValid)
            {
                // 从施法者到目标的方向向量
                shootDir = (currentTarget.CenterVector3 - caster.DrawPos).normalized;
                // 计算垂直于射击方向的右向量（叉乘：up × forward = right）
                rightDir = Vector3.Cross(Vector3.up, shootDir).normalized;
            }

            for (int i = 0; i < volleyCount; i++)
            {
                if (spread > 0f)
                {
                    // 在局部坐标系中生成散布
                    float forwardSpread = Rand.Range(-spread, spread);  // 前后散布（沿射击方向）
                    float rightSpread = Rand.Range(-spread, spread);    // 左右散布（垂直于射击方向）

                    // 转换到世界坐标系
                    shotOriginOffset = shootDir * forwardSpread + rightDir * rightSpread;
                }

                if (TryCastShotCore(chipEquipment))
                    anyHit = true;
            }
            shotOriginOffset = Vector3.zero;
            return anyHit;
        }

        // ── 范围指示器支持 ──

        /// <summary>
        /// 计算武器实际绘制角度，与Patch_DrawEquipmentAiming_Weapon.DrawEntry逻辑完全一致。
        /// 用于枪口位置计算时的旋转，确保与贴图对齐。
        /// </summary>
        private static float CalculateWeaponDrawAngle(float aimAngle, WeaponDrawEntry entry, float equippedAngleOffset)
        {
            float angle = aimAngle - 90f;
            bool isFlipMesh;

            if (aimAngle > 20f && aimAngle < 160f)
            {
                isFlipMesh = false;
                angle += equippedAngleOffset;
            }
            else if (aimAngle > 200f && aimAngle < 340f)
            {
                isFlipMesh = true;
                angle -= 180f;
                angle -= equippedAngleOffset;
            }
            else
            {
                isFlipMesh = false;
                angle += equippedAngleOffset;
            }

            // 叠加芯片配置角度（flipMesh时取反，与DrawEntry一致）
            angle += isFlipMesh ? -entry.angle : entry.angle;

            // flipHorizontal时对angle取反
            if (entry.flipHorizontal)
                angle = -angle;

            return angle;
        }

        /// <summary>
        /// 重写 DrawHighlight 方法，委托给管线的 AimRenderers。
        /// 此方法由 Targeter 在瞄准阶段自动调用。
        /// </summary>
        /// <param name="target">当前瞄准目标</param>
        public override void DrawHighlight(LocalTargetInfo target)
        {

            base.DrawHighlight(target);

            if (!target.IsValid)
            {
                return;
            }

            // 条件化预览线绘制：
            // - 非引导模式：始终绘制直线
            // - 引导模式 + 可直视：绘制直线（提供视觉反馈）
            // - 引导模式 + 不可直视：不绘制直线（由AnchorTargetingHelper绘制路径预览）
            bool isGuidedMode = SupportsGuided;
            bool hasDirectLOS = GenSight.LineOfSight(caster.Position, target.Cell, caster.Map);


            if (!isGuidedMode || hasDirectLOS)
            {
                GenDraw.DrawLineBetween(caster.DrawPos, target.CenterVector3);
            }
            else
            {
            }

            // 初始化管线
            InitShotPipeline();

            // 构建临时会话用于渲染
            var context = BuildContext();
            var session = new ShotSession(context);

            // 诊断日志：检查上下文数据

            // 执行 Aim 阶段（生成 AimResult）
            ShotPipeline.ShotPipeline.ExecuteAim(session, shotPipeline);

            // 调用所有 AimRenderers
            foreach (var renderer in shotPipeline.AimRenderers)
            {
                renderer.RenderTargeting(session, target);
            }
        }

        /// <summary>
        /// 绘制所有关键位置的彩色标记点（可视化调试）
        /// </summary>
        /// <param name="pawnPos">小人中心位置</param>
        /// <param name="weaponPos">武器位置</param>
        /// <param name="muzzlePos">枪口位置</param>
        /// <param name="firePos">实际开枪位置</param>
        /// <param name="isLeftHand">是否为左手武器</param>
        private void DrawAllPositionMarkers(Vector3 pawnPos, Vector3 weaponPos, Vector3 muzzlePos, Vector3 firePos, bool isLeftHand)
        {
            if (caster?.Map == null) return;

            // 定义颜色方案
            Color pawnColor = new Color(1f, 1f, 0f, 0.9f);      // 黄色 - 小人位置（共享）
            Color weaponColor = new Color(0f, 1f, 0f, 0.9f);    // 绿色 - 武器位置
            Color muzzleColor = isLeftHand
                ? new Color(0.2f, 0.5f, 1f, 0.9f)               // 蓝色 - 左手枪口
                : new Color(1f, 0.3f, 0.2f, 0.9f);              // 红色 - 右手枪口
            Color fireColor = new Color(1f, 0.6f, 0f, 0.9f);    // 橙色 - 开枪位置

            // 绘制各个位置点
            DrawPositionMarker(pawnPos, pawnColor, 0.4f);       // 小人位置稍大
            DrawPositionMarker(weaponPos, weaponColor, 0.3f);   // 武器位置
            DrawPositionMarker(muzzlePos, muzzleColor, 0.35f);  // 枪口位置稍大（重点）
            DrawPositionMarker(firePos, fireColor, 0.3f);       // 开枪位置
        }

        /// <summary>
        /// 在指定位置绘制单个彩色标记点
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="color">颜色</param>
        /// <param name="scale">缩放大小</param>
        private void DrawPositionMarker(Vector3 position, Color color, float scale)
        {
            if (caster?.Map == null) return;

            try
            {
                // 使用 Mote_PowerBeam 作为标记（简单的光点效果）
                Mote mote = (Mote)ThingMaker.MakeThing(ThingDefOf.Mote_PowerBeam);
                if (mote != null)
                {
                    mote.exactPosition = position;
                    mote.Scale = scale;
                    mote.rotationRate = 0f;
                    mote.instanceColor = color;

                    GenSpawn.Spawn(mote, position.ToIntVec3(), caster.Map);

                    // 设置生命周期：120 tick (约2秒)
                    // 注意：这些属性需要在Spawn之后设置才有效
                    if (mote.def.mote != null)
                    {
                        mote.def.mote.fadeInTime = 0f;
                        mote.def.mote.solidTime = 1.8f;
                        mote.def.mote.fadeOutTime = 0.2f;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[BDP.Muzzle.Visual] 绘制标记点失败: {ex.Message}");
            }
        }
    }
}
