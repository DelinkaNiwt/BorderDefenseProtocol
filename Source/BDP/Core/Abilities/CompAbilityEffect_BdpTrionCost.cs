using BDP.Core.Expressions;
using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// Ability 的 Trion 施法成本组件。
    /// 它负责三件事：
    /// - 按钮层禁用与原因提示
    /// - 目标校验层失败提示
    /// - 提供“正式提交施法成本”的统一入口给自定义 Ability Verb 调用
    /// </summary>
    public sealed class CompAbilityEffect_BdpTrionCost : CompAbilityEffect
    {
        /// <summary>
        /// Trion 不足时的固定提示。
        /// 用户已明确要求不用放到 XML 配。
        /// </summary>
        private static string InsufficientTrionMessage
        {
            get { return "BDP_Message_Ability_TrionInsufficient".Translate(); }
        }

        /// <summary>
        /// 当前 Pawn 没有 Trion 宿主时的固定提示。
        /// </summary>
        private static string MissingTrionHostMessage
        {
            get { return "BDP_Message_Ability_MissingTrionHost".Translate(); }
        }

        /// <summary>
        /// 当前表达结果声明的正式施法成本。
        /// AbilityDef 只作为原版宿主壳，BDP 成本必须来自表达系统绑定结果。
        /// public：供 Content 层（如蚱蜢链式跳跃）动态读取每次使用成本。
        /// </summary>
        public float TrionCost
        {
            get { return ResolveExpressionUseCost(); }
        }

        /// <summary>
        /// 当前表达结果声明的最低 Trion 要求。
        /// 它只参与准入检查，不参与正式扣费。
        /// </summary>
        internal float MinimumRequired
        {
            get { return ResolveExpressionMinimumRequired(); }
        }

        /// <summary>
        /// 当前施法前必须满足的可用 Trion 门槛。
        /// </summary>
        private float RequiredAvailable
        {
            get { return System.Math.Max(TrionCost, MinimumRequired); }
        }

        /// <summary>
        /// 当前施法是否需要 Trion 宿主参与。
        /// </summary>
        public override bool CanCast
        {
            get
            {
                float requiredAvailable = RequiredAvailable;
                if (requiredAvailable <= 0f)
                {
                    return true;
                }

                ITrionCommands commands = ResolveCommands();
                return commands != null && commands.CanAfford(requiredAvailable);
            }
        }

        /// <summary>
        /// 让能力按钮在资源不足时直接灰掉，并给出原因。
        /// </summary>
        public override bool GizmoDisabled(out string reason)
        {
            float requiredAvailable = RequiredAvailable;
            if (requiredAvailable <= 0f)
            {
                reason = null;
                return false;
            }

            ITrionCommands commands = ResolveCommands();
            if (commands == null)
            {
                reason = MissingTrionHostMessage;
                return true;
            }

            if (!commands.CanAfford(requiredAvailable))
            {
                reason = InsufficientTrionMessage;
                return true;
            }

            reason = null;
            return false;
        }

        /// <summary>
        /// 目标校验阶段也要守住资源门槛。
        /// 这样即使绕开按钮态，也会在正式施法前被拦住并提示。
        /// </summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            float requiredAvailable = RequiredAvailable;
            if (requiredAvailable <= 0f)
            {
                return true;
            }

            ITrionCommands commands = ResolveCommands();
            if (commands == null)
            {
                if (throwMessages)
                {
                    ShowRejectMessage(MissingTrionHostMessage);
                }

                return false;
            }

            if (!commands.CanAfford(requiredAvailable))
            {
                if (throwMessages)
                {
                    ShowRejectMessage(InsufficientTrionMessage);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 让 AI 也遵守同一套 Trion 准入规则。
        /// </summary>
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return CanCast && base.AICanTargetNow(target);
        }

        /// <summary>
        /// 在 Tooltip 里补一句固定格式的成本说明。
        /// </summary>
        public override string ExtraTooltipPart()
        {
            float cost = TrionCost;
            float minimumRequired = MinimumRequired;
            if (cost <= 0f && minimumRequired <= 0f)
            {
                return null;
            }

            if (minimumRequired > cost)
            {
                return "BDP_Ability_TrionCostWithMinimum".Translate(
                    cost.ToString("0.##"),
                    minimumRequired.ToString("0.##"));
            }

            return "BDP_Ability_TrionCost".Translate(cost.ToString("0.##"));
        }

        /// <summary>
        /// 给自定义 Ability Verb 调用的正式扣费入口。
        /// 它必须发生在真正触发 Ability 效果之前。
        /// </summary>
        internal bool TryCommitCastCost()
        {
            float cost = TrionCost;
            float requiredAvailable = RequiredAvailable;
            if (requiredAvailable <= 0f)
            {
                return true;
            }

            ITrionCommands commands = ResolveCommands();
            if (commands == null)
            {
                ShowRejectMessage(MissingTrionHostMessage);
                return false;
            }

            if (!commands.CanAfford(requiredAvailable))
            {
                ShowRejectMessage(InsufficientTrionMessage);
                return false;
            }

            if (cost <= 0f)
            {
                return true;
            }

            if (!commands.TryConsume(cost))
            {
                ShowRejectMessage(InsufficientTrionMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 解析当前 Pawn 的正式 Trion 命令面。
        /// </summary>
        private ITrionCommands ResolveCommands()
        {
            return parent != null ? TrionSurfaceAccess.ResolveCommands(parent.pawn) : null;
        }

        /// <summary>
        /// 读取当前 Ability 宿主绑定的正式表达结果。
        /// </summary>
        private FormalExpressionResult ResolveBoundExpressionResult()
        {
            FormalExpressionResult result;
            return DefaultExpressionAbilityHostSynchronizer.TryResolveBoundAbilityResult(
                    parent != null ? parent.pawn : null,
                    parent != null ? parent.def : null,
                    out result)
                ? result
                : null;
        }

        /// <summary>
        /// 从表达结果读取每次使用成本。
        /// </summary>
        private float ResolveExpressionUseCost()
        {
            FormalExpressionResult result = ResolveBoundExpressionResult();
            return result?.Trion != null ? System.Math.Max(0f, result.Trion.UseCost) : 0f;
        }

        /// <summary>
        /// 从表达结果读取最低 Trion 要求。
        /// </summary>
        private float ResolveExpressionMinimumRequired()
        {
            FormalExpressionResult result = ResolveBoundExpressionResult();
            return result?.Trion != null ? System.Math.Max(0f, result.Trion.MinimumRequired) : 0f;
        }

        /// <summary>
        /// 用原版拒绝消息样式给玩家提示。
        /// </summary>
        private void ShowRejectMessage(string message)
        {
            if (parent?.pawn == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Messages.Message(message, parent.pawn, MessageTypeDefOf.RejectInput, false);
        }
    }
}
