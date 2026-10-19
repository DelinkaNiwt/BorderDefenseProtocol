using Verse;
using Verse.Grammar;

namespace BDP.Content.PlayerLog.CombatBody
{
    /// <summary>战斗体已确认关键事件的唯一提交入口。</summary>
    internal static class CombatBodyPlayerLog
    {
        /// <summary>交给原版战斗日志管理人物关联、分组和容量。</summary>
        internal static void Record(Pawn pawn, string textKey)
        {
            TickManager tickManager = Find.TickManager;
            // 原版日志条目构造器会读取 TickManager.TicksAbs；世界生成阶段起始绝对刻数尚未设置，不能写入日志。
            if (pawn == null || Find.BattleLog == null || tickManager == null
                || tickManager.gameStartAbsTick == 0) return;
            Find.BattleLog.Add(new BattleLogEntry_BdpCombatBodyEvent(pawn, textKey));
        }
    }

    /// <summary>保留原版事件条目的关联和存档，仅补充本地化句式。</summary>
    public sealed class BattleLogEntry_BdpCombatBodyEvent : BattleLogEntry_Event
    {
        /// <summary>事件句式的稳定语言键，不保存已翻译整句。</summary>
        private string textKey;

        /// <summary>供原版深度读档创建条目。</summary>
        public BattleLogEntry_BdpCombatBodyEvent() { }

        /// <summary>从实际发生事件的人物构造记录。</summary>
        public BattleLogEntry_BdpCombatBodyEvent(Pawn pawn, string textKey)
            : base(pawn, DefDatabase<RulePackDef>.GetNamed("BDP_PlayerLog_CombatBodyEvent"), null)
        {
            this.textKey = textKey;
        }

        /// <summary>关键事件始终保留在原版简略视图中。</summary>
        public override bool ShowInCompactView() { return true; }

        /// <summary>复用原版人物规则，由当前语言生成事件句式。</summary>
        protected override GrammarRequest GenerateGrammarRequest()
        {
            GrammarRequest request = base.GenerateGrammarRequest();
            request.Rules.Add(new Rule_String("bdp_sentence", textKey.Translate("[SUBJECT_definite]")));
            return request;
        }

        /// <summary>原版保存人物和文法包，本条目只额外保存事件语言键。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref textKey, "textKey");
        }
    }
}
