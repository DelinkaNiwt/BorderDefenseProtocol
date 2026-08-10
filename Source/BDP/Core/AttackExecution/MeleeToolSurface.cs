using RimWorld;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 一把声明近战 Tool 在 BDP 运行时展开出的最小攻击表面。
    /// 它只承载“这一刀要用哪组原版近战参数”，不额外引入新的战斗真值。
    /// </summary>
    public sealed class MeleeToolSurface
    {
        /// <summary>
        /// 当前表面对应的作者声明 Tool。
        /// </summary>
        public Tool Tool { get; set; }

        /// <summary>
        /// 当前表面对应的近战 Verb 属性。
        /// </summary>
        public VerbProperties VerbProps { get; set; }

        /// <summary>
        /// 当前表面对应的近战招式声明。
        /// </summary>
        public ManeuverDef Maneuver { get; set; }

        /// <summary>
        /// 当前表面解析出的主伤害类型。
        /// </summary>
        public DamageDef DamageDef { get; set; }

        /// <summary>
        /// 当前表面在声明工具列表里的稳定顺序索引。
        /// 它用于 step 选择、日志和后续存档恢复。
        /// </summary>
        public int DeclaredIndex { get; set; }
    }
}
