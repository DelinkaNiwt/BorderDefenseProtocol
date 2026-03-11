using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 组合效果定义基类——统一芯片组合匹配和效果管理。
    ///
    /// 设计原则：
    ///   · 基类负责芯片匹配（chipA + chipB）
    ///   · 子类负责效果激活/撤销的具体实现
    ///   · 支持一对芯片产生多种效果（Verb + Ability + Hediff）
    ///
    /// 继承链：
    ///   ComboEffectDef (抽象基类)
    ///   ├─ ComboVerbDef (攻击 Verb)
    ///   ├─ ComboAbilityDef (技能)
    ///   └─ ComboHediffDef (被动效果)
    /// </summary>
    public abstract class ComboEffectDef : Def
    {
        /// <summary>组合所需的芯片A的ThingDef。</summary>
        public ThingDef chipA;

        /// <summary>组合所需的芯片B的ThingDef。</summary>
        public ThingDef chipB;

        /// <summary>
        /// 检查两个芯片ThingDef是否匹配此组合定义（对称匹配）。
        /// </summary>
        public bool Matches(ThingDef a, ThingDef b)
        {
            if (chipA == null || chipB == null) return false;
            return (a == chipA && b == chipB) || (a == chipB && b == chipA);
        }

        /// <summary>
        /// 激活组合效果（抽象方法，由子类实现）。
        /// 在芯片激活后、匹配成功时调用。
        /// </summary>
        /// <param name="pawn">装备者</param>
        /// <param name="triggerComp">触发体 Comp</param>
        /// <param name="leftChip">左手芯片</param>
        /// <param name="rightChip">右手芯片</param>
        public abstract void ActivateEffect(Pawn pawn, CompTriggerBody triggerComp,
            Thing leftChip, Thing rightChip);

        /// <summary>
        /// 撤销组合效果（抽象方法，由子类实现）。
        /// 在芯片关闭后、不再匹配时调用。
        /// </summary>
        public abstract void DeactivateEffect(Pawn pawn, CompTriggerBody triggerComp);

        /// <summary>
        /// 获取效果类型标识（用于日志和调试）。
        /// </summary>
        public abstract ComboEffectType EffectType { get; }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var e in base.ConfigErrors())
                yield return e;
            if (chipA == null) yield return "chipA is null";
            if (chipB == null) yield return "chipB is null";
        }
    }
}
