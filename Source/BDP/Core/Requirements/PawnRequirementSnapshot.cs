namespace BDP.Core.Requirements
{
    /// <summary>
    /// 单条角色条件的公开只读快照。
    /// 外部程序集只能读取结果，不需要复制条件判断。
    /// </summary>
    public sealed class PawnRequirementSnapshot
    {
        /// <summary>玩家可读的条件名称。</summary>
        public string Label { get; private set; }

        /// <summary>绑定角色后读取到的当前值；静态说明时为空。</summary>
        public string CurrentValueText { get; private set; }

        /// <summary>定义声明的最低要求。</summary>
        public string RequiredValueText { get; private set; }

        /// <summary>有运行时求值时，该条件是否满足。</summary>
        public bool IsSatisfied { get; private set; }

        /// <summary>条件不满足时可直接向玩家展示的完整原因。</summary>
        public string FailureReason { get; private set; }

        /// <summary>创建一条不绑定角色的静态条件说明。</summary>
        public static PawnRequirementSnapshot Description(string label, string requirementText)
        {
            return new PawnRequirementSnapshot
            {
                Label = label,
                CurrentValueText = null,
                RequiredValueText = requirementText,
                IsSatisfied = true,
                FailureReason = null
            };
        }

        /// <summary>创建一条已绑定角色的条件检查结果。</summary>
        public static PawnRequirementSnapshot Evaluation(
            string label,
            string currentValueText,
            string requiredValueText,
            bool isSatisfied,
            string failureReason)
        {
            return new PawnRequirementSnapshot
            {
                Label = label,
                CurrentValueText = currentValueText,
                RequiredValueText = requiredValueText,
                IsSatisfied = isSatisfied,
                FailureReason = failureReason
            };
        }
    }
}
