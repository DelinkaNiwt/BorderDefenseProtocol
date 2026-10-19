using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>人物死亡前一次性冻结的形成资料。</summary>
    public sealed class BlackTriggerMakerSnapshot : IExposable
    {
        /// <summary>形成前的运行时人物引用。</summary>
        public Pawn Maker;

        /// <summary>来源人物稳定存档标识。</summary>
        public string MakerLoadId;

        /// <summary>来源人物显示名快照。</summary>
        public string MakerLabel;

        /// <summary>死亡前是否为玩家殖民者。</summary>
        public bool MakerWasColonistPlayerControlled;

        /// <summary>形成来源。</summary>
        public BlackTriggerFormationSource Source;

        /// <summary>死亡原因 DefName，仅供 Development 诊断。</summary>
        public string DamageDefName;

        /// <summary>致死病症 DefName，仅供 Development 诊断。</summary>
        public string CulpritHediffDefName;

        /// <summary>死亡前所在地图名称。</summary>
        public string MapLabel;

        /// <summary>前三项技能与全部有效前三名记录事实。</summary>
        public List<BlackTriggerFactSnapshot> Facts = new List<BlackTriggerFactSnapshot>();

        /// <summary>死亡前冻结的全部自身特性；压制项保留状态但不计入适应权重。</summary>
        public List<BlackTriggerTraitSnapshot> SourceTraits = new List<BlackTriggerTraitSnapshot>();

        /// <summary>死亡前冻结的全部实际能力，用于可选能力适应权重。</summary>
        public List<BlackTriggerAbilitySnapshot> SourceAbilities = new List<BlackTriggerAbilitySnapshot>();

        /// <summary>形成时可选择的传承人。</summary>
        public List<BlackTriggerHeirCandidateSnapshot> HeirCandidates = new List<BlackTriggerHeirCandidateSnapshot>();

        /// <summary>稳定随机抽出的最多三项继承特性选项。</summary>
        public List<BlackTriggerTraitOptionSnapshot> TraitOptions = new List<BlackTriggerTraitOptionSnapshot>();

        /// <summary>形成事件使用的稳定随机种子。</summary>
        public int StableSeed;

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public void ExposeData()
        {
            Scribe_References.Look(ref Maker, "maker", saveDestroyedThings: true);
            Scribe_Values.Look(ref MakerLoadId, "makerLoadId");
            Scribe_Values.Look(ref MakerLabel, "makerLabel");
            Scribe_Values.Look(ref MakerWasColonistPlayerControlled, "makerWasColonistPlayerControlled");
            Scribe_Values.Look(ref Source, "source");
            Scribe_Values.Look(ref DamageDefName, "damageDefName");
            Scribe_Values.Look(ref CulpritHediffDefName, "culpritHediffDefName");
            Scribe_Values.Look(ref MapLabel, "mapLabel");
            Scribe_Collections.Look(ref Facts, "facts", LookMode.Deep);
            Scribe_Collections.Look(ref SourceTraits, "sourceTraits", LookMode.Deep);
            Scribe_Collections.Look(ref SourceAbilities, "sourceAbilities", LookMode.Deep);
            Scribe_Collections.Look(ref HeirCandidates, "heirCandidates", LookMode.Deep);
            Scribe_Collections.Look(ref TraitOptions, "traitOptions", LookMode.Deep);
            Scribe_Values.Look(ref StableSeed, "stableSeed");
            if (Facts == null) Facts = new List<BlackTriggerFactSnapshot>();
            if (SourceTraits == null) SourceTraits = new List<BlackTriggerTraitSnapshot>();
            if (SourceAbilities == null) SourceAbilities = new List<BlackTriggerAbilitySnapshot>();
            if (HeirCandidates == null) HeirCandidates = new List<BlackTriggerHeirCandidateSnapshot>();
            if (TraitOptions == null) TraitOptions = new List<BlackTriggerTraitOptionSnapshot>();
        }

        /// <summary>成功形成后切断不再需要的运行时引用。</summary>
        public void DetachRuntimeReferences()
        {
            Maker = null;
            for (int index = 0; index < HeirCandidates.Count; index++)
            {
                if (HeirCandidates[index] != null) HeirCandidates[index].Pawn = null;
            }
        }

        /// <summary>按稳定键取得一项冻结事实。</summary>
        public BlackTriggerFactSnapshot FindFact(string stableKey)
        {
            return Facts.Find(value => value != null && value.StableKey == stableKey);
        }
    }
}
