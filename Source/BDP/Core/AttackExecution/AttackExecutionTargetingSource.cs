using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using BDP.Core.VerbHosting;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 把 BDP 正式攻击结果适配进原版 ITargetingSource 的通用桥。
    /// 当前版本只从 BDP 自有 VerbHosting 运行时层读取宿主实例。
    /// </summary>
    internal sealed class AttackExecutionTargetingSource : ITargetingSource
    {
        /// <summary>
        /// 当前适配层在无法借用真实 Verb 时使用的拒绝型目标参数。
        /// 没有真实宿主时不再给出通用攻击 targeting 能力。
        /// </summary>
        private static readonly TargetingParameters UnavailableTargetParams = new TargetingParameters();

        /// <summary>
        /// 当前 targetingSource 绑定的发起 Pawn。
        /// </summary>
        private readonly Pawn pawn;

        /// <summary>
        /// 当前 targetingSource 绑定的正式结果标识。
        /// </summary>
        private readonly string resultId;

        /// <summary>
        /// 当前 targetingSource 回写正式执行边界时使用的来源原因。
        /// </summary>
        private readonly AttackExecutionReason reason;

        /// <summary>
        /// 当前 targetingSource 回写正式执行边界时使用的派单意图。
        /// </summary>
        private readonly AttackDispatchIntent dispatchIntent;

        /// <summary>
        /// 当前 targetingSource 绑定的正式攻击执行入口。
        /// </summary>
        private readonly AttackExecutionService attackExecutionEntry;

        /// <summary>
        /// 当前这次玩家瞄准过程绑定的稳定模块会话。
        /// 从按钮点击到确认/取消结束，都必须沿用这一份。
        /// </summary>
        private readonly RangedAttackModuleSession moduleSession;

        /// <summary>
        /// 当前这次玩家瞄准过程共享的统一攻击上下文。
        /// 目标交互链的输入状态、交互推进状态都只在这里延续。
        /// </summary>
        private readonly AttackContext attackContext;

        /// <summary>
        /// 当前目标交互使用的统一驱动器。
        /// 它负责把一轮输入记录收口成 Targeter 主循环下一步动作。
        /// </summary>
        private readonly TargetingInteractionDriver interactionDriver = new TargetingInteractionDriver();

        /// <summary>
        /// 当前最近一轮输入驱动后的结果。
        /// 它用于让原版 Targeter 决定是否继续停留在目标交互流程里。
        /// </summary>
        private TargetingInteractionDriveResult pendingDriveResult;

        /// <summary>
        /// 用指定正式边界依赖构造一个 targeting 适配源。
        /// </summary>
        public AttackExecutionTargetingSource(
            Pawn pawn,
            string resultId,
            AttackExecutionReason reason,
            AttackDispatchIntent dispatchIntent,
            AttackExecutionService attackExecutionEntry,
            RangedAttackModuleSession moduleSession)
        {
            this.pawn = pawn;
            this.resultId = resultId;
            this.reason = reason;
            this.dispatchIntent = dispatchIntent;
            this.attackExecutionEntry = attackExecutionEntry;
            this.moduleSession = moduleSession;
            attackContext = new AttackContext();
            moduleSession?.ExportPrivateContexts(attackContext);
            if (moduleSession != null)
            {
                moduleSession.AttackContext = attackContext;
            }
        }

        /// <summary>
        /// 当前 targetingSource 绑定的结果标识，仅供组级诊断汇总使用。
        /// </summary>
        internal string DiagnosticResultId => resultId;

        /// <summary>
        /// 当前 targetingSource 绑定的正式来源侧别。
        /// 组级派单只读取正式结果，不根据按钮或列表顺序猜测主副侧；结果失效时返回空。
        /// </summary>
        internal ExpressionOriginKind? ResolvedOriginKind
        {
            get
            {
                FormalExpressionResult result = ResolveCurrentContext().Result;
                return result != null ? result.OriginKind : (ExpressionOriginKind?)null;
            }
        }

        /// <summary>
        /// 当前施法者是否为 Pawn。
        /// </summary>
        public bool CasterIsPawn => true;

        /// <summary>
        /// 当前 targetingSource 是否对应近战攻击。
        /// </summary>
        public bool IsMeleeAttack
        {
            get
            {
                ResolvedTargetingContext context = ResolveCurrentContext();
                if (context.TargetingRecord != null)
                {
                    return context.TargetingRecord.IsMeleeAttack;
                }

                return context.Result != null
                    && context.Result.WeaponMode == WeaponExpressionMode.Melee;
            }
        }

        /// <summary>
        /// 当前 source 是否需要进入目标选择流程。
        /// 手动入口当前仍然走原版 targeting 流程。
        /// </summary>
        public bool Targetable
        {
            get
            {
                ResolvedTargetingContext context = ResolveCurrentContext();
                return context.TargetingRecord == null || context.TargetingRecord.Targetable;
            }
        }

        /// <summary>
        /// 当前 source 是否支持 Shift 多选连续下单。
        /// 第一版保持单次确认，避免在适配层提前扩展多选语义。
        /// </summary>
        public bool MultiSelect
        {
            get
            {
                ResolvedTargetingContext context = ResolveCurrentContext();
                return context.TargetingRecord != null && context.TargetingRecord.MultiSelect;
            }
        }

        /// <summary>
        /// 当前 source 是否隐藏 Pawn tooltip。
        /// </summary>
        public bool HidePawnTooltips
        {
            get
            {
                ResolvedTargetingContext context = ResolveCurrentContext();
                return context.TargetingRecord != null && context.TargetingRecord.HidePawnTooltips;
            }
        }

        /// <summary>
        /// 当前施法者宿主。
        /// </summary>
        public Thing Caster => pawn;

        /// <summary>
        /// 当前施法 Pawn。
        /// </summary>
        public Pawn CasterPawn => pawn;

        /// <summary>
        /// 当前已解析出的真实 Verb。
        /// </summary>
        public Verb GetVerb
        {
            get
            {
                return ResolveCurrentContext().Verb;
            }
        }

        /// <summary>
        /// 当前 targeting UI 要展示的图标。
        /// 优先沿用真实 Verb 的 UIIcon，避免适配层伪造第二套攻击图标真值。
        /// </summary>
        public Texture2D UIIcon
        {
            get
            {
                ResolvedTargetingContext context = ResolveCurrentContext();
                return context.Verb != null ? context.Verb.UIIcon : null;
            }
        }

        /// <summary>
        /// 当前 targeting 使用的目标参数。
        /// 能解析到真实 Verb 时复用原版 Verb.targetParams；否则直接返回拒绝型参数。
        /// </summary>
        public TargetingParameters targetParams
        {
            get
            {
                ResolvedTargetingContext context = ResolveCurrentContext();
                return context.TargetingRecord != null && context.TargetingRecord.TargetingParameters != null
                    ? context.TargetingRecord.TargetingParameters
                    : UnavailableTargetParams;
            }
        }

        /// <summary>
        /// 当前 source 是否还要追加第二段目标选择。
        /// 当本轮驱动要求继续交互时，借用原版 DestinationSelector 机制留在 Targeter 主循环中。
        /// </summary>
        public ITargetingSource DestinationSelector => pendingDriveResult != null && pendingDriveResult.KeepTargeting
            ? this
            : null;

        /// <summary>
        /// 判断当前真实 Verb 是否能命中指定目标。
        /// 裁定顺序:dual 逐侧 → 模块显式接管 → 无人接管时回落原版事实。
        /// </summary>
        public bool CanHitTarget(LocalTargetInfo target)
        {
            ResolvedTargetingContext context = ResolveCurrentContext();
            bool currentTargetLegality;
            if (TryEvaluateDualWeaponTargetLegality(context, target, false, false, out currentTargetLegality))
            {
                return currentTargetLegality;
            }

            if (TryEvaluateCurrentTargetLegality(context, target, false, out currentTargetLegality))
            {
                return currentTargetLegality;
            }

            return CanEnterNeutralTargetingBoundary(context, target);
        }

        /// <summary>
        /// 判断当前目标是否允许正式确认。
        /// 裁定顺序与 CanHitTarget 一致,模块拒绝时按调用方要求反馈原因。
        /// </summary>
        public bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            ResolvedTargetingContext context = ResolveCurrentContext();
            bool currentTargetLegality;
            if (TryEvaluateDualWeaponTargetLegality(context, target, true, showMessages, out currentTargetLegality))
            {
                return currentTargetLegality;
            }

            if (TryEvaluateCurrentTargetLegality(context, target, showMessages, out currentTargetLegality))
            {
                return currentTargetLegality;
            }

            return CanValidateTargetAtNeutralBoundary(context, target, showMessages);
        }

        /// <summary>
        /// 绘制当前目标高亮。
        /// 能解析到真实 Verb 时转发给 Verb；解析失败时不再伪造高亮能力。
        /// </summary>
        public void DrawHighlight(LocalTargetInfo target)
        {
            ResolvedTargetingContext context = ResolveCurrentContext();
            PreviewRecord previewRecord = BuildPreviewRecord(context, target);
            if (context.Verb == null || previewRecord == null)
            {
                return;
            }

            if (PreviewDimensionPolicy.UsesVanilla(previewRecord, PreviewDimension.RangeRing))
            {
                DrawVanillaRangeRing(context.Verb);
            }

            if (!target.IsValid)
            {
                return;
            }

            if (PreviewDimensionPolicy.UsesVanilla(previewRecord, PreviewDimension.TargetHighlight))
            {
                GenDraw.DrawTargetHighlight(target);
            }

            if (PreviewDimensionPolicy.UsesVanilla(previewRecord, PreviewDimension.FieldRadius))
            {
                DrawVanillaFieldRadius(context.Verb, target, context.Result);
            }

            DrawPreviewItems(previewRecord);
        }

        /// <summary>
        /// 把玩家确认的目标正式回写到 AttackExecution。
        /// 这里负责“下单”，不直接把裸 Verb 当正式执行边界。
        /// </summary>
        public void OrderForceTarget(LocalTargetInfo target)
        {
            pendingDriveResult = null;
            ResolvedTargetingContext context = ResolveCurrentContext();
            if (!target.IsValid
                || pawn == null
                || attackExecutionEntry == null
                || string.IsNullOrWhiteSpace(resultId)
                || context == null
                || context.ProjectionVersion <= 0
                || context.Result == null
                || !context.Result.IsAvailable)
            {
                return;
            }

            TargetingRecord targetingRecord = BuildTargetingRecord(
                context.Result,
                context.Verb,
                context.ModuleSession,
                context.AttackContext,
                CreateSelectionInputFrame(target));
            pendingDriveResult = interactionDriver.Drive(targetingRecord);
            if (pendingDriveResult == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(pendingDriveResult.FeedbackMessage))
            {
                ShowInteractionFeedback(pendingDriveResult.FeedbackMessage);
            }

            if (pendingDriveResult.CancelTargeting || !pendingDriveResult.EnterConfirm)
            {
                return;
            }

            ConfirmRecord confirmRecord = BuildConfirmRecord(context, pendingDriveResult.TargetingRecord);
            if (confirmRecord != null && !confirmRecord.IsAllowed)
            {
                ReopenInteractionAfterConfirmRejected(pendingDriveResult.TargetingRecord);
                pendingDriveResult = new TargetingInteractionDriveResult
                {
                    TargetingRecord = pendingDriveResult.TargetingRecord,
                    KeepTargeting = true,
                    FeedbackMessage = confirmRecord.RejectReason
                };
                ShowRejectReason(confirmRecord);
                return;
            }

            if (confirmRecord == null || !confirmRecord.Target.IsValid)
            {
                return;
            }

            attackExecutionEntry.TryExecute(new AttackExecutionRequest
            {
                Pawn = pawn,
                SessionToken = AttackSessionToken.Create(
                    pawn,
                    resultId,
                    confirmRecord.ProjectionVersion),
                AttackContextSnapshot = BuildExecutionAttackContextSnapshot(confirmRecord),
                Target = confirmRecord.Target,
                Reason = confirmRecord.Reason,
                DispatchIntent = confirmRecord.DispatchIntent
            });
        }

        /// <summary>
        /// 把当前驱动层的提示或拒绝原因通过原版消息系统反馈给玩家。
        /// </summary>
        /// <param name="message">当前这一轮输入要反馈的提示文本。</param>
        private void ShowInteractionFeedback(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Messages.Message(message, pawn, MessageTypeDefOf.RejectInput, historical: false);
        }

        /// <summary>
        /// 在确认阶段拒绝下单时，把拒绝原因通过原版消息系统反馈给玩家。
        /// </summary>
        /// <param name="confirmRecord">当前已经裁定完成的确认记录。</param>
        private void ShowRejectReason(ConfirmRecord confirmRecord)
        {
            if (confirmRecord == null)
            {
                return;
            }

            string message = !string.IsNullOrWhiteSpace(confirmRecord.RejectReason)
                ? confirmRecord.RejectReason
                : "bdp_ranged_confirm_rejected";
            Messages.Message(message, pawn, MessageTypeDefOf.RejectInput, historical: false);
        }

        /// <summary>
        /// 绘制当前 targeting 鼠标附着图标。
        /// 正常情况下直接借用真实 Verb.OnGUI；解析失败时只给出不可用反馈。
        /// </summary>
        public void OnGUI(LocalTargetInfo target)
        {
            ResolvedTargetingContext context = ResolveCurrentContext();
            PreviewRecord previewRecord = BuildPreviewRecord(context, target);
            if (context.Verb == null || previewRecord == null)
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
                return;
            }

            if (PreviewDimensionPolicy.UsesVanilla(previewRecord, PreviewDimension.MouseAttachment))
            {
                if (TryEvaluateCurrentTargetLegality(context, target, false, out bool currentTargetIsLegal)
                    && !currentTargetIsLegal)
                {
                    GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
                }
                else
                {
                    context.Verb.OnGUI(target);
                }
            }

            DrawPreviewLabels(previewRecord);
        }

        /// <summary>
        /// 解析当前读取时刻对应的正式结果和正式宿主壳 Verb。
        /// targeting 继续读取原版动态表面，但模块会话必须固定为按钮点击时那一份。
        /// </summary>
        private ResolvedTargetingContext ResolveCurrentContext()
        {
            TriggerCombatProjectionState projection = null;
            int projectionVersion = 0;
            FormalExpressionResult result = null;
            if (pawn == null
                || string.IsNullOrWhiteSpace(resultId)
                || !AttackExecutionSurfaceAccess.TryGetPublishedResult(pawn, resultId, out projection, out result))
            {
                return new ResolvedTargetingContext
                {
                    Projection = projection,
                    ProjectionVersion = projection != null ? projection.ProjectionVersion : 0,
                    Result = moduleSession != null ? moduleSession.Result : result,
                    Verb = null,
                    ModuleSession = moduleSession,
                    AttackContext = attackContext,
                    TargetingRecord = BuildTargetingRecord(
                        moduleSession != null ? moduleSession.Result : result,
                        null,
                        moduleSession,
                        attackContext,
                        null)
                };
            }

            projectionVersion = projection.ProjectionVersion;
            Verb verb = null;
            if (VerbHostSurfaceAccess.TryGetByResultId(pawn, result.Id, out BdpFormalVerbBinding binding))
            {
                verb = binding.ResolveActiveVerb();
            }

            TargetingRecord targetingRecord = BuildTargetingRecord(result, verb, moduleSession, attackContext, null);

            return new ResolvedTargetingContext
            {
                Projection = projection,
                ProjectionVersion = projectionVersion,
                Result = result,
                Verb = verb,
                ModuleSession = moduleSession,
                AttackContext = attackContext,
                TargetingRecord = targetingRecord
            };
        }

        /// <summary>
        /// 构建当前 targeting 阶段记录，并允许模块调整目标选择表面。
        /// </summary>
        private static TargetingRecord BuildTargetingRecord(
            FormalExpressionResult result,
            Verb verb,
            RangedAttackModuleSession moduleSession,
            AttackContext attackContext,
            TargetingInputFrame inputFrame)
        {
            AttackContext resolvedAttackContext = attackContext ?? new AttackContext();
            TargetingInteractionSession interactionSession = resolvedAttackContext.GetOrCreate<TargetingInteractionSession>(AttackContextKeys.TargetingInteraction);
            TargetingInputState inputState = resolvedAttackContext.GetOrCreate<TargetingInputState>(AttackContextKeys.TargetingInputState);
            TargetingInputFrame currentInputFrame = inputFrame ?? new TargetingInputFrame();
            TargetingRecord record = new TargetingRecord
            {
                Pawn = moduleSession != null ? moduleSession.Pawn : null,
                Result = result,
                Verb = verb,
                ModuleSession = moduleSession,
                AttackContext = resolvedAttackContext,
                IsMeleeAttack = verb != null
                    ? verb.IsMeleeAttack
                    : result != null && result.WeaponMode == WeaponExpressionMode.Melee,
                Targetable = true,
                MultiSelect = false,
                HidePawnTooltips = verb != null && verb.HidePawnTooltips,
                TargetingParameters = verb != null ? verb.targetParams : UnavailableTargetParams,
                InputFrame = currentInputFrame,
                AdvanceDecision = BuildDefaultAdvanceDecision(currentInputFrame)
            };

            if (interactionSession != null)
            {
                if (!interactionSession.IsActive && !interactionSession.IsCompleted && !interactionSession.IsCanceled)
                {
                    interactionSession.Activate();
                }

                inputState.IsActive = interactionSession.IsActive;
                inputState.StepIndex = interactionSession.StepIndex;
                inputState.IsComplete = interactionSession.IsCompleted;
            }

            if (moduleSession?.GetTargetingModules() != null)
            {
                IReadOnlyList<ITargetingStageModule> modules = moduleSession.GetTargetingModules();
                for (int i = 0; i < modules.Count; i++)
                {
                    record.CurrentRuntime = modules[i] as IRangedAttackModuleRuntime;
                    modules[i]?.Contribute(record);
                    record.CurrentRuntime = null;
                }
            }

            if (record.Stop.IsRequested)
            {
                record.Targetable = false;
            }

            if (moduleSession != null)
            {
                RangedStageAddonDispatcher.Execute(
                    moduleSession.GetAddonModules(),
                    new RangedStageAddonContext(
                        RangedStageKind.Targeting,
                        record.Pawn,
                        record.Pawn != null ? record.Pawn.Map : null,
                        null,
                        record.ResultId,
                        -1,
                        null,
                        record.Pawn,
                        record.Verb?.EquipmentSource ?? (Thing)record.Pawn,
                        LocalTargetInfo.Invalid,
                        LocalTargetInfo.Invalid,
                        default,
                        null,
                        default,
                        record.Result != null ? record.Result.SemanticContext : null,
                        record.AttackContext?.ToSnapshot()));
            }

            return record;
        }

        /// <summary>
        /// 构建当前预览阶段记录，并允许模块补充反馈。
        /// </summary>
        private static PreviewRecord BuildPreviewRecord(ResolvedTargetingContext context, LocalTargetInfo target)
        {
            PreviewRecord record = new PreviewRecord
            {
                Pawn = context != null && context.ModuleSession != null ? context.ModuleSession.Pawn : null,
                Result = context != null ? context.Result : null,
                Verb = context != null ? context.Verb : null,
                ModuleSession = context != null ? context.ModuleSession : null,
                AttackContext = context != null ? context.AttackContext : null,
                Target = target
            };
            PreviewDimensionPolicy.ApplyBaseline(record);

            if (context?.ModuleSession?.GetPreviewModules() != null)
            {
                IReadOnlyList<IPreviewStageModule> modules = context.ModuleSession.GetPreviewModules();
                for (int i = 0; i < modules.Count; i++)
                {
                    record.CurrentRuntime = modules[i] as IRangedAttackModuleRuntime;
                    modules[i]?.Contribute(record);
                    record.CurrentRuntime = null;
                }
            }

            if (record.Stop.IsRequested)
            {
                record.UseVanillaRangeRing = false;
                record.UseVanillaTargetHighlight = false;
                record.UseVanillaFieldRadius = false;
                record.UseVanillaMouseAttachment = false;
            }

            if (context?.ModuleSession != null)
            {
                RangedStageAddonDispatcher.Execute(
                    context.ModuleSession.GetAddonModules(),
                    new RangedStageAddonContext(
                        RangedStageKind.Preview,
                        record.Pawn,
                        record.Pawn != null ? record.Pawn.Map : null,
                        null,
                        record.ResultId,
                        -1,
                        null,
                        record.Pawn,
                        record.Verb?.EquipmentSource ?? (Thing)record.Pawn,
                        record.Target,
                        record.Target,
                        record.Target.IsValid ? record.Target.CenterVector3 : default,
                        null,
                        default,
                        context.Result != null ? context.Result.SemanticContext : null,
                        record.AttackContext?.ToSnapshot()));
            }

            return record;
        }

        /// <summary>
        /// 为 Gizmo 悬停预览绘制当前攻击的原版射程圈。
        /// 该入口不启动 Targeter，只读取当前已激活武器的真实 Verb。
        /// </summary>
        internal void DrawGizmoRangePreview()
        {
            DrawVanillaRangeRing(ResolveCurrentContext().Verb);
        }

        /// <summary>
        /// 绘制原版射程圈。
        /// </summary>
        private static void DrawVanillaRangeRing(Verb verb)
        {
            if (verb?.verbProps == null || verb.Caster == null)
            {
                return;
            }

            verb.verbProps.DrawRadiusRing(verb.Caster.Position, verb);
        }

        /// <summary>
        /// 绘制原版目标周边范围反馈。
        /// 这里按原版 `Verb.DrawHighlightFieldRadiusAroundTarget` 的公开事实做最小桥接。
        /// </summary>
        private static void DrawVanillaFieldRadius(
            Verb verb,
            LocalTargetInfo target,
            FormalExpressionResult result)
        {
            if (verb?.verbProps == null || verb.Caster == null || !target.IsValid)
            {
                return;
            }

            bool needLosToCenter;
            float radius = verb.HighlightFieldRadiusAroundTarget(out needLosToCenter);
            if (!(radius > 0.2f))
            {
                return;
            }

            ShootLine resultingLine;
            if (verb.TryFindShootLineFromTo(verb.Caster.Position, target, out resultingLine))
            {
                if (needLosToCenter)
                {
                    GenExplosion.RenderPredictedAreaOfEffect(
                        resultingLine.Dest,
                        radius,
                        verb.verbProps.explosionRadiusRingColor);
                    return;
                }

                List<IntVec3> cells = new List<IntVec3>();
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(resultingLine.Dest, radius, useCenter: true))
                {
                    if (cell.InBounds(Find.CurrentMap))
                    {
                        cells.Add(cell);
                    }
                }

                GenDraw.DrawFieldEdges(cells, verb.verbProps.explosionRadiusRingColor);
                return;
            }

            // 原版 Verb 以“射手到目标的直射线”作为爆炸预览前置条件。
            // 路线引导表达允许最终目标不直视，此时实际爆炸仍发生在最终目标格，
            // 因此只在正式规格允许间接目标且目标未超射程时绕过这一个原版前置条件。
            if (result?.ResolvedVerbSpec == null
                || result.ResolvedVerbSpec.RequiresDirectTargetLineOfSight
                || IsOutOfRange(verb, target))
            {
                return;
            }

            GenExplosion.RenderPredictedAreaOfEffect(
                target.Cell,
                radius,
                verb.verbProps.explosionRadiusRingColor);
        }

        /// <summary>
        /// 判断间接路线预览目标是否已经超出原版武器射程。
        /// 绕过直射 LOS 前置条件不等于绕过射程判定。
        /// </summary>
        private static bool IsOutOfRange(Verb verb, LocalTargetInfo target)
        {
            if (verb == null || verb.Caster == null || !target.IsValid)
            {
                return true;
            }

            CellRect occupiedRect = target.HasThing
                ? target.Thing.OccupiedRect()
                : CellRect.SingleCell(target.Cell);
            return verb.OutOfRange(verb.Caster.Position, target, occupiedRect);
        }

        /// <summary>
        /// 构建当前确认阶段记录，并允许模块在正式下单前做最后修正。
        /// 这里同时准备 `Target（导航目标）` 与 `SemanticTarget（语义目标）` 两条口径，避免确认阶段把两类职责混成一值。
        /// </summary>
        private ConfirmRecord BuildConfirmRecord(ResolvedTargetingContext context, TargetingRecord targetingRecord)
        {
            ConfirmRecord record = new ConfirmRecord
            {
                Pawn = pawn,
                Result = context != null ? context.Result : null,
                ModuleSession = context != null ? context.ModuleSession : null,
                AttackContext = context != null ? context.AttackContext : null,
                Target = targetingRecord != null && targetingRecord.InputFrame != null
                    ? targetingRecord.InputFrame.SelectedTarget
                    : LocalTargetInfo.Invalid,
                SemanticTarget = targetingRecord != null && targetingRecord.InputFrame != null
                    ? targetingRecord.InputFrame.SelectedTarget
                    : LocalTargetInfo.Invalid,
                ProjectionVersion = context != null ? context.ProjectionVersion : 0,
                Reason = reason,
                DispatchIntent = dispatchIntent,
                IsAllowed = true
            };

            if (targetingRecord != null && targetingRecord.AdvanceDecision != null)
            {
                if (targetingRecord.AdvanceDecision.IsCanceled)
                {
                    record.IsAllowed = false;
                    record.RejectReason = string.IsNullOrWhiteSpace(targetingRecord.AdvanceDecision.Reason)
                        ? "bdp_targeting_interaction_canceled"
                        : targetingRecord.AdvanceDecision.Reason;
                }
                else if (targetingRecord.AdvanceDecision.IsRejected || !targetingRecord.AdvanceDecision.AllowsConfirm)
                {
                    record.IsAllowed = false;
                    record.RejectReason = string.IsNullOrWhiteSpace(targetingRecord.AdvanceDecision.Reason)
                        ? "bdp_targeting_interaction_rejected"
                        : targetingRecord.AdvanceDecision.Reason;
                }
            }

            if (context?.ModuleSession?.GetConfirmModules() != null)
            {
                IReadOnlyList<IConfirmStageModule> modules = context.ModuleSession.GetConfirmModules();
                for (int i = 0; i < modules.Count; i++)
                {
                    record.CurrentRuntime = modules[i] as IRangedAttackModuleRuntime;
                    modules[i]?.Contribute(record);
                    record.CurrentRuntime = null;
                }
            }

            if (record.Stop.IsRequested)
            {
                record.IsAllowed = false;
                record.RejectReason = record.Stop.Reason;
            }

            if (context?.ModuleSession != null)
            {
                RangedStageAddonDispatcher.Execute(
                    context.ModuleSession.GetAddonModules(),
                    new RangedStageAddonContext(
                        RangedStageKind.Confirm,
                        record.Pawn,
                        record.Pawn != null ? record.Pawn.Map : null,
                        null,
                        record.ResultId,
                        -1,
                        null,
                        record.Pawn,
                        record.Pawn?.equipment?.Primary ?? (Thing)record.Pawn,
                        record.Target,
                        record.Target,
                        record.Target.IsValid ? record.Target.CenterVector3 : default,
                        null,
                        default,
                        record.Result != null ? record.Result.SemanticContext : null,
                        record.AttackContext?.ToSnapshot()));
            }

            return record;
        }

        /// <summary>
        /// 为当前点击目标构造一轮正式输入帧。
        /// 没有模块接管时，它自然退化为原版“单击即完成”。
        /// </summary>
        /// <param name="target">玩家当前点击的目标。</param>
        /// <returns>交给目标交互驱动器消费的一轮输入事实。</returns>
        private static TargetingInputFrame CreateSelectionInputFrame(LocalTargetInfo target)
        {
            TargetingInputRuntimeFacts runtimeFacts = TargetingInputRuntimeScope.Current;
            return new TargetingInputFrame
            {
                HoveredTarget = target,
                SelectedTarget = target,
                PressedButton = runtimeFacts != null ? runtimeFacts.PressedButton : TargetingInputButton.None,
                Modifiers = runtimeFacts != null ? runtimeFacts.Modifiers : TargetingInputModifiers.None,
                ConfirmRequested = target.IsValid
            };
        }

        /// <summary>
        /// 为即时合法性探测构造一轮只读输入帧。
        /// 它保留当前鼠标目标与修饰键事实，但不会把点击语义推进到业务状态里。
        /// </summary>
        /// <param name="target">当前鼠标所在候选目标。</param>
        /// <returns>只服务 CanHitTarget / ValidateTarget 探测的一轮输入事实。</returns>
        private static TargetingInputFrame CreateProbeInputFrame(LocalTargetInfo target)
        {
            TargetingInputRuntimeFacts runtimeFacts = TargetingInputRuntimeScope.Current;
            return new TargetingInputFrame
            {
                HoveredTarget = target,
                SelectedTarget = target,
                PressedButton = TargetingInputButton.None,
                Modifiers = runtimeFacts != null ? runtimeFacts.Modifiers : TargetingInputModifiers.None,
                ConfirmRequested = false
            };
        }

        /// <summary>
        /// 判断当前候选目标是否仍允许留在 Targeter/确认入口边界。
        /// 模块显式接管已在调用点前置裁定；这里只处理无人接管的场景:
        /// 需要目标直射的远程攻击回落原版 Verb 的“现在能否命中”判定；
        /// 允许间接目标的远程攻击只保留目标射程边界，不能在进入 DrawHighlight 前被原版 LOS 拦截；
        /// 近战保持原版“先选中再接近”语义，不要求现在就能命中，避免把“当前一步非法”误写成“整个目标无效”。
        /// </summary>
        /// <param name="context">当前 targeting 解析上下文。</param>
        /// <param name="target">当前候选目标。</param>
        /// <returns>当前候选点仍允许继续瞄准且（远程）能被真实 Verb 命中时返回 true。</returns>
        private static bool CanEnterNeutralTargetingBoundary(
            ResolvedTargetingContext context,
            LocalTargetInfo target)
        {
            if (context == null
                || context.Verb == null
                || context.TargetingRecord == null
                || !context.TargetingRecord.Targetable
                || !target.IsValid)
            {
                return false;
            }

            if (context.TargetingRecord.IsMeleeAttack)
            {
                return true;
            }

            ResolvedVerbSpec resolvedSpec = context.Result?.ResolvedVerbSpec;
            if (resolvedSpec != null && !resolvedSpec.RequiresDirectTargetLineOfSight)
            {
                // 原版 Targeter 会在 DrawHighlight 前调用 CanHitTarget。
                // 间接目标在这里不能再要求射手到最终目标的直射 LOS，
                // 但仍保留原版 Verb 的最小射程/最大射程判断。
                return !IsOutOfRange(context.Verb, target);
            }

            return context.Verb.CanHitTarget(target);
        }

        /// <summary>
        /// 判断当前候选目标是否允许正式确认下单。
        /// 悬停边界负责先把不可直视的远程目标无效化；确认边界沿用原版 Verb 的点击校验（意识形态等），
        /// 不再由适配层自造一套目标裁决。
        /// </summary>
        /// <param name="context">当前 targeting 解析上下文。</param>
        /// <param name="target">当前候选目标。</param>
        /// <param name="showMessages">是否允许 Verb 显示拒绝原因。</param>
        /// <returns>当前候选点仍允许确认且（远程）通过原版校验时返回 true。</returns>
        private static bool CanValidateTargetAtNeutralBoundary(
            ResolvedTargetingContext context,
            LocalTargetInfo target,
            bool showMessages)
        {
            return context != null
                && context.Verb != null
                && context.TargetingRecord != null
                && context.TargetingRecord.Targetable
                && target.IsValid
                && (context.TargetingRecord.IsMeleeAttack || context.Verb.ValidateTarget(target, showMessages));
        }

        /// <summary>
        /// 询问当前鼠标候选点是否已被模块显式裁定合法性。
        /// 这是一条 `current-candidate probe（当前候选点探针）`，宿主只消费“当前候选点合法/非法”的中性真值，不主动理解任何具体模块业务规则，也不把它上升成最终确认结论。
        /// </summary>
        /// <param name="context">当前 targeting 解析上下文。</param>
        /// <param name="target">当前鼠标候选目标。</param>
        /// <param name="showMessages">当前是否允许把拒绝原因显示给玩家。</param>
        /// <param name="isLegal">输出当前候选点是否合法。</param>
        /// <returns>模块已显式接管当前候选点合法性时返回 true。</returns>
        private bool TryEvaluateCurrentTargetLegality(
            ResolvedTargetingContext context,
            LocalTargetInfo target,
            bool showMessages,
            out bool isLegal)
        {
            isLegal = false;
            if (context == null
                || context.Verb == null
                || context.ModuleSession == null
                || context.AttackContext == null
                || !target.IsValid)
            {
                return false;
            }

            TargetingRecord probeRecord = BuildTargetingRecord(
                context.Result,
                context.Verb,
                context.ModuleSession,
                context.AttackContext,
                CreateProbeInputFrame(target));
            if (probeRecord == null || !probeRecord.HasCurrentTargetLegalityOverride)
            {
                return false;
            }

            isLegal = probeRecord.CurrentTargetIsLegal;
            if (!isLegal && showMessages && !string.IsNullOrWhiteSpace(probeRecord.CurrentTargetRejectReason))
            {
                Messages.Message(probeRecord.CurrentTargetRejectReason, MessageTypeDefOf.RejectInput, false);
            }

            return true;
        }

        /// <summary>
        /// 对 dual 复合结果做手动 targeting 合法性裁定。
        /// 手动入口不能再把复合宿主单侧 Verb 的判定误当成 dual 全体真值。
        /// </summary>
        private bool TryEvaluateDualWeaponTargetLegality(
            ResolvedTargetingContext context,
            LocalTargetInfo target,
            bool useValidateTarget,
            bool showMessages,
            out bool isLegal)
        {
            isLegal = false;
            if (context?.Result == null
                || context.Result.CompositeKind != CompositeExpressionKind.DualWeapon
                || context.TargetingRecord == null
                || !context.TargetingRecord.Targetable
                || !target.IsValid)
            {
                return false;
            }

            if (!TryGetDualWeaponCompositeReference(context, out CompositeExpressionReference reference))
            {
                return false;
            }

            bool allowMain = EvaluateDualWeaponSideTargetLegality(context, reference.MainSourceResultId, target, useValidateTarget);
            bool allowSub = EvaluateDualWeaponSideTargetLegality(context, reference.SubSourceResultId, target, useValidateTarget);
            isLegal = allowMain || allowSub;
            if (!isLegal && showMessages)
            {
                ShowDualWeaponRejectReason(context, reference, target);
            }

            return true;
        }

        /// <summary>
        /// 读取当前 dual 复合结果对应的来源引用。
        /// targeting 层必须从发布投影里的复合引用回到各自单侧真值。
        /// </summary>
        private static bool TryGetDualWeaponCompositeReference(
            ResolvedTargetingContext context,
            out CompositeExpressionReference reference)
        {
            reference = null;
            return context?.Projection?.CompositeReferenceIndex != null
                && context.Result != null
                && !string.IsNullOrWhiteSpace(context.Result.Id)
                && context.Projection.CompositeReferenceIndex.TryGetValue(context.Result.Id, out reference)
                && reference != null;
        }

        /// <summary>
        /// 判断 dual 某一侧在当前手动目标上是否仍然合法。
        /// 需要“射手到语义目标必要直射”的侧按自己的 formal host 命中真值裁定；其余侧直接保留到各自模块确认链，不在 dual 适配层提前筛掉。
        /// </summary>
        private bool EvaluateDualWeaponSideTargetLegality(
            ResolvedTargetingContext context,
            string sourceResultId,
            LocalTargetInfo target,
            bool useValidateTarget)
        {
            if (!TryResolveDualWeaponSide(context, sourceResultId, out FormalExpressionResult sourceResult, out Verb sourceVerb))
            {
                AttackExecutionDiagnostics.LogManualDualTargetingSideLegality(
                    pawn,
                    context?.Result != null ? context.Result.Id : null,
                    sourceResultId,
                    target,
                    useValidateTarget,
                    false,
                    false,
                    false,
                    "source_resolve_failed",
                    null);
                return false;
            }

            if (sourceResult.WeaponMode == WeaponExpressionMode.Melee)
            {
                return EvaluateDualWeaponMeleeSideTargetLegality(
                    context,
                    sourceResultId,
                    target,
                    useValidateTarget,
                    sourceVerb);
            }

            return EvaluateDualWeaponRangedSideTargetLegality(
                context,
                sourceResultId,
                target,
                useValidateTarget,
                sourceResult,
                sourceVerb);
        }

        /// <summary>
        /// 判断 dual 某一近战侧在当前手动目标上是否允许进入正式确认。
        /// 近战 side 只要求“目标本身可作为近战对象”，不把“当前站位立刻够到”误当成准入条件。
        /// </summary>
        private bool EvaluateDualWeaponMeleeSideTargetLegality(
            ResolvedTargetingContext context,
            string sourceResultId,
            LocalTargetInfo target,
            bool useValidateTarget,
            Verb sourceVerb)
        {
            bool allowed = target.IsValid
                && target.HasThing
                && sourceVerb != null
                && sourceVerb.ValidateTarget(target, false);
            string reason = !target.IsValid
                ? "invalid_target"
                : !target.HasThing
                    ? "melee_requires_thing_target"
                    : sourceVerb == null
                        ? "binding_has_no_active_verb"
                        : allowed
                            ? "melee_target_allowed"
                            : "melee_target_rejected";
            AttackExecutionDiagnostics.LogManualDualTargetingSideLegality(
                pawn,
                context?.Result != null ? context.Result.Id : null,
                sourceResultId,
                target,
                useValidateTarget,
                resolved: true,
                requiresDirectTargetLos: false,
                allowed,
                reason,
                sourceVerb);
            return allowed;
        }

        /// <summary>
        /// 判断 dual 某一远程侧在当前手动目标上是否仍然合法。
        /// 只有声明了“必要直达目标 LOS”的远程侧，才在 targeting 准入层使用当前宿主 Verb 做额外筛查。
        /// </summary>
        private bool EvaluateDualWeaponRangedSideTargetLegality(
            ResolvedTargetingContext context,
            string sourceResultId,
            LocalTargetInfo target,
            bool useValidateTarget,
            FormalExpressionResult sourceResult,
            Verb sourceVerb)
        {
            ResolvedVerbSpec resolvedSpec = sourceResult.ResolvedVerbSpec;
            if (resolvedSpec != null && resolvedSpec.RequiresDirectTargetLineOfSight)
            {
                bool allowed = useValidateTarget
                    ? sourceVerb.ValidateTarget(target, false)
                    : sourceVerb.CanHitTarget(target);
                AttackExecutionDiagnostics.LogManualDualTargetingSideLegality(
                    pawn,
                    context?.Result != null ? context.Result.Id : null,
                    sourceResultId,
                    target,
                    useValidateTarget,
                    true,
                    true,
                    allowed,
                    allowed ? "required_direct_los_pass" : "required_direct_los_blocked",
                    sourceVerb);
                return allowed;
            }

            AttackExecutionDiagnostics.LogManualDualTargetingSideLegality(
                pawn,
                context?.Result != null ? context.Result.Id : null,
                sourceResultId,
                target,
                useValidateTarget,
                true,
                false,
                true,
                "direct_target_los_not_required",
                sourceVerb);
            return true;
        }

        /// <summary>
        /// 解析 dual 某一侧的正式结果与 formal host Verb。
        /// 这里只做 targeting 期纯读，不改动任何会话状态。
        /// </summary>
        private bool TryResolveDualWeaponSide(
            ResolvedTargetingContext context,
            string sourceResultId,
            out FormalExpressionResult sourceResult,
            out Verb sourceVerb)
        {
            sourceResult = null;
            sourceVerb = null;
            if (context?.Projection?.ResultIndex == null
                || string.IsNullOrWhiteSpace(sourceResultId)
                || !context.Projection.ResultIndex.TryGetValue(sourceResultId, out sourceResult)
                || sourceResult == null
                || !sourceResult.IsAvailable
                || !VerbHostSurfaceAccess.TryGetByResultId(pawn, sourceResultId, out BdpFormalVerbBinding binding))
            {
                return false;
            }

            sourceVerb = binding.ResolveActiveVerb();
            return sourceVerb != null && sourceVerb.Available();
        }

        /// <summary>
        /// 当 dual 两侧都不合法时，尽量转发一侧真实 Verb 的拒绝原因给玩家。
        /// 若两侧都拿不到正式原因，则回退到通用拒绝提示。
        /// </summary>
        private void ShowDualWeaponRejectReason(
            ResolvedTargetingContext context,
            CompositeExpressionReference reference,
            LocalTargetInfo target)
        {
            if (TryShowDualWeaponSideRejectReason(context, reference != null ? reference.MainSourceResultId : null, target))
            {
                return;
            }

            if (TryShowDualWeaponSideRejectReason(context, reference != null ? reference.SubSourceResultId : null, target))
            {
                return;
            }

            Messages.Message("CannotHitTarget".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
        }

        /// <summary>
        /// 尝试借用单侧真实 Verb 的验证路径输出拒绝原因。
        /// </summary>
        private bool TryShowDualWeaponSideRejectReason(
            ResolvedTargetingContext context,
            string sourceResultId,
            LocalTargetInfo target)
        {
            return TryResolveDualWeaponSide(context, sourceResultId, out FormalExpressionResult sourceResult, out Verb sourceVerb)
                && sourceVerb != null
                && (sourceResult?.WeaponMode == WeaponExpressionMode.Melee
                    ? target.HasThing && !sourceVerb.ValidateTarget(target, true)
                    : sourceResult?.ResolvedVerbSpec != null
                        && sourceResult.ResolvedVerbSpec.RequiresDirectTargetLineOfSight
                        && !sourceVerb.ValidateTarget(target, true));
        }

        /// <summary>
        /// 当确认阶段拒绝正式下单时，把交互会话恢复到继续可选状态。
        /// </summary>
        /// <param name="targetingRecord">刚刚完成这一轮输入驱动的目标记录。</param>
        private static void ReopenInteractionAfterConfirmRejected(TargetingRecord targetingRecord)
        {
            if (targetingRecord == null)
            {
                return;
            }

            targetingRecord.InputState.IsActive = true;
            targetingRecord.InputState.IsComplete = false;
            if (targetingRecord.InteractionSession != null)
            {
                targetingRecord.InteractionSession.StepIndex = targetingRecord.InputState.StepIndex;
                targetingRecord.InteractionSession.Activate();
            }
        }

        /// <summary>
        /// 把确认阶段已经成立的上下文节点冻结成正式执行请求快照。
        /// 这里先复制当前攻击上下文，再补入确认节点，避免回头污染交互中的共享运行态。
        /// </summary>
        private static AttackContextSnapshot BuildExecutionAttackContextSnapshot(ConfirmRecord confirmRecord)
        {
            AttackContext attackContext = confirmRecord != null && confirmRecord.AttackContext != null
                ? AttackContext.FromSnapshot(confirmRecord.AttackContext.ToSnapshot())
                : new AttackContext();

            attackContext.Set(AttackContextKeys.ConfirmedInput, BuildConfirmedInputSnapshot(confirmRecord));
            attackContext.Set(AttackContextKeys.ConfirmedInteraction, BuildConfirmedInteractionSnapshot(confirmRecord));
            attackContext.Set(AttackContextKeys.ConfirmedTarget, BuildConfirmedTargetSnapshot(confirmRecord));
            return attackContext.ToSnapshot();
        }

        /// <summary>
        /// 按确认阶段当前上下文状态生成输入冻结节点。
        /// 这里才把交互链运行态压成执行边界需要的冻结事实，不让 ConfirmRecord 自己外挂独立主干。
        /// </summary>
        private static ConfirmedInputSnapshot BuildConfirmedInputSnapshot(ConfirmRecord confirmRecord)
        {
            ConfirmedInputSnapshot snapshot = new ConfirmedInputSnapshot
            {
                StepIndex = confirmRecord != null ? confirmRecord.InputState.StepIndex : 0,
                IsComplete = confirmRecord != null && confirmRecord.InputState.IsComplete
            };

            if (confirmRecord?.InputState?.Tags != null)
            {
                for (int i = 0; i < confirmRecord.InputState.Tags.Count; i++)
                {
                    string tag = confirmRecord.InputState.Tags[i];
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        snapshot.Tags.Add(tag);
                    }
                }
            }

            return snapshot;
        }

        /// <summary>
        /// 按确认阶段当前上下文状态生成交互冻结节点。
        /// 这里统一读取上下文节点，而不是继续依赖独立确认快照主干。
        /// </summary>
        private static ConfirmedInteractionSnapshot BuildConfirmedInteractionSnapshot(ConfirmRecord confirmRecord)
        {
            ConfirmedInteractionSnapshot snapshot = new ConfirmedInteractionSnapshot
            {
                StepIndex = confirmRecord != null && confirmRecord.InteractionSession != null
                    ? confirmRecord.InteractionSession.StepIndex
                    : 0,
                IsComplete = confirmRecord != null
                    && confirmRecord.InteractionSession != null
                    && confirmRecord.InteractionSession.IsCompleted
            };

            if (confirmRecord?.InputState?.Tags != null)
            {
                for (int i = 0; i < confirmRecord.InputState.Tags.Count; i++)
                {
                    string tag = confirmRecord.InputState.Tags[i];
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        snapshot.Tags.Add(tag);
                    }
                }
            }

            return snapshot;
        }

        /// <summary>
        /// 按确认阶段当前裁定结果生成目标冻结节点。
        /// 这里显式拆出冻结后的 `NavigationTarget（导航目标）` 与 `SemanticTarget（语义目标）`，避免模块导航点污染其他攻击语义。
        /// </summary>
        private static ConfirmedTargetSnapshot BuildConfirmedTargetSnapshot(ConfirmRecord confirmRecord)
        {
            return new ConfirmedTargetSnapshot
            {
                NavigationTarget = confirmRecord != null ? confirmRecord.Target : LocalTargetInfo.Invalid,
                SemanticTarget = confirmRecord != null && confirmRecord.SemanticTarget.IsValid
                    ? confirmRecord.SemanticTarget
                    : confirmRecord != null ? confirmRecord.Target : LocalTargetInfo.Invalid
            };
        }

        /// <summary>
        /// 为当前输入帧建立默认推进裁决。
        /// 没有模块时仍应自然退化为原版单步确认。
        /// </summary>
        private static TargetingAdvanceDecision BuildDefaultAdvanceDecision(TargetingInputFrame inputFrame)
        {
            if (inputFrame != null && inputFrame.CancelRequested)
            {
                return new TargetingAdvanceDecision
                {
                    Kind = TargetingAdvanceKind.Cancel
                };
            }

            if (inputFrame != null && inputFrame.ConfirmRequested)
            {
                return new TargetingAdvanceDecision
                {
                    Kind = TargetingAdvanceKind.Complete
                };
            }

            return new TargetingAdvanceDecision
            {
                Kind = TargetingAdvanceKind.Continue
            };
        }

        /// <summary>
        /// 绘制当前预览阶段的正式扩展绘制项。
        /// 宿主只按图元绘制，不理解任何业务来源。
        /// </summary>
        private static void DrawPreviewItems(PreviewRecord record)
        {
            if (record?.DrawItems == null)
            {
                return;
            }

            for (int i = 0; i < record.DrawItems.Count; i++)
            {
                PreviewDrawItem item = record.DrawItems[i];
                if (item == null)
                {
                    continue;
                }

                switch (item.Kind)
                {
                    case PreviewDrawItemKind.Line:
                        if (item.Color == Color.red)
                        {
                            GenDraw.DrawLineBetween(item.Start, item.End, SimpleColor.Red);
                        }
                        else
                        {
                            GenDraw.DrawLineBetween(item.Start, item.End);
                        }

                        break;
                    case PreviewDrawItemKind.Ring:
                        GenDraw.DrawRadiusRing(item.Start.ToIntVec3(), item.Radius);
                        break;
                    case PreviewDrawItemKind.CellGroup:
                        if (item.Cells != null && item.Cells.Count > 0)
                        {
                            GenDraw.DrawFieldEdges(item.Cells, item.Color);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// 在 OnGUI 阶段绘制预览文字。
        /// 文字属于界面层，不允许混入世界高亮绘制链路。
        /// </summary>
        /// <param name="record">当前预览阶段已经生成的绘制记录。</param>
        private static void DrawPreviewLabels(PreviewRecord record)
        {
            if (record?.DrawItems == null || Event.current == null)
            {
                return;
            }

            Vector2 mousePosition = Event.current.mousePosition;
            for (int i = 0; i < record.DrawItems.Count; i++)
            {
                PreviewDrawItem item = record.DrawItems[i];
                if (item == null
                    || item.Kind != PreviewDrawItemKind.Label
                    || string.IsNullOrWhiteSpace(item.Label))
                {
                    continue;
                }

                Rect rect = new Rect(mousePosition.x + 20f, mousePosition.y + 20f, 220f, 30f);
                Widgets.Label(rect, item.Label);
            }
        }

        /// <summary>
        /// 当前 targeting 读取共享的最小解析上下文。
        /// 它只承载本次读取命中的结果、宿主和版本号，不承担额外缓存职责。
        /// </summary>
        private sealed class ResolvedTargetingContext
        {
            /// <summary>
            /// 当前读取命中的已发布战斗投影。
            /// </summary>
            public TriggerCombatProjectionState Projection { get; set; }

            /// <summary>
            /// 当前读取命中的投影版本号。
            /// </summary>
            public int ProjectionVersion { get; set; }

            /// <summary>
            /// 当前命中的正式结果。
            /// </summary>
            public FormalExpressionResult Result { get; set; }

            /// <summary>
            /// 当前结果解析出的真实 Verb。
            /// </summary>
            public Verb Verb { get; set; }

            /// <summary>
            /// 当前结果绑定的模块运行时会话。
            /// </summary>
            public RangedAttackModuleSession ModuleSession { get; set; }

            /// <summary>
            /// 当前这轮 targeting 共享的统一攻击上下文。
            /// </summary>
            public AttackContext AttackContext { get; set; }

            /// <summary>
            /// 当前 targeting 阶段的已裁定记录。
            /// </summary>
            public TargetingRecord TargetingRecord { get; set; }
        }
    }
}
