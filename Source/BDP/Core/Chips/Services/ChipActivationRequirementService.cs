using System.Collections.Generic;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片激活条件的业务适配器。
    /// 它只解析芯片条件来源，通用描述与求值统一交给中性角色条件底层。
    /// </summary>
    public sealed class ChipActivationRequirementService
    {
        /// <summary>共享无状态服务实例。</summary>
        public static readonly ChipActivationRequirementService Instance =
            new ChipActivationRequirementService();

        /// <summary>禁止外部创建重复服务。</summary>
        private ChipActivationRequirementService()
        {
        }

        /// <summary>
        /// 按 XML 顺序读取一枚芯片的全部静态条件说明。
        /// </summary>
        public IReadOnlyList<PawnRequirementSnapshot> Describe(Thing chip)
        {
            return PawnRequirementEvaluator.Instance.Describe(ResolveRequirements(chip));
        }

        /// <summary>
        /// 按 XML 顺序检查全部条件，并一次收集所有失败项。
        /// </summary>
        public PawnRequirementCheckResult Evaluate(Pawn pawn, Thing chip)
        {
            return PawnRequirementEvaluator.Instance.Evaluate(pawn, ResolveRequirements(chip));
        }

        /// <summary>读取芯片定义中的条件集合，并浅复制公开边界。</summary>
        private static IReadOnlyList<PawnRequirement> ResolveRequirements(Thing chip)
        {
            // 所有芯片统一从制造期 Comp 读取配置（不再有 Def 静态配置路径）。
            ChipDefinitionConfig config;
            ChipInstanceSurfaceAccess.TryGetDefinition(chip, out config);
            return config?.ActivationRequirements != null
                ? new List<PawnRequirement>(config.ActivationRequirements).AsReadOnly()
                : new List<PawnRequirement>().AsReadOnly();
        }
    }
}
