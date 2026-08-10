namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达作者对“射手到语义目标是否必须直射”的声明。
    /// 它只服务 dual 分侧准入这类入口裁定，不等同于攻击内部是否会使用 LOS。
    /// </summary>
    public enum DirectTargetLineOfSightRequirementConfig
    {
        /// <summary>
        /// 沿用 VerbProps.requireLineOfSight 作为普通直射 Verb 的默认语义。
        /// </summary>
        FromVerb,

        /// <summary>
        /// 明确要求射手到语义目标必须直射。
        /// </summary>
        Required,

        /// <summary>
        /// 明确不要求射手到语义目标必须直射。
        /// 攻击内部的路径段 LOS 等约束仍由各自模块自行裁定。
        /// </summary>
        NotRequired
    }
}
