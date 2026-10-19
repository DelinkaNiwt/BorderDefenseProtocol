using System.Collections.Generic;
using BDP.Core.Expressions.Runtime;
using BDP.Core.Information;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using BDP.Core.VerbHosting;
using RimWorld;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达系统正式入口解析面。
    /// 主模组内其它系统应统一从这里拿表达系统正式读取口或内部服务口，而不是自己组装表达运行时依赖。
    /// </summary>
    public static class ExpressionSurfaceAccess
    {
        /// <summary>业务来源条件发生离散变化时，复用唯一发布者重新求值，不携带业务判断。</summary>
        public static void NotifySourceConditionsChanged(Pawn pawn)
        {
            TriggerSurfaceAccess.ResolveComp(pawn)?.NotifySourceConditionsChanged();
        }

        /// <summary>
        /// 读取当前 Pawn 已发布的公开表达投影快照。
        /// 这条口只读当前正式发布状态，不触发表达重建。
        /// </summary>
        public static ExpressionPublishedProjectionSnapshot ResolvePublishedProjection(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            TriggerCombatProjectionState projection = triggerBody != null
                ? triggerBody.PublishedCombatProjection
                : null;
            if (projection == null || projection.IsEmpty)
            {
                return ExpressionPublishedProjectionSnapshot.Empty();
            }

            TriggerPresentationState presentation = triggerBody.PublishedPresentationProjection;
            IReadOnlyDictionary<string, ChipFeatureResolutionResult> comboFeatureInformations =
                presentation != null
                    && presentation.ProjectionVersion == projection.ProjectionVersion
                    ? presentation.ComboFeatureInformations
                    : null;
            return ExpressionPublishedSnapshotBuilder.Build(
                projection,
                comboFeatureInformations);
        }

        /// <summary>
        /// 读取当前 Pawn 已发布的统一芯片信息模型。
        /// </summary>
        public static ChipInformationModel ResolveChipInformation(Pawn pawn)
        {
            ExpressionService service = ResolveService(pawn);
            return service != null
                ? service.GetChipInformation(pawn)
                : new ChipInformationModel(
                    new ChipInformationNode(null, "chip", null, null, 0, null, null),
                    null);
        }

        /// <summary>读取当前 Pawn 已发布的全部激活芯片信息模型。</summary>
        public static IReadOnlyList<ChipInformationModel> ResolveChipInformations(Pawn pawn)
        {
            ExpressionService service = ResolveService(pawn);
            return service != null
                ? service.GetChipInformations(pawn)
                : new List<ChipInformationModel>();
        }

        /// <summary>
        /// 按结果标识读取一条已发布的公开表达结果。
        /// </summary>
        public static bool TryGetPublishedResult(
            Pawn pawn,
            string resultId,
            out ExpressionPublishedResultSnapshot result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(resultId))
            {
                return false;
            }

            return ResolvePublishedProjection(pawn).TryGetResult(resultId, out result);
        }

        /// <summary>
        /// 按结果标识解析当前 live Verb 宿主。
        /// 这条口只适用于 Verb 通道。
        /// </summary>
        public static bool TryResolveVerbHost(Pawn pawn, string resultId, out Verb verb)
        {
            verb = null;
            if (pawn == null
                || string.IsNullOrWhiteSpace(resultId)
                || !VerbHostSurfaceAccess.TryGetByResultId(pawn, resultId, out BdpFormalVerbBinding binding))
            {
                return false;
            }

            verb = binding.ResolveActiveVerb();
            return verb != null && verb.Available();
        }

        /// <summary>
        /// 按公开结果快照解析当前 live Verb 宿主。
        /// </summary>
        public static bool TryResolveVerbHost(
            Pawn pawn,
            ExpressionPublishedResultSnapshot result,
            out Verb verb)
        {
            verb = null;
            return result != null
                && result.ChannelKind == ExpressionPublishedChannelKind.Verb
                && TryResolveVerbHost(pawn, result.ResultId, out verb);
        }

        /// <summary>
        /// 按 AbilityDef 名称解析当前 live Ability 宿主。
        /// 这条口只适用于 Ability 通道。
        /// </summary>
        public static bool TryResolveAbilityHost(
            Pawn pawn,
            string abilityDefName,
            out Ability ability)
        {
            ability = null;
            if (pawn?.abilities == null || string.IsNullOrWhiteSpace(abilityDefName))
            {
                return false;
            }

            AbilityDef abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
            if (abilityDef == null)
            {
                return false;
            }

            ability = pawn.abilities.GetAbility(abilityDef, true);
            return ability != null
                && DefaultExpressionAbilityHostSynchronizer.TryResolveBoundAbilityResult(
                    pawn,
                    abilityDef,
                    out FormalExpressionResult _);
        }

        /// <summary>
        /// 按公开结果快照解析当前 live Ability 宿主。
        /// </summary>
        public static bool TryResolveAbilityHost(
            Pawn pawn,
            ExpressionPublishedResultSnapshot result,
            out Ability ability)
        {
            ability = null;
            return result != null
                && result.ChannelKind == ExpressionPublishedChannelKind.Ability
                && TryResolveAbilityHost(pawn, result.AbilityDefName, out ability);
        }

        /// <summary>
        /// 按 HediffDef 名称解析当前 live Hediff 宿主。
        /// 这条口只适用于 Hediff 通道。
        /// </summary>
        public static bool TryResolveHediffHost(
            Pawn pawn,
            string hediffDefName,
            out Hediff hediff)
        {
            hediff = null;
            if (pawn?.health?.hediffSet == null || string.IsNullOrWhiteSpace(hediffDefName))
            {
                return false;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (hediffDef == null)
            {
                return false;
            }

            hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef, false);
            BdpExpressionHostHediff hostHediff = hediff as BdpExpressionHostHediff;
            return hostHediff != null
                && hostHediff.ExpressionResults != null
                && hostHediff.ExpressionResults.Count > 0;
        }

        /// <summary>
        /// 按公开结果快照解析当前 live Hediff 宿主。
        /// </summary>
        public static bool TryResolveHediffHost(
            Pawn pawn,
            ExpressionPublishedResultSnapshot result,
            out Hediff hediff)
        {
            hediff = null;
            return result != null
                && result.ChannelKind == ExpressionPublishedChannelKind.Hediff
                && TryResolveHediffHost(pawn, result.HediffDefName, out hediff);
        }

        /// <summary>
        /// 读取表达系统正式只读口。
        /// 这条口只对内开放给主模组内部调用，不作为对外公开协议。
        /// </summary>
        internal static IExpressionReader ResolveReader(Pawn pawn)
        {
            return ResolveService(pawn);
        }

        /// <summary>
        /// 读取表达系统内部正式服务口。
        /// 这条口只对内开放给需要访问内部辅助步骤的模块，不作为通用对外读取协议。
        /// </summary>
        internal static ExpressionService ResolveService(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            return triggerBody != null ? triggerBody.RuntimeServices?.ExpressionService : null;
        }

        /// <summary>
        /// 在实际表达宿主 Hediff 发生离散变化后，立即复用当前已发布快照完成宿主与持续消耗对账。
        /// </summary>
        internal static void SynchronizePublishedExpressionHostsImmediately(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            triggerBody?.RuntimeCoordinator?.SynchronizePublishedExpressionHostsImmediately();
        }

        /// <summary>
        /// 读取表达系统共享运行时仓库。
        /// </summary>
        internal static ExpressionRuntimeRepository ResolveRuntimeRepository(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            return triggerBody != null ? triggerBody.RuntimeServices?.ExpressionRuntimeRepository : null;
        }

    }
}
