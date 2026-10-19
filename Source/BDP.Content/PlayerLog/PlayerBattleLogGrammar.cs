using System.Linq;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Semantics;
using Verse;
using Verse.Grammar;

namespace BDP.Content.PlayerLog
{
    /// <summary>只用于当前BDP条目的文法状态，不观察人物持续状态。</summary>
    internal sealed class PlayerBattleLogGrammar : IExposable
    {
        /// <summary>冻结的正式来源身份。</summary>
        private PlayerBattleLogSource source = new PlayerBattleLogSource();
        /// <summary>当次承伤结果分类。</summary>
        private DamageResolutionOutcome outcome;
        /// <summary>本次交互明确声明的主要直接结果文法。</summary>
        private RulePackDef directResult;
        /// <summary>直接结果对应的效果定义。</summary>
        private HediffDef resultEffect;
        /// <summary>直接结果涉及的固定身体定义。</summary>
        private BodyDef resultBody;
        /// <summary>身体树内的固定部位位置；不保存已翻译部位名。</summary>
        private int resultPartIndex = -1;
        /// <summary>重要直接后果可替换普通附着结果，避免列出效果流水。</summary>
        private bool importantResult;
        /// <summary>构造空状态供原版深度读档使用。</summary>
        public PlayerBattleLogGrammar() { }
        /// <summary>从已确认的攻击来源构造条目状态。</summary>
        internal PlayerBattleLogGrammar(ISemanticContext semanticContext)
        {
            source = PlayerBattleLogSource.Capture(semanticContext);
        }

        /// <summary>补充真实承伤结果，绝不由零伤害推断未命中。</summary>
        internal void Complete(DamageResolution resolution, bool blockedByShield)
        {
            outcome = blockedByShield ? DamageResolutionOutcome.ShieldBlocked
                : resolution != null ? resolution.Outcome : outcome;
        }

        /// <summary>只替换当前请求中的来源名称，保留原版部位和结果文法。</summary>
        internal void ApplySource(GrammarRequest request, string prefix = "WEAPON")
        {
            string label = source?.ResolveLabel();
            if (!label.NullOrEmpty())
            {
                SetLabel(request, prefix, label);
                // 独立能力的命中沿用原版弹体句式，但从固定能力来源取名称；物理弹体定义仍保留。
                if (source.IsAbilitySource && request.Rules.Any(rule => rule.keyword == "PROJECTILE_label"))
                    SetLabel(request, "PROJECTILE", label);
                // 原版的WEAPON_missing还决定弹体取词，不能因补来源名就伪造静态武器输入。
            }
        }

        /// <summary>生成完整开火句，缺失来源或目标时只省略相应成分。</summary>
        internal string ResolveFire(GrammarRequest request, bool forceLog)
        {
            string label = source?.ResolveLabel();
            bool hasTarget = request.Constants.TryGetValue("RECIPIENT_missing", out string missing) && missing == "True"
                ? false : HasName(request, "RECIPIENT");
            string key = hasTarget ? label.NullOrEmpty() ? "BDP_Log_FireTarget" : "BDP_Log_FireTargetSource"
                : label.NullOrEmpty() ? "BDP_Log_Fire" : "BDP_Log_FireSource";
            string sentence = key.Translate(NameToken(request, "INITIATOR"), label ?? "", NameToken(request, "RECIPIENT"));
            return ResolveSentence(request, sentence, forceLog);
        }

        /// <summary>普通伤害沿用原版；无部位的实际交互使用准确的简短句。</summary>
        internal string ResolveImpact(GrammarRequest request, bool hasDamageParts, bool deflected,
            bool area, bool forceLog)
        {
            bool hasTarget = HasName(request, "RECIPIENT");
            if (!hasTarget)
            {
                return GrammarResolver.Resolve("r_logentry", request, null, forceLog);
            }
            if (hasDamageParts || deflected)
            {
                string native = GrammarResolver.Resolve("r_logentry", request, null, forceLog);
                return AppendClause(request, native, forceLog);
            }
            string key = outcome == DamageResolutionOutcome.ShieldBlocked ? "BDP_Log_DamageBlocked"
                : area ? "BDP_Log_AreaContact" : "BDP_Log_Contact";
            string sentence = ResolveSentence(request, key.Translate(NameToken(request, "INITIATOR"),
                AttackToken(request), NameToken(request, "RECIPIENT")), forceLog);
            return AppendClause(request, sentence, forceLog);
        }

        /// <summary>保存本次已确认的主要后果；不为持续状态变化生成记录。</summary>
        internal void AppendConfirmedResult(RulePackDef rules, HediffDef effect, BodyPartRecord part, bool important)
        {
            if (directResult != null && (!important || importantResult)) return;
            directResult = rules;
            resultEffect = effect;
            resultBody = part?.body;
            resultPartIndex = part != null ? part.body.AllParts.IndexOf(part) : -1;
            importantResult = important;
        }

        /// <summary>通过文法把必要后果合入同一条记录，普通伤害部位仍来自原版。</summary>
        private string AppendClause(GrammarRequest request, string sentence, bool forceLog)
        {
            if (directResult == null) return sentence;
            if (resultEffect != null) request.Rules.AddRange(GrammarUtility.RulesForDef("BDP_EFFECT", resultEffect));
            string partLabel = resultBody != null && resultPartIndex >= 0 && resultPartIndex < resultBody.AllParts.Count
                ? resultBody.AllParts[resultPartIndex].Label : "BDP_Log_UnknownPart".Translate().ToString();
            request.Rules.Add(new Rule_String("bdp_result_effect", "BDP_Log_ResultEffect".Translate("[BDP_EFFECT_label]")));
            request.Rules.Add(new Rule_String("bdp_result_weight", "BDP_Log_ResultWeight".Translate()));
            request.Rules.Add(new Rule_String("bdp_result_part", "BDP_Log_ResultPartRemoved".Translate(partLabel)));
            request.Includes.Add(directResult);
            string clause = GrammarResolver.Resolve("bdp_result", request, null, forceLog);
            return "BDP_Log_ResultWithClause".Translate(sentence.TrimEnd('。', '.'), clause);
        }

        /// <summary>通过原版文法解析完整句，避免直接替换全局显示方法。</summary>
        internal static string ResolveSentence(GrammarRequest request, string sentence, bool forceLog)
        {
            request.Includes.Clear();
            request.Includes.Add(DefDatabase<RulePackDef>.GetNamed("BDP_PlayerLog_Sentence"));
            request.Rules.Add(new Rule_String("bdp_sentence", sentence));
            return GrammarResolver.Resolve("r_logentry", request, null, forceLog);
        }

        /// <summary>选择当前请求实际拥有的人物或对象名称。</summary>
        private static string NameToken(GrammarRequest request, string prefix)
        {
            return HasName(request, prefix) ? "[" + prefix + "_definite]" : "BDP_Log_UnknownActor".Translate().ToString();
        }

        /// <summary>判断原版请求是否提供了可用对象名。</summary>
        private static bool HasName(GrammarRequest request, string prefix)
        {
            return request.Rules.Any(rule => rule.keyword == prefix + "_definite");
        }

        /// <summary>优先使用弹体名称，缺失时读取通用攻击语言键。</summary>
        private static string AttackToken(GrammarRequest request)
        {
            return request.Rules.Any(rule => rule.keyword == "PROJECTILE_label") ? "[PROJECTILE_label]"
                : request.Rules.Any(rule => rule.keyword == "WEAPON_projectile_label") ? "[WEAPON_projectile_label]"
                : "BDP_Log_UnknownAttack".Translate().ToString();
        }

        /// <summary>只修改当前请求的三个名称规则，不影响原版定义或其它条目。</summary>
        internal static void SetLabel(GrammarRequest request, string prefix, string label)
        {
            request.Rules.RemoveAll(rule => rule.keyword == prefix + "_label"
                || rule.keyword == prefix + "_definite" || rule.keyword == prefix + "_indefinite");
            request.Rules.Add(new Rule_String(prefix + "_label", label));
            request.Rules.Add(new Rule_String(prefix + "_definite", Find.ActiveLanguageWorker.WithDefiniteArticle(label)));
            request.Rules.Add(new Rule_String(prefix + "_indefinite", Find.ActiveLanguageWorker.WithIndefiniteArticle(label)));
            request.Constants[prefix + "_gender"] = LanguageDatabase.activeLanguage.ResolveGender(label).ToString();
        }

        /// <summary>保存本条记录的来源与结果，不重播已经发生的事件。</summary>
        public void ExposeData()
        {
            Scribe_Deep.Look(ref source, "source");
            Scribe_Values.Look(ref outcome, "outcome", DamageResolutionOutcome.NotResolved);
            Scribe_Defs.Look(ref directResult, "directResult");
            Scribe_Defs.Look(ref resultEffect, "resultEffect");
            Scribe_Defs.Look(ref resultBody, "resultBody");
            Scribe_Values.Look(ref resultPartIndex, "resultPartIndex", -1);
            Scribe_Values.Look(ref importantResult, "importantResult");
        }
    }

    /// <summary>Content内部共享本条记录的文法状态与缓存失效操作。</summary>
    internal interface IPlayerBattleLogEntry
    {
        /// <summary>当前条目的文法状态。</summary>
        PlayerBattleLogGrammar Presentation { get; }
        /// <summary>实际结果变化后使原版文字缓存失效。</summary>
        void InvalidatePresentation();
    }
}
