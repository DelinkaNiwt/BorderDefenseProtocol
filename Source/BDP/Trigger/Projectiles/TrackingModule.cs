using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 追踪诊断日志开关。
    /// 游戏内开启：在dev console或Harmony patch中设置 BDP.Trigger.TrackingDiag.Enabled = true
    /// </summary>
    public static class TrackingDiag
    {
        public static bool Enabled = true;
        /// <summary>日志间隔（tick），避免刷屏。1秒≈60tick。</summary>
        public static int Interval = 60;
    }

    /// <summary>
    /// 追踪模块——实时追踪目标的弹道控制。
    /// Priority=15（在GuidedModule之后执行）。
    ///
    /// v5管线接口：
    ///   IBDPFlightIntentProvider — 每tick产出飞行方向意图（核心追踪逻辑）
    ///   IBDPLifecyclePolicy     — 计数飞行时间，处理超时/丢锁自毁
    ///   IBDPHitResolver         — 极近距离命中保证 + 追踪过期打地面
    ///   IBDPArrivalPolicy       — 到达时判断是否需要继续追踪
    ///
    /// v5核心原则：模块只产出意图，不直接写宿主状态。
    ///   · 不写host.Destroy() → ctx.RequestDestroy
    ///   · 不写host.usedTarget → ctx.OverrideTarget
    ///   · 不写origin/destination → ctx.Intent
    ///   · 不写Phase → ctx.RequestPhaseChange
    ///   · 读host.Phase替代读host.IsOnFinalSegment/IsTracking
    /// </summary>
    public class TrackingModule : IBDPProjectileModule,
        IBDPFlightIntentProvider, IBDPLifecyclePolicy,
        IBDPHitResolver, IBDPArrivalPolicy
    {
        /// <summary>追踪配置引用。</summary>
        private readonly BDPTrackingConfig config;

        /// <summary>当前飞行角度（度，0=东偏北方向，与Atan2(x,z)一致）。</summary>
        private float currentAngle;

        /// <summary>当前角速度（度/tick，仅Smooth模式使用）。</summary>
        private float angularVelocity;

        /// <summary>飞行已持续tick数（从发射开始计）。</summary>
        private int flyingTicks;

        /// <summary>是否已初始化角度（首次ProvideIntent时从弹道方向初始化）。</summary>
        private bool angleInitialized;

        /// <summary>上次搜索目标的tick。</summary>
        private int lastSearchTick;

        /// <summary>
        /// 最终进近标志——进入近距离后置true，阻止ProvideIntent继续重定向，
        /// 让ticksToImpact自然递减到0触发ImpactSomething。
        /// </summary>
        private bool finalApproach;

        /// <summary>
        /// 射手到目标的初始距离（格）。首次满足追踪前置条件时记录一次。
        /// 用于距离比例激活：distToTarget ≤ initialDistance × trackingStartRatio 时开始追踪。
        /// </summary>
        private float initialDistance;

        /// <summary>
        /// 丢失追踪后的累计tick数。追踪恢复时归零。
        /// 超过阈值仍未重新锁定则请求自毁。
        /// </summary>
        private int trackingLostTicks;

        /// <summary>
        /// 是否曾成功进入追踪锁定。
        /// 仅在该标志为true后，丢锁倒计时才会生效。
        /// </summary>
        private bool hadTrackingLock;

        /// <summary>连续触发ArrivalContinue的次数（诊断用）。</summary>
        private int arrivalContinueStreak;

        /// <summary>累计进入finalApproach的次数（诊断用）。</summary>
        private int finalApproachEntries;

        /// <summary>上一帧目标位置（速度外推预测用）。</summary>
        private Vector3 lastTargetPos;

        /// <summary>lastTargetPos是否有效（首tick无上一帧数据）。</summary>
        private bool lastTargetPosValid;

        public int Priority => 15;

        /// <summary>无参构造——Scribe反序列化需要。</summary>
        public TrackingModule() { config = null; }

        public TrackingModule(BDPTrackingConfig config)
        {
            this.config = config;
        }

        public void OnSpawn(Bullet_BDP host)
        {
            // 初始追踪目标 = 弹道的最终目标
            host.TrackingTarget = host.FinalTarget;
        }

        // ══════════════════════════════════════════
        //  IBDPLifecyclePolicy — 飞行计时 + 超时/丢锁自毁
        // ══════════════════════════════════════════

        public void CheckLifecycle(Bullet_BDP host, ref LifecycleContext ctx)
        {
            flyingTicks++;

            var cfg = GetConfig(host);
            if (cfg == null) return;

            // 超时自毁
            if (flyingTicks >= cfg.maxFlyingTicks)
            {
                ctx.RequestDestroy = true;
                if (TrackingDiag.Enabled)
                    Log.Message($"[BDP-Track] Destroy timeout tick={flyingTicks}");
                ctx.DestroyReason = $"超时 flyingTicks={flyingTicks}";
                return;
            }

            bool isTracking = ctx.CurrentPhase == FlightPhase.Tracking;
            bool isFinalOrTracking = ctx.CurrentPhase == FlightPhase.FinalApproach
                || ctx.CurrentPhase == FlightPhase.Tracking;

            if (isTracking)
                hadTrackingLock = true;

            // 丢锁检测：上一tick无模块产出Intent且曾有追踪锁定
            if (!ctx.PreviousTickHadIntent
                && hadTrackingLock
                && ctx.CurrentPhase == FlightPhase.Tracking)
            {
                ctx.RequestPhaseChange = FlightPhase.TrackingLost;
                if (TrackingDiag.Enabled)
                    Log.Message($"[BDP-Track] LockLost→TrackingLost tick={flyingTicks}");
            }

            // 目标丢失时的丢锁倒计时
            bool isLost = ctx.CurrentPhase == FlightPhase.TrackingLost;
            if (isLost)
            {
                trackingLostTicks++;
                int destroyAfter = Mathf.Max(1, cfg.lostTrackingSelfDestructTicks);

                // 丢锁超时自毁
                if (trackingLostTicks >= destroyAfter)
                {
                    ctx.RequestDestroy = true;
                    if (TrackingDiag.Enabled)
                        Log.Message($"[BDP-Track] Destroy lostLock ticks={trackingLostTicks}");
                    ctx.DestroyReason = $"丢锁超时 lostTicks={trackingLostTicks}";
                    return;
                }

                // 尝试重锁
                if (cfg.allowRetarget)
                {
                    TrySearchNewTarget(host, host.Position, cfg);
                    if (IsTargetValid(host.TrackingTarget))
                    {
                        trackingLostTicks = 0;
                        ctx.RequestPhaseChange = FlightPhase.Tracking;
                    }
                }
            }
            else if (ctx.CurrentPhase != FlightPhase.TrackingLost)
            {
                trackingLostTicks = 0;
            }
        }

        // ══════════════════════════════════════════
        //  IBDPFlightIntentProvider — 核心追踪逻辑
        // ══════════════════════════════════════════

        public void ProvideIntent(Bullet_BDP host, ref FlightIntentContext ctx)
        {
            var cfg = GetConfig(host);
            if (cfg == null) return;

            // 最终进近阶段——不再重定向，让ticksToImpact自然归零
            if (finalApproach) return;

            // 激活条件：Phase == FinalApproach || Phase == Tracking || Phase == Direct（纯追踪弹）
            bool canActivate = ctx.CurrentPhase == FlightPhase.FinalApproach
                || ctx.CurrentPhase == FlightPhase.Tracking
                || ctx.CurrentPhase == FlightPhase.Direct;

            if (!canActivate)
                return;

            // 需要有效目标
            if (!IsTargetValid(host.TrackingTarget))
                return;

            Vector3 currentPos = host.DrawPos;

            // 计算到目标的原始位置（用于距离判断和初始距离记录）
            Vector3 targetPos = host.TrackingTarget.Thing != null
                ? host.TrackingTarget.Thing.DrawPos
                : host.TrackingTarget.Cell.ToVector3Shifted();
            Vector3 toTargetRaw = (targetPos - currentPos).Yto0();
            if (toTargetRaw.sqrMagnitude < 0.001f) return;
            float rawDistToTarget = toTargetRaw.magnitude;

            // 记录初始距离（用原始距离，不受预测影响）
            if (initialDistance <= 0f)
                initialDistance = rawDistToTarget;

            // 追踪激活——距离比例 + 最低tick保底
            if (cfg.trackingStartRatio > 0f
                && rawDistToTarget > initialDistance * cfg.trackingStartRatio)
                return;
            if (flyingTicks < cfg.trackingDelay) return;

            // ── 速度外推预测（三种模式通用） ──
            Vector3 predictedPos = targetPos;
            if (cfg.enablePrediction && lastTargetPosValid
                && host.TrackingTarget.Thing is Pawn pTarget
                && pTarget.pather != null && pTarget.pather.Moving)
            {
                Vector3 velocity = targetPos - lastTargetPos;
                predictedPos = targetPos + velocity * cfg.predictionTicks;
            }
            lastTargetPos = targetPos;
            lastTargetPosValid = true;

            // 用预测位置计算追踪方向
            Vector3 toTarget = (predictedPos - currentPos).Yto0();
            if (toTarget.sqrMagnitude < 0.001f) return;
            float distToTarget = toTarget.magnitude;

            // 初始化角度
            if (!angleInitialized)
            {
                Vector3 dir = (ctx.CurrentDestination - currentPos).Yto0();
                if (dir.sqrMagnitude > 0.001f)
                    currentAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                angleInitialized = true;
            }

            // 计算期望角度
            float desiredAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

            // 脱锁检查
            float angleDiff = Mathf.DeltaAngle(currentAngle, desiredAngle);
            if (Mathf.Abs(angleDiff) > cfg.maxLockAngle)
            {
                if (!cfg.allowRetarget)
                {
                    return;
                }
                TrySearchNewTarget(host, currentPos.ToIntVec3(), cfg);
                if (!IsTargetValid(host.TrackingTarget))
                {
                    return;
                }

                // 找到新目标，重新计算（预测数据下一tick才有效）
                targetPos = host.TrackingTarget.Thing.DrawPos;
                predictedPos = targetPos;
                lastTargetPos = targetPos;
                toTarget = (predictedPos - currentPos).Yto0();
                desiredAngle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                angleDiff = Mathf.DeltaAngle(currentAngle, desiredAngle);
                distToTarget = toTarget.magnitude;
                rawDistToTarget = (targetPos - currentPos).Yto0().magnitude;
            }

            float speedPerTick = host.EffectiveSpeedTilesPerTick;

            // ══════════════════════════════════════════
            //  贝塞尔模式——独立分支，产出ExactPosition意图
            // ══════════════════════════════════════════
            if (cfg.turnMode == TrackingTurnMode.Bezier)
            {
                // 极近距离命中保证（用原始距离判断，不受预测影响）
                if (rawDistToTarget <= speedPerTick * 1.5f)
                {
                    finalApproach = true;
                    finalApproachEntries++;
                    if (TrackingDiag.Enabled)
                        Log.Message($"[BDP-Track] finalApproach#{finalApproachEntries} dist={rawDistToTarget:F2} spd={speedPerTick:F3} tick={flyingTicks}");
                    currentAngle = desiredAngle;
                    Vector3 snapDest = currentPos + toTargetRaw.normalized * rawDistToTarget;
                    ctx.Intent = new FlightIntent
                    {
                        TargetPosition = snapDest,
                        TrackingActivated = true,
                        ExactPosition = true
                    };
                    hadTrackingLock = true;
                    return;
                }

                // 贝塞尔精确位置
                Vector3 nextPos = ComputeBezierNextPosition(
                    currentPos, predictedPos, speedPerTick, cfg.bezierControlRatio);
                ctx.Intent = new FlightIntent
                {
                    TargetPosition = nextPos,
                    TrackingActivated = true,
                    ExactPosition = true
                };
                hadTrackingLock = true;
                return;
            }

            // ══════════════════════════════════════════
            //  Simple / Smooth 模式——角度转向 + 远距离意图点
            // ══════════════════════════════════════════

            // 末段转速加成
            float turnMult = 1f;
            if (cfg.finalPhaseTurnMult > 1f && initialDistance > 0f
                && rawDistToTarget <= initialDistance * cfg.finalPhaseRatio)
            {
                turnMult = cfg.finalPhaseTurnMult;
            }

            if (cfg.turnMode == TrackingTurnMode.Simple)
                UpdateAngleSimple(angleDiff, cfg, turnMult);
            else
                UpdateAngleSmooth(angleDiff, cfg, turnMult);

            // 从当前位置出发，沿追踪方向设置新弹道
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 newDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            // 极近距离命中保证（用原始距离判断）
            if (rawDistToTarget <= speedPerTick * 1.5f)
            {
                finalApproach = true;
                finalApproachEntries++;
                if (TrackingDiag.Enabled)
                    Log.Message($"[BDP-Track] finalApproach#{finalApproachEntries} dist={rawDistToTarget:F2} spd={speedPerTick:F3} tick={flyingTicks}");
                currentAngle = desiredAngle;
                // 产出意图：飞向目标精确位置
                Vector3 snapDest = currentPos + toTargetRaw.normalized * rawDistToTarget;
                ctx.Intent = new FlightIntent
                {
                    TargetPosition = snapDest,
                    TrackingActivated = true
                };
                hadTrackingLock = true;
                return;
            }

            // 正常追踪：沿追踪方向产出意图
            // 远距离时目标点设在远处（宿主ApplyFlightRedirect会处理距离策略）
            Vector3 intentDest = currentPos + newDir * Mathf.Max(distToTarget, speedPerTick * 60f);
            ctx.Intent = new FlightIntent
            {
                TargetPosition = intentDest,
                TrackingActivated = true
            };
            hadTrackingLock = true;
        }

        /// <summary>Simple模式：角速度限幅。turnMult=末段转速倍率。</summary>
        private void UpdateAngleSimple(float angleDiff, BDPTrackingConfig cfg, float turnMult = 1f)
        {
            float maxTurn = cfg.maxTurnRate * turnMult;
            float turn = Mathf.Clamp(angleDiff, -maxTurn, maxTurn);
            currentAngle += turn;
        }

        /// <summary>Smooth模式：角加速度 + 阻尼。turnMult=末段转速倍率。</summary>
        private void UpdateAngleSmooth(float angleDiff, BDPTrackingConfig cfg, float turnMult = 1f)
        {
            float maxTurn = cfg.maxTurnRate * turnMult;
            float accel = Mathf.Clamp(angleDiff, -cfg.angularAccel, cfg.angularAccel);
            angularVelocity += accel;
            angularVelocity = Mathf.Clamp(angularVelocity, -maxTurn, maxTurn);
            angularVelocity *= cfg.damping;
            currentAngle += angularVelocity;
        }

        /// <summary>
        /// 二次贝塞尔下一帧位置。
        /// P0=当前位置，P1=P0+当前方向×控制距离，P2=目标位置。
        /// 返回沿曲线前进 speedPerTick 后的精确位置，并更新 currentAngle。
        /// </summary>
        private Vector3 ComputeBezierNextPosition(
            Vector3 currentPos, Vector3 targetPos, float speedPerTick, float controlRatio)
        {
            // 当前飞行方向（从currentAngle推导）
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 currentDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

            // 控制点：沿当前方向延伸，距离 = 到目标距离 × controlRatio
            float distToTarget = (targetPos - currentPos).Yto0().magnitude;
            float controlDist = distToTarget * controlRatio;
            Vector3 P0 = currentPos;
            Vector3 P1 = currentPos + currentDir * controlDist;
            Vector3 P2 = targetPos;

            // 弦长近似曲线长度：(|P0P1| + |P1P2| + |P0P2|) / 2
            float len01 = (P1 - P0).magnitude;
            float len12 = (P2 - P1).magnitude;
            float len02 = (P2 - P0).magnitude;
            float curveLength = (len01 + len12 + len02) * 0.5f;
            if (curveLength < 0.001f) return targetPos;

            // 采样 t 值：沿曲线前进 speedPerTick 的比例
            float t = Mathf.Clamp01(speedPerTick / curveLength);

            // B(t) = (1-t)²P0 + 2(1-t)tP1 + t²P2
            float oneMinusT = 1f - t;
            Vector3 nextPos = oneMinusT * oneMinusT * P0
                            + 2f * oneMinusT * t * P1
                            + t * t * P2;
            nextPos.y = currentPos.y;  // 保持高度不变

            // 更新 currentAngle（下一帧方向）
            Vector3 newDir = (nextPos - currentPos).Yto0();
            if (newDir.sqrMagnitude > 0.0001f)
                currentAngle = Mathf.Atan2(newDir.x, newDir.z) * Mathf.Rad2Deg;

            return nextPos;
        }

        // ══════════════════════════════════════════
        //  IBDPHitResolver — 命中修正
        // ══════════════════════════════════════════

        public void ResolveHit(Bullet_BDP host, ref HitContext ctx)
        {
            var cfg = GetConfig(host);
            if (cfg == null) return;

            // 追踪过期（TrackingLost/Free）打地面——防止vanilla无视距离命中usedTarget
            if (ctx.CurrentPhase == FlightPhase.TrackingLost
                || ctx.CurrentPhase == FlightPhase.Free)
            {
                ctx.ForceGround = true;
                if (TrackingDiag.Enabled)
                    Log.Message($"[BDP-Track] HitResolve ForceGround Phase={ctx.CurrentPhase}");
                return;
            }

            // 极近距离命中保证——追踪中且目标有效时覆盖usedTarget
            if ((ctx.CurrentPhase == FlightPhase.Tracking
                || ctx.CurrentPhase == FlightPhase.FinalApproach)
                && IsTargetValid(host.TrackingTarget))
            {
                ctx.OverrideTarget = host.TrackingTarget;
                if (TrackingDiag.Enabled)
                    Log.Message($"[BDP-Track] HitResolve Override={host.TrackingTarget} Phase={ctx.CurrentPhase}");
            }
        }

        // ══════════════════════════════════════════
        //  IBDPArrivalPolicy — 到达时继续追踪
        // ══════════════════════════════════════════

        public void DecideArrival(Bullet_BDP host, ref ArrivalContextV5 ctx)
        {
            var cfg = GetConfig(host);
            if (cfg == null) return;

            // 仅在追踪相关Phase时处理
            bool isTrackingPhase = ctx.CurrentPhase == FlightPhase.Tracking
                || ctx.CurrentPhase == FlightPhase.FinalApproach;
            if (!isTrackingPhase) return;
            if (!IsTargetValid(host.TrackingTarget)) return;

            Vector3 targetPos = host.TrackingTarget.Thing != null
                ? host.TrackingTarget.Thing.DrawPos
                : host.TrackingTarget.Cell.ToVector3Shifted();
            float distToTarget = (targetPos - host.DrawPos).Yto0().magnitude;

            // 目标足够近（<1格），让vanilla Impact保证命中
            if (distToTarget < 1f)
            {
                if (TrackingDiag.Enabled && arrivalContinueStreak > 0)
                    Log.Message($"[BDP-Track] ArrivalImpact dist={distToTarget:F2} streak={arrivalContinueStreak} faEntries={finalApproachEntries}");
                arrivalContinueStreak = 0;
                return;
            }

            // 沿追踪方向重定向继续飞行
            bool wasFinalApproach = finalApproach;
            finalApproach = false;
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 trackDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            ctx.Continue = true;
            ctx.NextDestination = host.DrawPos + trackDir * distToTarget;
            arrivalContinueStreak++;
            if (TrackingDiag.Enabled)
                Log.Message($"[BDP-Track] ArrivalContinue#{arrivalContinueStreak} dist={distToTarget:F2} fa={wasFinalApproach}→false Phase={ctx.CurrentPhase}");
            // 保持当前Phase
        }

        // ══════════════════════════════════════════
        //  辅助方法
        // ══════════════════════════════════════════

        /// <summary>获取配置（优先用构造时传入的，回退到def上的）。</summary>
        private BDPTrackingConfig GetConfig(Bullet_BDP host)
        {
            return config ?? host.def.GetModExtension<BDPTrackingConfig>();
        }

        /// <summary>目标是否有效（活着、在地图上）。</summary>
        private static bool IsTargetValid(LocalTargetInfo target)
        {
            if (!target.IsValid) return false;
            if (target.Thing == null) return false;
            if (target.Thing.Destroyed) return false;
            if (!target.Thing.Spawned) return false;
            if (target.Thing is Pawn p && (p.Dead || p.Downed)) return false;
            return true;
        }

        /// <summary>尝试搜索新目标。</summary>
        private void TrySearchNewTarget(Bullet_BDP host, IntVec3 position,
            BDPTrackingConfig cfg)
        {
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick - lastSearchTick < cfg.searchInterval) return;
            lastSearchTick = currentTick;

            var newTarget = TargetSearcher.FindNearestEnemy(
                host.Map, position, cfg.searchRadius, host.Launcher);
            if (newTarget.IsValid)
            {
                host.TrackingTarget = newTarget;
                hadTrackingLock = true;
                angleInitialized = false;
            }
        }

        // ══════════════════════════════════════════
        //  序列化
        // ══════════════════════════════════════════

        public void ExposeData()
        {
            Scribe_Values.Look(ref currentAngle, "trackingAngle", 0f);
            Scribe_Values.Look(ref angularVelocity, "trackingAngVel", 0f);
            Scribe_Values.Look(ref flyingTicks, "trackingFlyingTicks", 0);
            Scribe_Values.Look(ref angleInitialized, "trackingAngleInit", false);
            Scribe_Values.Look(ref lastSearchTick, "trackingLastSearch", 0);
            Scribe_Values.Look(ref finalApproach, "trackingFinalApproach", false);
            Scribe_Values.Look(ref initialDistance, "trackingInitialDist", 0f);
            Scribe_Values.Look(ref trackingLostTicks, "trackingLostTicks", 0);
            Scribe_Values.Look(ref hadTrackingLock, "trackingHadLock", false);
            Scribe_Values.Look(ref arrivalContinueStreak, "trackingArrivalStreak", 0);
            Scribe_Values.Look(ref finalApproachEntries, "trackingFAEntries", 0);
            Scribe_Values.Look(ref lastTargetPos, "trackingLastTargetPos");
            Scribe_Values.Look(ref lastTargetPosValid, "trackingLastTargetPosValid", false);
        }
    }
}
