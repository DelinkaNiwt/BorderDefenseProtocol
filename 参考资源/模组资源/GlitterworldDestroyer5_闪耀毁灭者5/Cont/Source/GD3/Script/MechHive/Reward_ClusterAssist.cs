using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;
using RimWorld;

namespace GD3
{
    [StaticConstructorOnStartup]
    public class Reward_ClusterAssist : Reward
    {
        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Symbols/ClusterReward");

        public override IEnumerable<GenUI.AnonymousStackElement> StackElements
        {
            get
            {
                yield return QuestPartUtility.GetStandardRewardStackElement("Reward_ClusterAssist_Label".Translate(), Icon, () => GetDescription(default(RewardsGeneratorParams)).CapitalizeFirst());
            }
        }

        public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
        {
            throw new NotImplementedException();
        }

        public override string GetDescription(RewardsGeneratorParams parms)
        {
            return "Reward_ClusterAssist".Translate().Resolve();
        }
    }
}
