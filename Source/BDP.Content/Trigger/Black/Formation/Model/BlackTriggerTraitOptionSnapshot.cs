using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>形成决策时可供玩家选择的一项冻结特性。</summary>
    public sealed class BlackTriggerTraitOptionSnapshot : IExposable
    {
        /// <summary>原人物实际拥有的特性 Def。</summary>
        public TraitDef TraitDef;

        /// <summary>原人物实际拥有的特性 degree。</summary>
        public int Degree;

        /// <summary>死亡前冻结的玩家可读名称。</summary>
        public string Label;

        /// <summary>死亡前是否被其它特性或基因压制。</summary>
        public bool WasSuppressed;

        /// <summary>特性选项稳定键。</summary>
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
