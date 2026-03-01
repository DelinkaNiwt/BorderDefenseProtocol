using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 组合技攻击定义（v10.0新增）——数据结构，定义两个芯片组合后产生的特殊攻击Verb。
    ///
    /// 与ComboAbilityDef的区别：
    ///   · ComboAbilityDef → 授予原版Ability（技能按钮）
    ///   · ComboVerbDef → 生成第4个攻击Gizmo（攻击模式），发射专属弹药
    ///
    /// 匹配规则：chipA和chipB的顺序无关（对称匹配），同ComboAbilityDef。
    /// </summary>
    public class ComboVerbDef : Def
    {
        /// <summary>组合所需的芯片A的ThingDef。</summary>
        public ThingDef chipA;

        /// <summary>组合所需的芯片B的ThingDef。</summary>
        public ThingDef chipB;

        /// <summary>组合技发射的弹药ThingDef（如BDP_Bullet_Argus）。</summary>
        public ThingDef projectileDef;

        /// <summary>是否支持齐射模式（右键触发）。</summary>
        public bool supportsVolley = true;

        /// <summary>是否支持引导瞄准（锚点折线弹道）。</summary>
        public bool supportsGuided = true;

        /// <summary>最大锚点数（不含最终目标）。仅supportsGuided=true时有效。</summary>
        public int maxAnchors = 3;

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
            if (projectileDef == null) yield return "projectileDef is null";
        }
    }
}
