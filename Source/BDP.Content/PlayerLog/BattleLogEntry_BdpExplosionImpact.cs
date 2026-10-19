using BDP.Core.Semantics;
using Verse;
using Verse.Grammar;

namespace BDP.Content.PlayerLog
{
    /// <summary>原版爆炸逐目标条目的BDP扩展，不汇总其它目标的效果。</summary>
    public sealed class BattleLogEntry_BdpExplosionImpact : BattleLogEntry_ExplosionImpact, IPlayerBattleLogEntry
    {
        /// <summary>本次目标的来源和结果。</summary>
        private PlayerBattleLogGrammar presentation = new PlayerBattleLogGrammar();
        /// <summary>Content内部本次结果补充口。</summary>
        PlayerBattleLogGrammar IPlayerBattleLogEntry.Presentation => presentation;
        /// <summary>供原版深度读档构造。</summary>
        public BattleLogEntry_BdpExplosionImpact() { }
        /// <summary>使用实际目标创建一次范围交互记录。</summary>
        public BattleLogEntry_BdpExplosionImpact(Thing initiator, Thing recipient, ThingDef weapon,
            ThingDef projectile, DamageDef damage, ISemanticContext context)
            : base(initiator, recipient, weapon, projectile, damage)
        {
            presentation = new PlayerBattleLogGrammar(context);
        }
        /// <summary>保留原版部位文法，只补当次正式来源。</summary>
        protected override GrammarRequest GenerateGrammarRequest()
        {
            GrammarRequest request = base.GenerateGrammarRequest();
            presentation.ApplySource(request);
            return request;
        }
        /// <summary>取消普通伤害的范围作用使用准确直接结果句。</summary>
        protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog)
        {
            return presentation.ResolveImpact(GenerateGrammarRequest(), !damagedParts.NullOrEmpty(), deflected, true, forceLog);
        }
        /// <summary>补充结果后使原版文字缓存失效。</summary>
        void IPlayerBattleLogEntry.InvalidatePresentation() { ResetCache(); }
        /// <summary>额外只保存本条来源及结果事实。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref presentation, "bdpPresentation");
        }
    }
}
