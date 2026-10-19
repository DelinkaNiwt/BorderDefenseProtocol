using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>人物死亡前冻结的一项自身特性，不包含黑触发器临时继承特性。</summary>
    public sealed class BlackTriggerTraitSnapshot : IExposable
    {
        /// <summary>来源人物实际拥有的特性定义。</summary>
        public TraitDef TraitDef;

        /// <summary>来源人物实际拥有的特性 degree。</summary>
        public int Degree;

        /// <summary>死亡前冻结的玩家可读名称。</summary>
        public string Label;

        /// <summary>死亡前是否被其它特性或基因压制。</summary>
        public bool WasSuppressed;

        /// <summary>特性定义与 degree 组成的稳定键。</summary>
        public string StableKey
        {
            get { return TraitDef == null ? string.Empty : TraitDef.defName + ":" + Degree; }
        }

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public void ExposeData()
        {
            Scribe_Defs.Look(ref TraitDef, "traitDef");
            Scribe_Values.Look(ref Degree, "degree");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref WasSuppressed, "wasSuppressed");
        }
    }
}
