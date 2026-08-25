using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Aim;
using BDP.Core.AttackExecution.RangedProtocol.Fire;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.AttackExecution.RangedProtocol.Prepare;
using BDP.Core.AttackExecution.RangedProtocol.ProjectileInit;
using BDP.Core.CombatModel;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol
{
    /// <summary>
    /// 远程攻击协议前半段总入口。
    /// 它负责把 AttackExecution 已解析好的请求串成 Entry/Aim/Prepare/Fire/ProjectileInit 五段。
    /// </summary>
    internal sealed class RangedAttackProtocolService
    {
        /// <summary>
        /// 当前攻击协议持有的瞄准阶段服务。
        /// </summary>
        private readonly List<IAimStageModule> aimModules;

        /// <summary>
        /// 当前攻击协议持有的准备阶段服务。
        /// </summary>
        private readonly List<IPrepareStageModule> prepareModules;

        /// <summary>
        /// 当前攻击协议持有的发射阶段服务。
        /// </summary>
        private readonly List<IFireStageModule> fireModules;

        /// <summary>
        /// 当前攻击协议持有的投射物初始化阶段服务。
        /// </summary>
        private readonly List<IProjectileInitStageModule> projectileInitModules;

        /// <summary>
        /// 当前攻击协议持有的模块运行时宿主。
        /// </summary>
        private readonly RangedAttackModuleRuntimeHost rangedAttackModuleRuntimeHost;

        /// <summary>
        /// dual 当前动作步里按来源切出来的一条单侧泳道。
        /// 它只承认单一来源的 cast/emit 真值，不再携带复合混合面。
        /// </summary>
        private sealed class DualSourceLane
        {
            /// <summary>
            /// 当前泳道绑定的来源结果标识。
            /// </summary>
            public string SourceResultId { get; set; }

            /// <summary>
            /// 当前泳道绑定的单侧正式结果。
            /// </summary>
            public FormalExpressionResult SourceResult { get; set; }

            /// <summary>
            /// 当前泳道实际参与本步的 cast 集合。
            /// </summary>
            public List<AttackExecutionCast> Casts { get; } = new List<AttackExecutionCast>();

            /// <summary>
            /// 当前泳道实际参与本步的 emit 集合。
            /// </summary>
            public List<AttackExecutionEmit> Emits { get; } = new List<AttackExecutionEmit>();
        }

        /// <summary>
        /// dual 单侧泳道成功跑完协议后的结果包。
        /// 它把泳道、单侧入口与协议产物绑在一起，便于最后按外层顺序合并。
        /// </summary>
        private sealed class SuccessfulDualLaneProtocol
        {
            /// <summary>
            /// 当前成功结果对应的来源泳道。
            /// </summary>
            public DualSourceLane Lane { get; set; }

            /// <summary>
            /// 当前泳道实际运行使用的单侧协议入口。
            /// </summary>
            public RangedAttackEntry Entry { get; set; }

            /// <summary>
            /// 当前泳道跑出来的正式协议结果。
            /// </summary>
            public RangedAttackProtocolResult Protocol { get; set; }
        }

        internal RangedAttackProtocolService(
            RangedAttackModuleRuntimeHost rangedAttackModuleRuntimeHost,
            IEnumerable<IAimStageModule> aimModules,
            IEnumerable<IPrepareStageModule> prepareModules,
            IEnumerable<IFireStageModule> fireModules,
            IEnumerable<IProjectileInitStageModule> projectileInitModules)
        {
            this.rangedAttackModuleRuntimeHost = rangedAttackModuleRuntimeHost;
            this.aimModules = aimModules != null ? new List<IAimStageModule>(aimModules) : new List<IAimStageModule>();
            this.prepareModules = prepareModules != null ? new List<IPrepareStageModule>(prepareModules) : new List<IPrepareStageModule>();
            this.fireModules = fireModules != null ? new List<IFireStageModule>(fireModules) : new List<IFireStageModule>();
            this.projectileInitModules = projectileInitModules != null ? new List<IProjectileInitStageModule>(projectileInitModules) : new List<IProjectileInitStageModule>();
        }

        /// <summary>
        /// 把一次已准备好的攻击执行请求整理成远程协议可消费的完整结果。
        /// 任一阶段中止时，也会把已得到的阶段结果带回给上层。
        /// </summary>
        public bool TryBuild(AttackExecutionPreparedContext request, AttackRuntimeStep step, FormalExpressionResult result, out RangedAttackProtocolResult protocolResult)
        {
            protocolResult = null;
            RangedAttackEntry entry = BuildEntry(request, step, result);
            if (entry == null || !entry.IsValid)
            {
                return false;
            }

            if (ShouldUseDualSourceLaneIsolation(entry))
            {
                return TryBuildDualSourceLaneProtocol(request, step, result, entry, out protocolResult);
            }

            return TryBuildFromEntry(request, result, entry, out protocolResult);
        }

        /// <summary>
        /// 按单一协议入口跑完整个远程前半段。
        /// 非 dual 基线与 dual 单侧泳道都统一复用这一条单入口链路。
        /// </summary>
        private bool TryBuildFromEntry(
            AttackExecutionPreparedContext request,
            FormalExpressionResult fallbackResult,
            RangedAttackEntry entry,
            out RangedAttackProtocolResult protocolResult)
        {
            protocolResult = null;
            if (entry == null || !entry.IsValid)
            {
                return false;
            }

            if (entry.ModuleSession == null)
            {
                entry.ModuleSession = CreateModuleSession(request, entry.SessionResult ?? fallbackResult);
            }

            entry.AttackContext = entry.ModuleSession != null
                ? entry.ModuleSession.AttackContext
                : AttackContext.FromSnapshot(request != null ? request.AttackContextSnapshot : null);
            AimRecord aim = CreateAimStageService(entry.ModuleSession).Execute(entry);
            if (aim.IsAborted)
            {
                protocolResult = new RangedAttackProtocolResult
                {
                    Entry = entry,
                    Aim = aim
                };
                return false;
            }

            PrepareRecord prepare = CreatePrepareStageService(entry.ModuleSession).Execute(entry, aim);
            if (prepare.IsAborted)
            {
                protocolResult = new RangedAttackProtocolResult
                {
                    Entry = entry,
                    Aim = aim,
                    Prepare = prepare
                };
                return false;
            }

            FireRecord fire = CreateFireStageService(entry.ModuleSession).Execute(entry, aim, prepare);
            if (fire.IsAborted || fire.Emits == null || fire.Emits.Count == 0)
            {
                protocolResult = new RangedAttackProtocolResult
                {
                    Entry = entry,
                    Aim = aim,
                    Prepare = prepare,
                    Fire = fire
                };
                return false;
            }

            IReadOnlyList<ProjectileInitPlan> projectilePlans = CreateProjectileInitStageService(entry.ModuleSession).Execute(entry, aim, prepare, fire);
            if (projectilePlans == null || projectilePlans.Count == 0)
            {
                protocolResult = new RangedAttackProtocolResult
                {
                    Entry = entry,
                    Aim = aim,
                    Prepare = prepare,
                    Fire = fire
                };
                return false;
            }

            AttackExecutionDiagnostics.LogRangedProjectilePlanSummary(
                entry.Pawn,
                "single_or_lane",
                entry.SessionResultId,
                entry.SourceResultId,
                entry.AttackInstanceId,
                projectilePlans);

            protocolResult = new RangedAttackProtocolResult
            {
                Entry = entry,
                Aim = aim,
                Prepare = prepare,
                Fire = fire,
                ProjectilePlans = projectilePlans,
                VerbEmissionPlan = BuildVerbEmissionPlan(entry, projectilePlans),
                ProjectionSeed = BuildProjectionSeed(entry, aim, fire)
            };
            return true;
        }

        /// <summary>
        /// 判断当前入口是否应该启用 dual 来源泳道隔离。
        /// 这里只看是否为 dual 复合结果，以及当前动作步里是否真的带出了来源真值。
        /// </summary>
        private static bool ShouldUseDualSourceLaneIsolation(RangedAttackEntry entry)
        {
            if (entry?.SessionResult == null
                || entry.RuntimeStep == null
                || entry.SessionResult.CompositeKind != CompositeExpressionKind.DualWeapon)
            {
                return false;
            }

            if (entry.RuntimeStep.Emits != null)
            {
                for (int i = 0; i < entry.RuntimeStep.Emits.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(entry.RuntimeStep.Emits[i]?.SourceResultId))
                    {
                        return true;
                    }
                }
            }

            if (entry.RuntimeStep.Casts != null)
            {
                for (int i = 0; i < entry.RuntimeStep.Casts.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(entry.RuntimeStep.Casts[i]?.ResultId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 按 dual 来源泳道分别运行单侧协议，再把成功泳道合并回一份外层正式结果。
        /// 这样 dual 只负责双侧编排，不再用一份混合会话驱动两侧模块。
        /// </summary>
        private bool TryBuildDualSourceLaneProtocol(
            AttackExecutionPreparedContext request,
            AttackRuntimeStep step,
            FormalExpressionResult result,
            RangedAttackEntry outerEntry,
            out RangedAttackProtocolResult protocolResult)
        {
            protocolResult = null;
            List<DualSourceLane> lanes = CollectDualSourceLanes(request, step);
            if (lanes == null || lanes.Count == 0)
            {
                return TryBuildFromEntry(request, result, outerEntry, out protocolResult);
            }

            List<SuccessfulDualLaneProtocol> successfulLaneProtocols = new List<SuccessfulDualLaneProtocol>();
            RangedAttackProtocolResult firstFailedProtocol = null;
            for (int i = 0; i < lanes.Count; i++)
            {
                DualSourceLane lane = lanes[i];
                if (lane?.SourceResult == null)
                {
                    continue;
                }

                RangedAttackEntry laneEntry = BuildDualSourceLaneEntry(request, outerEntry, lane);
                laneEntry.ModuleSession = CreateModuleSession(request, laneEntry.SessionResult);
                bool laneSucceeded = TryBuildFromEntry(request, lane.SourceResult, laneEntry, out RangedAttackProtocolResult laneProtocolResult);
                if (laneSucceeded)
                {
                    successfulLaneProtocols.Add(new SuccessfulDualLaneProtocol
                    {
                        Lane = lane,
                        Entry = laneEntry,
                        Protocol = laneProtocolResult
                    });
                }
                else if (firstFailedProtocol == null && laneProtocolResult != null)
                {
                    firstFailedProtocol = laneProtocolResult;
                }
            }

            if (successfulLaneProtocols.Count == 0)
            {
                protocolResult = firstFailedProtocol;
                return false;
            }

            outerEntry.ModuleSession = successfulLaneProtocols.Count == 1
                ? successfulLaneProtocols[0].Entry.ModuleSession
                : null;
            outerEntry.AttackContext = BuildMergedAttackContext(request, outerEntry, successfulLaneProtocols);
            IReadOnlyList<ProjectileInitPlan> mergedProjectilePlans = MergeProjectilePlansByOuterEmitOrder(outerEntry, successfulLaneProtocols);
            AttackExecutionDiagnostics.LogRangedProjectilePlanSummary(
                request != null ? request.Pawn : null,
                "dual_merged",
                result != null ? result.Id : null,
                outerEntry != null ? outerEntry.SourceResultId : null,
                outerEntry != null ? outerEntry.AttackInstanceId : null,
                mergedProjectilePlans);
            protocolResult = new RangedAttackProtocolResult
            {
                Entry = outerEntry,
                Aim = successfulLaneProtocols[0].Protocol.Aim,
                Prepare = BuildMergedPrepareRecord(successfulLaneProtocols),
                Fire = BuildMergedFireRecord(outerEntry, successfulLaneProtocols, mergedProjectilePlans),
                ProjectilePlans = mergedProjectilePlans,
                VerbEmissionPlan = BuildMergedVerbEmissionPlan(outerEntry, successfulLaneProtocols, mergedProjectilePlans),
                ProjectionSeed = BuildMergedProjectionSeed(outerEntry, successfulLaneProtocols, mergedProjectilePlans)
            };
            return true;
        }

        /// <summary>
        /// 从 dual 动作步里按来源整理出独立泳道。
        /// 先信任 emit 的来源真值；当前步没有 emit 来源时，再退回 cast 来源。
        /// </summary>
        private static List<DualSourceLane> CollectDualSourceLanes(
            AttackExecutionPreparedContext request,
            AttackRuntimeStep step)
        {
            List<DualSourceLane> lanes = new List<DualSourceLane>();
            Dictionary<string, DualSourceLane> laneIndex = new Dictionary<string, DualSourceLane>();

            if (step?.Emits != null)
            {
                for (int i = 0; i < step.Emits.Count; i++)
                {
                    AttackExecutionEmit emit = step.Emits[i];
                    if (string.IsNullOrWhiteSpace(emit?.SourceResultId))
                    {
                        continue;
                    }

                    DualSourceLane lane = GetOrCreateDualSourceLane(
                        request,
                        lanes,
                        laneIndex,
                        emit.SourceResultId,
                        emit.Result);
                    if (lane != null)
                    {
                        lane.Emits.Add(emit);
                    }
                }
            }

            if (lanes.Count > 0)
            {
                if (step?.Casts != null)
                {
                    for (int i = 0; i < step.Casts.Count; i++)
                    {
                        AttackExecutionCast cast = step.Casts[i];
                        if (string.IsNullOrWhiteSpace(cast?.ResultId)
                            || !laneIndex.TryGetValue(cast.ResultId, out DualSourceLane lane))
                        {
                            continue;
                        }

                        lane.Casts.Add(cast);
                    }
                }

                return lanes;
            }

            if (step?.Casts != null)
            {
                for (int i = 0; i < step.Casts.Count; i++)
                {
                    AttackExecutionCast cast = step.Casts[i];
                    if (string.IsNullOrWhiteSpace(cast?.ResultId))
                    {
                        continue;
                    }

                    DualSourceLane lane = GetOrCreateDualSourceLane(
                        request,
                        lanes,
                        laneIndex,
                        cast.ResultId,
                        cast.Result);
                    if (lane != null)
                    {
                        lane.Casts.Add(cast);
                    }
                }
            }

            return lanes;
        }

        /// <summary>
        /// 按单侧来源结果为一条 dual 泳道构造协议入口。
        /// 从这里开始，前半段模块看到的就只剩本侧自己的会话真值。
        /// </summary>
        private static RangedAttackEntry BuildDualSourceLaneEntry(
            AttackExecutionPreparedContext request,
            RangedAttackEntry outerEntry,
            DualSourceLane lane)
        {
            LocalTargetInfo laneTarget = ResolveLaneTarget(
                lane,
                outerEntry != null ? outerEntry.Target : LocalTargetInfo.Invalid);
            LocalTargetInfo laneSemanticTarget = ResolveLaneSemanticTarget(
                lane,
                outerEntry != null ? outerEntry.SemanticTarget : LocalTargetInfo.Invalid);
            AttackRuntimeStep laneStep = new AttackRuntimeStep
            {
                AttackInstanceId = outerEntry != null ? outerEntry.AttackInstanceId : null,
                GroupIndex = outerEntry?.RuntimeStep != null ? outerEntry.RuntimeStep.GroupIndex : 0,
                StepIndex = outerEntry?.RuntimeStep != null ? outerEntry.RuntimeStep.StepIndex : 0,
                WeaponMode = lane.SourceResult.WeaponMode,
                ExecutionKind = outerEntry?.RuntimeStep != null ? outerEntry.RuntimeStep.ExecutionKind : default,
                HostResultId = lane.SourceResult.Id,
                Target = laneTarget,
                Casts = lane.Casts,
                Emits = lane.Emits,
                IntervalTicksAfter = outerEntry?.RuntimeStep != null ? outerEntry.RuntimeStep.IntervalTicksAfter : 0,
                IsPrimarySelection = outerEntry?.RuntimeStep != null && outerEntry.RuntimeStep.IsPrimarySelection
            };

            return new RangedAttackEntry
            {
                AttackInstanceId = outerEntry != null ? outerEntry.AttackInstanceId : null,
                RequestReason = outerEntry != null ? outerEntry.RequestReason : AttackExecutionReason.AutoRanged,
                DispatchIntent = outerEntry != null ? outerEntry.DispatchIntent : default,
                Pawn = outerEntry != null ? outerEntry.Pawn : null,
                Target = laneTarget,
                SemanticTarget = laneSemanticTarget,
                SessionResultId = lane.SourceResult.Id,
                SourceResultId = lane.SourceResult.Id,
                SessionResult = lane.SourceResult,
                SourceResult = lane.SourceResult,
                WeaponMode = lane.SourceResult.WeaponMode,
                ExecutionStyle = lane.SourceResult.ExecutionStyle,
                AttackRole = lane.SourceResult.VerbAttackRole,
                SemanticContext = lane.SourceResult.SemanticContext,
                AttackContext = AttackContext.FromSnapshot(request != null ? request.AttackContextSnapshot : null),
                RuntimeStep = laneStep,
                StepCasts = lane.Casts,
                StepEmits = lane.Emits,
                IsValid = lane.SourceResult.WeaponMode == WeaponExpressionMode.Ranged,
                RejectReason = lane.SourceResult.WeaponMode == WeaponExpressionMode.Ranged ? null : "entry_not_ranged",
                CreatedTick = outerEntry != null ? outerEntry.CreatedTick : -1
            };
        }

        /// <summary>
        /// 在 dual 泳道收集阶段，为指定来源取出或创建一条泳道。
        /// 同一来源只会建立一条泳道，保持首次出现顺序。
        /// </summary>
        private static DualSourceLane GetOrCreateDualSourceLane(
            AttackExecutionPreparedContext request,
            List<DualSourceLane> lanes,
            Dictionary<string, DualSourceLane> laneIndex,
            string sourceResultId,
            FormalExpressionResult fallbackResult)
        {
            if (string.IsNullOrWhiteSpace(sourceResultId))
            {
                return null;
            }

            if (laneIndex.TryGetValue(sourceResultId, out DualSourceLane lane))
            {
                return lane;
            }

            FormalExpressionResult sourceResult = ResolveDualSourceLaneResult(request, sourceResultId, fallbackResult);
            if (sourceResult == null)
            {
                return null;
            }

            lane = new DualSourceLane
            {
                SourceResultId = sourceResultId,
                SourceResult = sourceResult
            };
            lanes.Add(lane);
            laneIndex.Add(sourceResultId, lane);
            return lane;
        }

        /// <summary>
        /// 解析 dual 泳道真正对应的单侧正式结果。
        /// 优先信任请求侧结果索引；索引缺失时才退回 runtime step 已携带的结果引用。
        /// </summary>
        private static FormalExpressionResult ResolveDualSourceLaneResult(
            AttackExecutionPreparedContext request,
            string sourceResultId,
            FormalExpressionResult fallbackResult)
        {
            FormalExpressionResult sourceResult = FindResult(request, sourceResultId);
            if (sourceResult != null)
            {
                return sourceResult;
            }

            return fallbackResult != null && fallbackResult.Id == sourceResultId
                ? fallbackResult
                : null;
        }

        /// <summary>
        /// 读取一条单侧泳道应使用的主目标。
        /// 优先取本泳道 emit，再退回 cast，最后才回退外层目标。
        /// </summary>
        private static LocalTargetInfo ResolveLaneTarget(DualSourceLane lane, LocalTargetInfo fallback)
        {
            if (lane?.Emits != null)
            {
                for (int i = 0; i < lane.Emits.Count; i++)
                {
                    if (lane.Emits[i] != null && lane.Emits[i].Target.IsValid)
                    {
                        return lane.Emits[i].Target;
                    }
                }
            }

            if (lane?.Casts != null)
            {
                for (int i = 0; i < lane.Casts.Count; i++)
                {
                    if (lane.Casts[i] != null && lane.Casts[i].Target.IsValid)
                    {
                        return lane.Casts[i].Target;
                    }
                }
            }

            return fallback;
        }

        /// <summary>
        /// 读取一条单侧泳道应使用的语义目标。
        /// 它优先信任本泳道 emit 已冻结下来的语义目标。
        /// </summary>
        private static LocalTargetInfo ResolveLaneSemanticTarget(DualSourceLane lane, LocalTargetInfo fallback)
        {
            if (lane?.Emits != null)
            {
                for (int i = 0; i < lane.Emits.Count; i++)
                {
                    if (lane.Emits[i] != null && lane.Emits[i].SemanticTarget.IsValid)
                    {
                        return lane.Emits[i].SemanticTarget;
                    }
                }
            }

            return fallback;
        }

        /// <summary>
        /// 按外层动作步 emit 顺序把成功泳道的 projectile 计划重新排成一条正式队列。
        /// 这样 dual 宿主仍只消费一份外层发射计划，但计划内部顺序回到真实编排顺序。
        /// </summary>
        private static IReadOnlyList<ProjectileInitPlan> MergeProjectilePlansByOuterEmitOrder(
            RangedAttackEntry outerEntry,
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols)
        {
            List<ProjectileInitPlan> mergedProjectilePlans = new List<ProjectileInitPlan>();
            Dictionary<string, Queue<ProjectileInitPlan>> projectileQueues = BuildProjectilePlanQueues(successfulLaneProtocols);
            if (outerEntry?.StepEmits != null)
            {
                for (int i = 0; i < outerEntry.StepEmits.Count; i++)
                {
                    string sourceResultId = outerEntry.StepEmits[i]?.SourceResultId;
                    if (string.IsNullOrWhiteSpace(sourceResultId)
                        || !projectileQueues.TryGetValue(sourceResultId, out Queue<ProjectileInitPlan> queue)
                        || queue.Count == 0)
                    {
                        continue;
                    }

                    mergedProjectilePlans.Add(queue.Dequeue());
                }
            }

            AppendRemainingQueuedProjectilePlans(mergedProjectilePlans, successfulLaneProtocols, projectileQueues);
            return mergedProjectilePlans;
        }

        /// <summary>
        /// 为 dual 合并后的外层入口保留可续发的攻击上下文。
        /// 多侧成功时没有单一模块会话，但仍必须保留玩家确认阶段冻结的中性上下文。
        /// </summary>
        private static AttackContext BuildMergedAttackContext(
            AttackExecutionPreparedContext request,
            RangedAttackEntry outerEntry,
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols)
        {
            if (request?.AttackContextSnapshot != null)
            {
                return AttackContext.FromSnapshot(request.AttackContextSnapshot);
            }

            if (outerEntry?.AttackContext != null)
            {
                return AttackContext.FromSnapshot(outerEntry.AttackContext.ToSnapshot());
            }

            if (successfulLaneProtocols != null)
            {
                for (int i = 0; i < successfulLaneProtocols.Count; i++)
                {
                    AttackContext laneContext = successfulLaneProtocols[i]?.Entry?.AttackContext;
                    if (laneContext != null)
                    {
                        return AttackContext.FromSnapshot(laneContext.ToSnapshot());
                    }
                }
            }

            return new AttackContext();
        }

        /// <summary>
        /// 聚合成功泳道的 PrepareRecord。
        /// dual 在这里不再发明新的准备算法，只做明确、可审计的字段归并。
        /// </summary>
        private static PrepareRecord BuildMergedPrepareRecord(IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols)
        {
            PrepareRecord merged = new PrepareRecord
            {
                ResourceCost = 0f,
                MinimumRequired = 0f,
                LockSatisfied = true
            };

            if (successfulLaneProtocols == null)
            {
                return merged;
            }

            for (int i = 0; i < successfulLaneProtocols.Count; i++)
            {
                PrepareRecord prepare = successfulLaneProtocols[i]?.Protocol?.Prepare;
                if (prepare == null)
                {
                    continue;
                }

                merged.ResourceCost += prepare.ResourceCost;
                merged.MinimumRequired = System.Math.Max(merged.MinimumRequired, prepare.MinimumRequired);
                merged.SkipResourceConsumption |= prepare.SkipResourceConsumption;
                merged.WarmupTicks = System.Math.Max(merged.WarmupTicks, prepare.WarmupTicks);
                merged.ChargeTicks = System.Math.Max(merged.ChargeTicks, prepare.ChargeTicks);
                merged.RequiresLock |= prepare.RequiresLock;
                merged.LockSatisfied &= prepare.LockSatisfied;
                AppendTags(merged.Tags, prepare.Tags);
            }

            merged.RequiresWarmup = merged.WarmupTicks > 0;
            merged.RequiresCharge = merged.ChargeTicks > 0;
            return merged;
        }

        /// <summary>
        /// 聚合成功泳道的 FireRecord。
        /// emit 明细按外层顺序重建，FireCount 以最终合并后的 projectile 数量为准。
        /// </summary>
        private static FireRecord BuildMergedFireRecord(
            RangedAttackEntry outerEntry,
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols,
            IReadOnlyList<ProjectileInitPlan> mergedProjectilePlans)
        {
            List<FireEmitRecord> mergedEmits = MergeFireEmitRecordsByOuterEmitOrder(outerEntry, successfulLaneProtocols);
            FireRecord merged = new FireRecord
            {
                ProjectileDef = ResolveMergedProjectileDef(successfulLaneProtocols, mergedProjectilePlans),
                FireCount = mergedProjectilePlans != null && mergedProjectilePlans.Count > 0
                    ? mergedProjectilePlans.Count
                    : mergedEmits.Count,
                Emits = mergedEmits
            };

            if (successfulLaneProtocols != null)
            {
                for (int i = 0; i < successfulLaneProtocols.Count; i++)
                {
                    AppendTags(merged.Tags, successfulLaneProtocols[i]?.Protocol?.Fire?.Tags);
                }
            }

            return merged;
        }

        /// <summary>
        /// 聚合成功泳道的宿主发射计划。
        /// 外层仍只保留一份宿主计划，不把 dual 扩成多宿主分发。
        /// </summary>
        private static RangedVerbEmissionPlan BuildMergedVerbEmissionPlan(
            RangedAttackEntry outerEntry,
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols,
            IReadOnlyList<ProjectileInitPlan> mergedProjectilePlans)
        {
            RangedVerbEmissionWindowPlan windowPlan = new RangedVerbEmissionWindowPlan
            {
                EmissionMode = ResolveVerbEmissionMode(outerEntry),
                ProjectilePlans = mergedProjectilePlans,
                ExpectedEmitCount = mergedProjectilePlans != null ? mergedProjectilePlans.Count : 0
            };
            return new RangedVerbEmissionPlan
            {
                Windows = new List<RangedVerbEmissionWindowPlan> { windowPlan },
                StepAttackInstanceId = outerEntry != null ? outerEntry.AttackInstanceId : null,
                StepHostResultId = outerEntry?.RuntimeStep != null ? outerEntry.RuntimeStep.HostResultId : null,
                StepSourceResultIds = CollectMergedSourceResultIds(outerEntry, successfulLaneProtocols, mergedProjectilePlans),
                ExpectedEmitCount = windowPlan.ExpectedEmitCount
            };
        }

        /// <summary>
        /// 聚合成功泳道的投影种子。
        /// 单成功泳道直接复用单侧真值；多成功泳道才拼成一个最小外层种子。
        /// </summary>
        private static RangedProjectionSeed BuildMergedProjectionSeed(
            RangedAttackEntry outerEntry,
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols,
            IReadOnlyList<ProjectileInitPlan> mergedProjectilePlans)
        {
            if (successfulLaneProtocols != null
                && successfulLaneProtocols.Count == 1
                && successfulLaneProtocols[0].Protocol?.ProjectionSeed != null)
            {
                return CloneProjectionSeed(successfulLaneProtocols[0].Protocol.ProjectionSeed);
            }

            RangedProjectionSeed seed = new RangedProjectionSeed
            {
                AttackInstanceId = outerEntry != null ? outerEntry.AttackInstanceId : null,
                MainTarget = ResolveMergedProjectionTarget(outerEntry, mergedProjectilePlans),
                AttackRole = outerEntry != null ? outerEntry.AttackRole : VerbAttackRole.None,
                SourceResultId = null,
                FireCount = mergedProjectilePlans != null ? mergedProjectilePlans.Count : 0
            };

            if (successfulLaneProtocols != null)
            {
                for (int i = 0; i < successfulLaneProtocols.Count; i++)
                {
                    AimRecord aim = successfulLaneProtocols[i]?.Protocol?.Aim;
                    FireRecord fire = successfulLaneProtocols[i]?.Protocol?.Fire;
                    AppendTags(seed.AimTags, aim != null ? aim.Tags : null);
                    AppendTags(seed.FireTags, fire != null ? fire.Tags : null);
                    AppendTags(seed.VisualHintTags, aim != null ? aim.Tags : null);
                    AppendTags(seed.VisualHintTags, fire != null ? fire.Tags : null);
                    AppendTags(seed.InfoProjectionTags, aim != null ? aim.Tags : null);
                    AppendTags(seed.InfoProjectionTags, fire != null ? fire.Tags : null);
                }
            }

            return seed;
        }

        /// <summary>
        /// 为每条成功泳道建立 projectile 计划队列。
        /// 同一来源的计划严格保持该来源自身原顺序，等待外层顺序来抽取。
        /// </summary>
        private static Dictionary<string, Queue<ProjectileInitPlan>> BuildProjectilePlanQueues(
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols)
        {
            Dictionary<string, Queue<ProjectileInitPlan>> queues = new Dictionary<string, Queue<ProjectileInitPlan>>();
            if (successfulLaneProtocols == null)
            {
                return queues;
            }

            for (int i = 0; i < successfulLaneProtocols.Count; i++)
            {
                SuccessfulDualLaneProtocol successfulLaneProtocol = successfulLaneProtocols[i];
                string sourceResultId = successfulLaneProtocol?.Lane?.SourceResultId;
                if (string.IsNullOrWhiteSpace(sourceResultId))
                {
                    continue;
                }

                if (!queues.TryGetValue(sourceResultId, out Queue<ProjectileInitPlan> queue))
                {
                    queue = new Queue<ProjectileInitPlan>();
                    queues.Add(sourceResultId, queue);
                }

                IReadOnlyList<ProjectileInitPlan> projectilePlans = successfulLaneProtocol?.Protocol?.ProjectilePlans;
                if (projectilePlans == null)
                {
                    continue;
                }

                for (int j = 0; j < projectilePlans.Count; j++)
                {
                    if (projectilePlans[j] != null)
                    {
                        queue.Enqueue(projectilePlans[j]);
                    }
                }
            }

            return queues;
        }

        /// <summary>
        /// 把外层顺序未消耗完的 projectile 计划继续按来源原顺序追加到末尾。
        /// 这样不丢真值，也不打乱每条来源自身内部顺序。
        /// </summary>
        private static void AppendRemainingQueuedProjectilePlans(
            List<ProjectileInitPlan> mergedProjectilePlans,
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols,
            Dictionary<string, Queue<ProjectileInitPlan>> projectileQueues)
        {
            if (mergedProjectilePlans == null
                || successfulLaneProtocols == null
                || projectileQueues == null)
            {
                return;
            }

            for (int i = 0; i < successfulLaneProtocols.Count; i++)
            {
                string sourceResultId = successfulLaneProtocols[i]?.Lane?.SourceResultId;
                if (string.IsNullOrWhiteSpace(sourceResultId)
                    || !projectileQueues.TryGetValue(sourceResultId, out Queue<ProjectileInitPlan> queue))
                {
                    continue;
                }

                while (queue.Count > 0)
                {
                    mergedProjectilePlans.Add(queue.Dequeue());
                }
            }
        }

        /// <summary>
        /// 按外层动作步 emit 顺序重建 FireEmitRecord。
        /// 它和 projectile 合并顺序保持同一来源抽取规则。
        /// </summary>
        private static List<FireEmitRecord> MergeFireEmitRecordsByOuterEmitOrder(
            RangedAttackEntry outerEntry,
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols)
        {
            List<FireEmitRecord> mergedEmits = new List<FireEmitRecord>();
            Dictionary<string, Queue<FireEmitRecord>> emitQueues = BuildFireEmitQueues(successfulLaneProtocols);
            if (outerEntry?.StepEmits != null)
            {
                for (int i = 0; i < outerEntry.StepEmits.Count; i++)
                {
                    string sourceResultId = outerEntry.StepEmits[i]?.SourceResultId;
                    if (string.IsNullOrWhiteSpace(sourceResultId)
                        || !emitQueues.TryGetValue(sourceResultId, out Queue<FireEmitRecord> queue)
                        || queue.Count == 0)
                    {
                        continue;
                    }

                    mergedEmits.Add(CloneFireEmitRecord(queue.Dequeue(), mergedEmits.Count));
                }
            }

            AppendRemainingQueuedFireEmits(mergedEmits, successfulLaneProtocols, emitQueues);
            return mergedEmits;
        }

        /// <summary>
        /// 为每条成功泳道建立 FireEmitRecord 队列。
        /// 队列内部顺序完全沿用该泳道自己的 Fire 阶段结果。
        /// </summary>
        private static Dictionary<string, Queue<FireEmitRecord>> BuildFireEmitQueues(
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols)
        {
            Dictionary<string, Queue<FireEmitRecord>> queues = new Dictionary<string, Queue<FireEmitRecord>>();
            if (successfulLaneProtocols == null)
            {
                return queues;
            }

            for (int i = 0; i < successfulLaneProtocols.Count; i++)
            {
                SuccessfulDualLaneProtocol successfulLaneProtocol = successfulLaneProtocols[i];
                string sourceResultId = successfulLaneProtocol?.Lane?.SourceResultId;
                if (string.IsNullOrWhiteSpace(sourceResultId))
                {
                    continue;
                }

                if (!queues.TryGetValue(sourceResultId, out Queue<FireEmitRecord> queue))
                {
                    queue = new Queue<FireEmitRecord>();
                    queues.Add(sourceResultId, queue);
                }

                List<FireEmitRecord> emits = successfulLaneProtocol?.Protocol?.Fire?.Emits;
                if (emits == null)
                {
                    continue;
                }

                for (int j = 0; j < emits.Count; j++)
                {
                    if (emits[j] != null)
                    {
                        queue.Enqueue(emits[j]);
                    }
                }
            }

            return queues;
        }

        /// <summary>
        /// 把外层顺序未消耗完的 FireEmitRecord 按来源原顺序补到末尾。
        /// </summary>
        private static void AppendRemainingQueuedFireEmits(
            List<FireEmitRecord> mergedEmits,
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols,
            Dictionary<string, Queue<FireEmitRecord>> emitQueues)
        {
            if (mergedEmits == null
                || successfulLaneProtocols == null
                || emitQueues == null)
            {
                return;
            }

            for (int i = 0; i < successfulLaneProtocols.Count; i++)
            {
                string sourceResultId = successfulLaneProtocols[i]?.Lane?.SourceResultId;
                if (string.IsNullOrWhiteSpace(sourceResultId)
                    || !emitQueues.TryGetValue(sourceResultId, out Queue<FireEmitRecord> queue))
                {
                    continue;
                }

                while (queue.Count > 0)
                {
                    mergedEmits.Add(CloneFireEmitRecord(queue.Dequeue(), mergedEmits.Count));
                }
            }
        }

        /// <summary>
        /// 复制一份 FireEmitRecord，并把 emit 序号改成合并后的新顺序。
        /// </summary>
        private static FireEmitRecord CloneFireEmitRecord(FireEmitRecord source, int emitIndex)
        {
            FireEmitRecord clone = new FireEmitRecord
            {
                EmitIndex = emitIndex,
                Target = source != null ? source.Target : LocalTargetInfo.Invalid,
                SemanticTarget = source != null ? source.SemanticTarget : LocalTargetInfo.Invalid,
                OriginOffsetWorld = source != null ? source.OriginOffsetWorld : default,
                SpreadOffsetWorld = source != null ? source.SpreadOffsetWorld : default,
                HasOriginSpreadRange = source != null && source.HasOriginSpreadRange,
                OriginSpreadLateralMin = source != null ? source.OriginSpreadLateralMin : 0f,
                OriginSpreadLateralMax = source != null ? source.OriginSpreadLateralMax : 0f,
                OriginSpreadForwardMin = source != null ? source.OriginSpreadForwardMin : 0f,
                OriginSpreadForwardMax = source != null ? source.OriginSpreadForwardMax : 0f,
                SpeedFactor = source != null ? source.SpeedFactor : 1f,
                DamageFactor = source != null ? source.DamageFactor : 1f,
                StoppingPowerFactor = source != null ? source.StoppingPowerFactor : 1f,
                ProjectileOverride = source != null ? source.ProjectileOverride : null,
                ResultId = source != null ? source.ResultId : null,
                SourceResultId = source != null ? source.SourceResultId : null,
                SourceResult = source != null ? source.SourceResult : null,
                SemanticContext = source != null ? source.SemanticContext : null,
                OriginSide = source != null ? source.OriginSide : null
            };
            AppendTags(clone.Tags, source != null ? source.Tags : null);
            return clone;
        }

        /// <summary>
        /// 解析合并后 FireRecord 应展示的基线投射物定义。
        /// 优先信任最终 projectile 计划，找不到时再退回成功泳道的 FireRecord。
        /// </summary>
        private static ThingDef ResolveMergedProjectileDef(
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols,
            IReadOnlyList<ProjectileInitPlan> mergedProjectilePlans)
        {
            if (mergedProjectilePlans != null)
            {
                for (int i = 0; i < mergedProjectilePlans.Count; i++)
                {
                    if (mergedProjectilePlans[i]?.ProjectileDef != null)
                    {
                        return mergedProjectilePlans[i].ProjectileDef;
                    }
                }
            }

            if (successfulLaneProtocols != null)
            {
                for (int i = 0; i < successfulLaneProtocols.Count; i++)
                {
                    ThingDef projectileDef = successfulLaneProtocols[i]?.Protocol?.Fire?.ProjectileDef;
                    if (projectileDef != null)
                    {
                        return projectileDef;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 收集合并后实际落地的来源结果标识。
        /// 优先信任最终 projectile 计划的来源，其次才回退外层 step 溯源。
        /// </summary>
        private static IReadOnlyList<string> CollectMergedSourceResultIds(
            RangedAttackEntry outerEntry,
            IReadOnlyList<SuccessfulDualLaneProtocol> successfulLaneProtocols,
            IReadOnlyList<ProjectileInitPlan> mergedProjectilePlans)
        {
            List<string> resultIds = new List<string>();
            if (mergedProjectilePlans != null)
            {
                for (int i = 0; i < mergedProjectilePlans.Count; i++)
                {
                    string resultId = mergedProjectilePlans[i]?.ResultId;
                    if (!string.IsNullOrWhiteSpace(resultId) && !resultIds.Contains(resultId))
                    {
                        resultIds.Add(resultId);
                    }
                }
            }

            if (resultIds.Count > 0)
            {
                return resultIds;
            }

            if (successfulLaneProtocols != null)
            {
                for (int i = 0; i < successfulLaneProtocols.Count; i++)
                {
                    string resultId = successfulLaneProtocols[i]?.Lane?.SourceResultId;
                    if (!string.IsNullOrWhiteSpace(resultId) && !resultIds.Contains(resultId))
                    {
                        resultIds.Add(resultId);
                    }
                }
            }

            return resultIds.Count > 0
                ? resultIds
                : CollectStepSourceResultIds(outerEntry);
        }

        /// <summary>
        /// 解析多成功泳道合并后投影种子应使用的主目标。
        /// 优先读最终发射计划，保证下游视觉/信息层看到的目标和真实发射顺序一致。
        /// </summary>
        private static LocalTargetInfo ResolveMergedProjectionTarget(
            RangedAttackEntry outerEntry,
            IReadOnlyList<ProjectileInitPlan> mergedProjectilePlans)
        {
            if (mergedProjectilePlans != null)
            {
                for (int i = 0; i < mergedProjectilePlans.Count; i++)
                {
                    ProjectileInitPlan plan = mergedProjectilePlans[i];
                    if (plan == null)
                    {
                        continue;
                    }

                    if (plan.LaunchTarget.IsValid)
                    {
                        return plan.LaunchTarget;
                    }

                    if (plan.CurrentTarget.IsValid)
                    {
                        return plan.CurrentTarget;
                    }

                    if (plan.AimTarget.IsValid)
                    {
                        return plan.AimTarget;
                    }
                }
            }

            return outerEntry != null
                ? outerEntry.Target
                : LocalTargetInfo.Invalid;
        }

        /// <summary>
        /// 复制一份投影种子，避免外层结果直接复用单侧协议对象的可变引用。
        /// </summary>
        private static RangedProjectionSeed CloneProjectionSeed(RangedProjectionSeed source)
        {
            if (source == null)
            {
                return null;
            }

            RangedProjectionSeed clone = new RangedProjectionSeed
            {
                AttackInstanceId = source.AttackInstanceId,
                MainTarget = source.MainTarget,
                AttackRole = source.AttackRole,
                SourceResultId = source.SourceResultId,
                FireCount = source.FireCount
            };
            AppendTags(clone.AimTags, source.AimTags);
            AppendTags(clone.FireTags, source.FireTags);
            AppendTags(clone.VisualHintTags, source.VisualHintTags);
            AppendTags(clone.InfoProjectionTags, source.InfoProjectionTags);
            return clone;
        }

        /// <summary>
        /// 创建当前会话应使用的 Aim 阶段服务。
        /// </summary>
        private AimStageService CreateAimStageService(RangedAttackModuleSession session)
        {
            return new AimStageService(
                ComposeModules(aimModules, session != null ? session.GetAimModules() : null),
                session != null ? session.GetAddonModules() : null);
        }

        /// <summary>
        /// 创建当前会话应使用的 Prepare 阶段服务。
        /// </summary>
        private PrepareStageService CreatePrepareStageService(RangedAttackModuleSession session)
        {
            return new PrepareStageService(
                ComposeModules(prepareModules, session != null ? session.GetPrepareModules() : null),
                session != null ? session.GetAddonModules() : null);
        }

        /// <summary>
        /// 创建当前会话应使用的 Fire 阶段服务。
        /// </summary>
        private FireStageService CreateFireStageService(RangedAttackModuleSession session)
        {
            return new FireStageService(
                ComposeModules(fireModules, session != null ? session.GetFireModules() : null),
                session != null ? session.GetAddonModules() : null);
        }

        /// <summary>
        /// 创建当前会话应使用的 ProjectileInit 阶段服务。
        /// </summary>
        private ProjectileInitStageService CreateProjectileInitStageService(RangedAttackModuleSession session)
        {
            return new ProjectileInitStageService(
                ComposeModules(projectileInitModules, session != null ? session.GetProjectileInitModules() : null),
                session != null ? session.GetAddonModules() : null);
        }

        /// <summary>
        /// 按当前请求命中的结果重建一份模块运行时会话，并从攻击上下文快照恢复模块私有节点。
        /// 正式执行边界之后不再透出旧碎片字段，只允许通过统一上下文回填冻结节点。
        /// </summary>
        private RangedAttackModuleSession CreateModuleSession(
            AttackExecutionPreparedContext request,
            FormalExpressionResult result)
        {
            if (rangedAttackModuleRuntimeHost == null || request?.Pawn == null || result == null)
            {
                return null;
            }

            RangedAttackModuleSession session = rangedAttackModuleRuntimeHost.CreateSession(request.Pawn, result);
            if (session != null)
            {
                session.AttackContext = BuildSessionAttackContext(request, result, session);
            }
            return session;
        }

        /// <summary>
        /// 为当前会话构建应恢复的统一攻击上下文。
        /// 普通结果直接沿用请求快照；dual 单侧泳道则先复制非模块节点，再把模块私有节点映射回本侧挂载序号。
        /// </summary>
        private static AttackContext BuildSessionAttackContext(
            AttackExecutionPreparedContext request,
            FormalExpressionResult result,
            RangedAttackModuleSession session)
        {
            if (request?.AttackContextSnapshot == null)
            {
                return new AttackContext();
            }

            if (!ShouldRemapDualLanePrivateContexts(request, result, session))
            {
                return AttackContext.FromSnapshot(request.AttackContextSnapshot);
            }

            AttackContext attackContext = new AttackContext();
            CopyNonModuleAttackContextNodes(request.AttackContextSnapshot, attackContext);
            CopyDualLanePrivateContexts(request, result, session, attackContext);
            return attackContext;
        }

        /// <summary>
        /// 判断当前会话是否需要把复合结果快照里的模块私有节点重映射到单侧 lane 挂载空间。
        /// 只有 dual 复合请求切到单侧来源结果时才需要这一步。
        /// </summary>
        private static bool ShouldRemapDualLanePrivateContexts(
            AttackExecutionPreparedContext request,
            FormalExpressionResult result,
            RangedAttackModuleSession session)
        {
            return request?.AttackContextSnapshot != null
                && request.Result != null
                && request.Result.CompositeKind == CompositeExpressionKind.DualWeapon
                && result != null
                && !string.IsNullOrWhiteSpace(result.Id)
                && !string.Equals(request.Result.Id, result.Id, System.StringComparison.Ordinal)
                && request.Result.RangedModules != null
                && session?.Slots != null;
        }

        /// <summary>
        /// 复制请求快照中的非模块节点。
        /// 模块私有节点必须在 dual lane 下按来源结果重新对位，不能直接整份照搬。
        /// </summary>
        private static void CopyNonModuleAttackContextNodes(
            AttackContextSnapshot snapshot,
            AttackContext attackContext)
        {
            if (snapshot == null || attackContext == null)
            {
                return;
            }

            foreach (AttackContextSnapshot.Entry entry in snapshot.GetEntries())
            {
                if (entry?.Node == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                if (entry.Key.StartsWith(AttackContextKeys.ModulePrivatePrefix, System.StringComparison.Ordinal))
                {
                    continue;
                }

                attackContext.Set(entry.Key, entry.Node.Clone());
            }
        }

        /// <summary>
        /// 把复合结果快照中的模块私有节点按当前单侧来源结果重新映射到 lane 会话。
        /// 这样前置 targeting 冻结出来的状态就能回到本侧自己的挂载键位上。
        /// </summary>
        private static void CopyDualLanePrivateContexts(
            AttackExecutionPreparedContext request,
            FormalExpressionResult result,
            RangedAttackModuleSession session,
            AttackContext attackContext)
        {
            if (request?.AttackContextSnapshot == null
                || request.Result?.RangedModules == null
                || result == null
                || session?.Slots == null
                || attackContext == null)
            {
                return;
            }

            for (int i = 0; i < session.Slots.Count; i++)
            {
                var slot = session.Slots[i];
                if (slot == null)
                {
                    continue;
                }

                int compositeMountIndex = ResolveDualCompositeMountIndex(
                    request.Result,
                    result.Id,
                    slot.MountIndex);
                if (compositeMountIndex < 0)
                {
                    continue;
                }

                IAttackContextNode node = request.AttackContextSnapshot.GetNode(
                    AttackContextKeys.GetModulePrivateKey(compositeMountIndex));
                if (node != null)
                {
                    attackContext.Set(AttackContextKeys.GetModulePrivateKey(slot.MountIndex), node.Clone());
                }
            }
        }

        /// <summary>
        /// 按当前单侧来源结果与来源内挂载序号，回溯它在复合结果挂载表中的真实键位。
        /// dual 复合表始终保留“同来源内部顺序不变”的事实，所以这里只做来源内对位换算。
        /// </summary>
        private static int ResolveDualCompositeMountIndex(
            FormalExpressionResult compositeResult,
            string sourceResultId,
            int laneMountIndex)
        {
            if (compositeResult?.RangedModules == null
                || string.IsNullOrWhiteSpace(sourceResultId)
                || laneMountIndex < 0)
            {
                return -1;
            }

            int sourceLocalMountIndex = -1;
            for (int i = 0; i < compositeResult.RangedModules.Count; i++)
            {
                RangedModuleMountConfig mount = compositeResult.RangedModules[i];
                if (mount == null
                    || !string.Equals(mount.sourceResultId, sourceResultId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                sourceLocalMountIndex++;
                if (sourceLocalMountIndex == laneMountIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 合并基线阶段模块与当前会话模块。
        /// 基线总是排在前面，会话模块顺序完全保留作者挂载顺序。
        /// </summary>
        private static IReadOnlyList<TModule> ComposeModules<TModule>(
            IReadOnlyList<TModule> baselineModules,
            IReadOnlyList<TModule> sessionModules)
        {
            List<TModule> result = new List<TModule>();
            AppendModules(result, baselineModules);
            AppendModules(result, sessionModules);
            return result;
        }

        /// <summary>
        /// 把一组阶段模块追加到目标列表。
        /// </summary>
        private static void AppendModules<TModule>(
            List<TModule> target,
            IReadOnlyList<TModule> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    target.Add(source[i]);
                }
            }
        }

        /// <summary>
        /// 把当前 runtime step 的宿主发射语义整理成正式对象。
        /// 协议层在这里明确回答“这一步怎么发”，Verb 宿主只负责执行。
        /// </summary>
        private static RangedVerbEmissionPlan BuildVerbEmissionPlan(
            RangedAttackEntry entry,
            IReadOnlyList<ProjectileInitPlan> projectilePlans)
        {
            RangedVerbEmissionWindowPlan windowPlan = new RangedVerbEmissionWindowPlan
            {
                EmissionMode = ResolveVerbEmissionMode(entry),
                ProjectilePlans = projectilePlans,
                ExpectedEmitCount = entry?.StepEmits != null ? entry.StepEmits.Count : projectilePlans != null ? projectilePlans.Count : 0
            };

            return new RangedVerbEmissionPlan
            {
                Windows = new List<RangedVerbEmissionWindowPlan> { windowPlan },
                StepAttackInstanceId = entry != null ? entry.AttackInstanceId : null,
                StepHostResultId = entry?.RuntimeStep != null ? entry.RuntimeStep.HostResultId : null,
                StepSourceResultIds = CollectStepSourceResultIds(entry),
                ExpectedEmitCount = windowPlan.ExpectedEmitCount
            };
        }

        /// <summary>
        /// 把 AttackExecution 已经准备好的运行时真值压成远程协议入口对象。
        /// </summary>
        private static RangedAttackEntry BuildEntry(AttackExecutionPreparedContext request, AttackRuntimeStep step, FormalExpressionResult result)
        {
            Pawn pawn = request?.Pawn;
            LocalTargetInfo target = step != null && step.Target.IsValid
                ? step.Target
                : request != null ? request.Target : LocalTargetInfo.Invalid;
            LocalTargetInfo semanticTarget = AttackExecutionSemanticTargetResolver.Resolve(request);
            if (pawn == null || result == null || !target.IsValid)
            {
                return new RangedAttackEntry
                {
                    IsValid = false,
                    RejectReason = "entry_invalid"
                };
            }

            FormalExpressionResult sessionResult = ResolveSessionResult(request, step, result);
            FormalExpressionResult sourceResult = ResolvePrimarySourceResult(request, step, result);
            return new RangedAttackEntry
            {
                AttackInstanceId = request.AttackInstanceId,
                RequestReason = request.Request != null ? request.Request.Reason : AttackExecutionReason.AutoRanged,
                DispatchIntent = request.DispatchIntent,
                Pawn = pawn,
                Target = target,
                SemanticTarget = semanticTarget,
                SessionResultId = sessionResult != null ? sessionResult.Id : result.Id,
                SourceResultId = sourceResult != null ? sourceResult.Id : result.Id,
                SessionResult = sessionResult ?? result,
                SourceResult = sourceResult ?? result,
                WeaponMode = sessionResult != null ? sessionResult.WeaponMode : result.WeaponMode,
                ExecutionStyle = sessionResult != null ? sessionResult.ExecutionStyle : result.ExecutionStyle,
                AttackRole = sourceResult != null ? sourceResult.VerbAttackRole : result.VerbAttackRole,
                SemanticContext = sourceResult != null ? sourceResult.SemanticContext : result.SemanticContext,
                AttackContext = request != null
                    ? AttackContext.FromSnapshot(request.AttackContextSnapshot)
                    : new AttackContext(),
                RuntimeStep = step,
                StepCasts = step != null ? step.Casts : null,
                StepEmits = step != null ? step.Emits : null,
                IsValid = (sessionResult != null ? sessionResult.WeaponMode : result.WeaponMode) == WeaponExpressionMode.Ranged,
                RejectReason = (sessionResult != null ? sessionResult.WeaponMode : result.WeaponMode) == WeaponExpressionMode.Ranged ? null : "entry_not_ranged",
                CreatedTick = Find.TickManager != null ? Find.TickManager.TicksGame : -1
            };
        }

        /// <summary>
        /// 生成给投影层消费的基础种子，不在这里重算任何攻击业务。
        /// </summary>
        private static RangedProjectionSeed BuildProjectionSeed(RangedAttackEntry entry, AimRecord aim, FireRecord fire)
        {
            RangedProjectionSeed seed = new RangedProjectionSeed
            {
                AttackInstanceId = entry != null ? entry.AttackInstanceId : null,
                MainTarget = aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid,
                AttackRole = entry != null ? entry.AttackRole : VerbAttackRole.None,
                SourceResultId = entry != null ? entry.SourceResultId : null,
                FireCount = fire != null ? fire.FireCount : 0
            };
            AppendTags(seed.AimTags, aim != null ? aim.Tags : null);
            AppendTags(seed.FireTags, fire != null ? fire.Tags : null);
            AppendTags(seed.VisualHintTags, aim != null ? aim.Tags : null);
            AppendTags(seed.VisualHintTags, fire != null ? fire.Tags : null);
            AppendTags(seed.InfoProjectionTags, aim != null ? aim.Tags : null);
            AppendTags(seed.InfoProjectionTags, fire != null ? fire.Tags : null);
            return seed;
        }

        /// <summary>
        /// 统一把阶段标签追加到目标列表。
        /// </summary>
        private static void AppendTags(List<string> target, List<string> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                {
                    target.Add(source[i]);
                }
            }
        }

        /// <summary>
        /// 根据当前 runtime step 真值裁定宿主该如何消费本步计划。
        /// 这一步只看正式编排结果，不看任何具体业务模块名。
        /// </summary>
        private static RangedVerbEmissionMode ResolveVerbEmissionMode(RangedAttackEntry entry)
        {
            if (entry?.RuntimeStep?.Casts != null && entry.RuntimeStep.Casts.Count > 1)
            {
                return RangedVerbEmissionMode.SimultaneousStep;
            }

            if (entry?.RuntimeStep?.Emits != null && entry.RuntimeStep.Emits.Count > 1)
            {
                return RangedVerbEmissionMode.SimultaneousStep;
            }

            if (entry?.ExecutionStyle?.Single != null
                && entry.ExecutionStyle.Single.RangedRhythm == RangedExecutionRhythm.Simultaneous)
            {
                return RangedVerbEmissionMode.SimultaneousStep;
            }

            return RangedVerbEmissionMode.SequentialBurst;
        }

        /// <summary>
        /// 收集当前动作步涉及到的来源结果标识。
        /// 它只服务诊断和回溯，不构成任何宿主特判条件。
        /// </summary>
        private static IReadOnlyList<string> CollectStepSourceResultIds(RangedAttackEntry entry)
        {
            List<string> resultIds = new List<string>();
            if (entry?.StepEmits != null)
            {
                for (int i = 0; i < entry.StepEmits.Count; i++)
                {
                    string resultId = entry.StepEmits[i]?.SourceResultId;
                    if (string.IsNullOrWhiteSpace(resultId) || resultIds.Contains(resultId))
                    {
                        continue;
                    }

                    resultIds.Add(resultId);
                }
            }

            if (resultIds.Count > 0)
            {
                return resultIds;
            }

            if (entry?.StepCasts != null)
            {
                for (int i = 0; i < entry.StepCasts.Count; i++)
                {
                    string resultId = entry.StepCasts[i]?.ResultId;
                    if (string.IsNullOrWhiteSpace(resultId) || resultIds.Contains(resultId))
                    {
                        continue;
                    }

                    resultIds.Add(resultId);
                }
            }

            return resultIds;
        }

        /// <summary>
        /// 优先按 runtime step 的宿主结果标识回溯正式会话结果。
        /// 找不到时再退回请求侧默认结果。
        /// </summary>
        private static FormalExpressionResult ResolveSessionResult(AttackExecutionPreparedContext request, AttackRuntimeStep step, FormalExpressionResult fallback)
        {
            if (!string.IsNullOrWhiteSpace(step?.HostResultId))
            {
                FormalExpressionResult session = FindResult(request, step.HostResultId);
                if (session != null)
                {
                    return session;
                }
            }

            return request?.Result ?? fallback;
        }

        /// <summary>
        /// 优先按 emit/cast 顺序回溯本步的主来源结果。
        /// 它服务单来源基线，dual 等多来源表象另外从 step 真值读取。
        /// </summary>
        private static FormalExpressionResult ResolvePrimarySourceResult(AttackExecutionPreparedContext request, AttackRuntimeStep step, FormalExpressionResult fallback)
        {
            if (step?.Emits != null)
            {
                for (int i = 0; i < step.Emits.Count; i++)
                {
                    AttackExecutionEmit emit = step.Emits[i];
                    if (!string.IsNullOrWhiteSpace(emit?.SourceResultId))
                    {
                        FormalExpressionResult source = FindResult(request, emit.SourceResultId);
                        if (source != null)
                        {
                            return source;
                        }
                    }
                }
            }

            if (step?.Casts != null)
            {
                for (int i = 0; i < step.Casts.Count; i++)
                {
                    AttackExecutionCast cast = step.Casts[i];
                    if (!string.IsNullOrWhiteSpace(cast?.ResultId))
                    {
                        FormalExpressionResult source = FindResult(request, cast.ResultId);
                        if (source != null)
                        {
                            return source;
                        }
                    }
                }
            }

            return fallback;
        }

        /// <summary>
        /// 从预建结果索引里读取指定结果标识对应的正式结果。
        /// </summary>
        private static FormalExpressionResult FindResult(AttackExecutionPreparedContext request, string resultId)
        {
            if (request?.ResultIndex == null || string.IsNullOrWhiteSpace(resultId))
            {
                return null;
            }

            request.ResultIndex.TryGetValue(resultId, out FormalExpressionResult result);
            return result;
        }
    }
}
