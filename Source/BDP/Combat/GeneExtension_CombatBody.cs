using System.Collections.Generic;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// GeneDef 的 ModExtension，用于配置战斗体装备。
    /// </summary>
    public class GeneExtension_CombatBody : DefModExtension
    {
        /// <summary>
        /// 默认战斗体装备列表（ThingDef defName）。
        /// 在基因添加时自动生成并存入 CombatApparelContainer。
        /// </summary>
        public List<ThingDef> defaultCombatApparel;

        /// <summary>
        /// 战斗体维持消耗（每天）。默认0。
        /// 此消耗独立于芯片消耗，由战斗体系统注册到CompTrion。
        /// </summary>
        public float maintenanceDrainPerDay = 0f;
    }
}
