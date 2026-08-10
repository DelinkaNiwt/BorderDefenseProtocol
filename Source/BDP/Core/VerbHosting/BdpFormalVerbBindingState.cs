using BDP.Core.Expressions;
using BDP.Core.AttackExecution;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.VerbHosting
{
    /// <summary>
    /// 一条正式宿主槽位当前绑定的最小状态。
    /// 它只表达宿主层需要知道的“当前这条壳 verb 该承接谁”，不复制 Trigger/Expression 真值。
    /// </summary>
    internal sealed class BdpFormalVerbBindingState
    {
        /// <summary>
        /// 当前正式宿主槽位身份。
        /// </summary>
        public BdpFormalVerbHostSlot Slot { get; set; }

        /// <summary>
        /// 当前槽位绑定的正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前槽位对应已发布结果的基础会话令牌。
        /// 它只描述“当前绑定命中的正式结果与发布版本”，不默认代表活跃攻击实例。
        /// </summary>
        public AttackSessionToken SessionToken { get; set; }

        /// <summary>
        /// 当前槽位是否可用。
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 当前槽位绑定的武器模式。
        /// 它只用于决定取哪一条正式壳 verb。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 当前槽位绑定的正式 Verb 属性。
        /// 它是表达结果对原版战斗会话表面的声明，不是新的业务真值。
        /// </summary>
        public VerbProperties VerbProps { get; set; }

        /// <summary>
        /// 当前槽位绑定的近战 Tool。
        /// </summary>
        public Tool Tool { get; set; }

        /// <summary>
        /// 当前槽位保留的全部近战 Tool。
        /// </summary>
        public IReadOnlyList<Tool> DeclaredTools { get; set; }

        /// <summary>
        /// 当前槽位可供 step 选择的全部近战运行时表面。
        /// </summary>
        public IReadOnlyList<MeleeToolSurface> DeclaredMeleeToolSurfaces { get; set; }

        /// <summary>
        /// 当前槽位绑定的 Maneuver。
        /// </summary>
        public ManeuverDef Maneuver { get; set; }
    }
}
