using BDP.Core.Semantics;
using Verse;

namespace BDP.Content.PlayerLog
{
    /// <summary>保留原版远程开火行为，只补完整来源文法的条目。</summary>
    public sealed class BattleLogEntry_BdpRangedFire : BattleLogEntry_RangedFire
    {
        /// <summary>本次来源与文法状态。</summary>
        private PlayerBattleLogGrammar presentation = new PlayerBattleLogGrammar();
        /// <summary>供原版深度读档构造。</summary>
        public BattleLogEntry_BdpRangedFire() { }
        /// <summary>使用原版粒度创建开火记录。</summary>
        public BattleLogEntry_BdpRangedFire(Thing initiator, Thing target, ThingDef weapon, ThingDef projectile,
            bool burst, ISemanticContext context) : base(initiator, target, weapon, projectile, burst)
        {
            presentation = new PlayerBattleLogGrammar(context);
        }
        /// <summary>原版负责缓存与随机种子，这里只提供完整文法请求。</summary>
        protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog)
        {
            return presentation.ResolveFire(base.GenerateGrammarRequest(), forceLog);
        }
        /// <summary>原版存档之外只保存当次来源身份。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref presentation, "bdpPresentation");
        }
    }
}
