using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>由当前装备黑触发器临时赋予、仍按原版 Trait 运行的特性实例。</summary>
    public sealed class Trait_BlackTriggerInherited : Trait, IExposable
    {
        /// <summary>提供该临时特性的黑触发器 ThingID。</summary>
        public string SourceThingId;

        /// <summary>添加临时特性前已经存在、移除后必须恢复的同名能力。</summary>
        public List<AbilityDef> PreexistingAbilities = new List<AbilityDef>();

        /// <summary>供存档系统反射构造。</summary>
        public Trait_BlackTriggerInherited() { }

        /// <summary>用原 TraitDef 和 degree 创建临时继承实例。</summary>
        public Trait_BlackTriggerInherited(TraitDef def, int degree, string sourceThingId)
            : base(def, degree)
        {
            SourceThingId = sourceThingId;
        }

        /// <summary>保存原版特性数据与来源物品标识。</summary>
        public new void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SourceThingId, "blackTriggerSourceThingId");
            Scribe_Collections.Look(ref PreexistingAbilities, "blackTriggerPreexistingAbilities", LookMode.Def);
            if (PreexistingAbilities == null) PreexistingAbilities = new List<AbilityDef>();
        }
    }
}
