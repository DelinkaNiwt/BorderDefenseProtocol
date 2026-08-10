using System.Collections.Generic;
using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口 Trion 流失提示组件注入器。
    /// 它只在 Def 阶段给原版伤口类追加显示组件，避免新增健康面板 UI 补丁。
    /// </summary>
    internal static class CombatBodyWoundTrionInfoInjector
    {
        /// <summary>
        /// 给当前加载的伤口 Def 追加 BDP 伤口提示组件。
        /// 第一版只接入 Hediff_Injury，缺失部位是否显示流失以后再单独决定。
        /// </summary>
        internal static void Apply()
        {
            List<HediffDef> defs = DefDatabase<HediffDef>.AllDefsListForReading;
            for (int index = 0; index < defs.Count; index++)
            {
                HediffDef def = defs[index];
                if (!ShouldInject(def) || HasInfoComp(def))
                {
                    continue;
                }

                if (def.comps == null)
                {
                    def.comps = new List<HediffCompProperties>();
                }

                def.comps.Add(new CombatBodyWoundTrionInfoHediffCompProperties());
            }
        }

        /// <summary>
        /// 判断当前 Def 是否属于第一版要接入提示的伤口类型。
        /// </summary>
        private static bool ShouldInject(HediffDef def)
        {
            return def?.hediffClass != null && typeof(Hediff_Injury).IsAssignableFrom(def.hediffClass);
        }

        /// <summary>
        /// 判断当前 Def 是否已经拥有 BDP 伤口 Trion 提示组件。
        /// </summary>
        private static bool HasInfoComp(HediffDef def)
        {
            if (def?.comps == null)
            {
                return false;
            }

            for (int index = 0; index < def.comps.Count; index++)
            {
                HediffCompProperties props = def.comps[index];
                if (props is CombatBodyWoundTrionInfoHediffCompProperties ||
                    props?.compClass == typeof(CombatBodyWoundTrionInfoHediffComp))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
