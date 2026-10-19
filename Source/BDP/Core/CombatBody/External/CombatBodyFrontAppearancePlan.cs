using System.Collections.Generic;

namespace BDP.Core.CombatBody.External
{
    /// <summary>
    /// 一次战斗体前台外观解析结果。
    /// </summary>
    public sealed class CombatBodyFrontAppearancePlan
    {
        /// <summary>
        /// 当前个人外观计划的前台模式。
        /// </summary>
        public CombatBodyFrontMode Mode = CombatBodyFrontMode.MirrorOriginal;

        /// <summary>
        /// 当前预设模式需要创建的服装条目；空集合代表合法空模板。
        /// </summary>
        public List<CombatBodyFrontAppearanceEntry> Entries =
            new List<CombatBodyFrontAppearanceEntry>();

        /// <summary>
        /// 当前结果是否来自动态个人数据，而不是全局 Def 配置。
        /// </summary>
        public bool IsDynamic;

        /// <summary>
        /// 当前结果是否通过完整有效性校验。
        /// </summary>
        public bool IsValid = true;
    }
}
