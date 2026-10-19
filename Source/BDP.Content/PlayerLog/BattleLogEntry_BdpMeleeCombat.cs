using BDP.Core.Semantics;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace BDP.Content.PlayerLog
{
    /// <summary>保留原版招式和伤害结果，只补正式武装名称的近战条目。</summary>
    public sealed class BattleLogEntry_BdpMeleeCombat : BattleLogEntry_MeleeCombat
    {
        /// <summary>攻击发生时的正式来源身份。</summary>
        private PlayerBattleLogGrammar presentation = new PlayerBattleLogGrammar();
        /// <summary>原版装备或身体招式使用的文法前缀。</summary>
        private string ownerRuleName;
        /// <summary>供原版深度读档构造。</summary>
        public BattleLogEntry_BdpMeleeCombat() { }
        /// <summary>原样传递招式、命中分类、工具和伤害图标定义。</summary>
        public BattleLogEntry_BdpMeleeCombat(RulePackDef rules, bool alwaysShow, Pawn initiator, Thing target,
            ImplementOwnerTypeDef implementType, string toolLabel, ThingDef equipment, HediffDef hediff,
            LogEntryDef entryDef, ISemanticContext context)
            : base(rules, alwaysShow, initiator, target, implementType, toolLabel, equipment, hediff, entryDef)
        {
            presentation = new PlayerBattleLogGrammar(context);
            ownerRuleName = implementType?.implementOwnerRuleName;
        }
        /// <summary>不修改招式规则，仅在当前请求内替换正式武装名。</summary>
        protected override GrammarRequest GenerateGrammarRequest()
        {
            GrammarRequest request = base.GenerateGrammarRequest();
            if (!ownerRuleName.NullOrEmpty()) presentation.ApplySource(request, ownerRuleName);
            return request;
        }
        /// <summary>原版负责招式和伤害存档，额外只保存必要来源。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref presentation, "bdpPresentation");
            Scribe_Values.Look(ref ownerRuleName, "bdpOwnerRuleName");
        }
    }
}
