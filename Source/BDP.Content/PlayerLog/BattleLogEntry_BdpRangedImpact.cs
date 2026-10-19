using BDP.Core.Semantics;
using Verse;
using Verse.Grammar;

namespace BDP.Content.PlayerLog
{
    /// <summary>原版远程伤害条目的BDP扩展，保留人物与伤害部位关联。</summary>
    public sealed class BattleLogEntry_BdpRangedImpact : BattleLogEntry_RangedImpact, IPlayerBattleLogEntry
    {
        /// <summary>当前条目的来源和结果。</summary>
        private PlayerBattleLogGrammar presentation = new PlayerBattleLogGrammar();
        /// <summary>提供Content内部的结果补充口。</summary>
        PlayerBattleLogGrammar IPlayerBattleLogEntry.Presentation => presentation;
        /// <summary>供原版深度读档构造。</summary>
        public BattleLogEntry_BdpRangedImpact() { }
        /// <summary>仍以原版实际受击对象和原意图目标创建记录。</summary>
        public BattleLogEntry_BdpRangedImpact(Thing initiator, Thing recipient, Thing originalTarget,
            ThingDef weapon, ThingDef projectile, ThingDef cover, ISemanticContext context)
            : base(initiator, recipient, originalTarget, weapon, projectile, cover)
        {
            presentation = new PlayerBattleLogGrammar(context);
        }
        /// <summary>保留原版伤害文法，替换当次正式来源名。</summary>
        protected override GrammarRequest GenerateGrammarRequest()
        {
            GrammarRequest request = base.GenerateGrammarRequest();
            presentation.ApplySource(request);
            return request;
        }
        /// <summary>零伤害交互不会被误写为未命中。</summary>
        protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog)
        {
            return presentation.ResolveImpact(GenerateGrammarRequest(), !damagedParts.NullOrEmpty(), deflected, false, forceLog);
        }
        /// <summary>结果补充后沿用原版缓存失效入口。</summary>
        void IPlayerBattleLogEntry.InvalidatePresentation() { ResetCache(); }
        /// <summary>原版结果之外只保存必要来源与交互分类。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref presentation, "bdpPresentation");
        }
    }
}
