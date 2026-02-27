using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 组合能力定义（v4.0 F1新增）——数据结构，定义两个芯片组合后产生的特殊Ability。
    ///
    /// 使用方式：
    ///   CompTriggerBody在芯片激活/关闭时查询DefDatabase&lt;ComboAbilityDef&gt;，
    ///   匹配成功则授予/移除对应Ability。Ability自带Gizmo（原版机制）。
    ///
    /// 匹配规则：
    ///   chipA和chipB的顺序无关（对称匹配）。
    ///   同一对芯片只能匹配一个ComboAbilityDef。
    ///
    /// 当前阶段：仅数据结构，不实现具体组合逻辑。
    /// </summary>
    public class ComboAbilityDef : Def
    {
        /// <summary>组合所需的芯片A的ThingDef。</summary>
        public ThingDef chipA;

        /// <summary>组合所需的芯片B的ThingDef。</summary>
        public ThingDef chipB;

        /// <summary>组合成功后授予的AbilityDef。</summary>
        public AbilityDef abilityDef;

        /// <summary>
        /// 检查两个芯片ThingDef是否匹配此组合定义（对称匹配）。
        /// </summary>
        public bool Matches(ThingDef a, ThingDef b)
        {
            if (chipA == null || chipB == null) return false;
            return (a == chipA && b == chipB) || (a == chipB && b == chipA);
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors())
                yield return e;
            if (chipA == null) yield return "chipA is null";
            if (chipB == null) yield return "chipB is null";
            if (abilityDef == null) yield return "abilityDef is null";
        }
    }
}
