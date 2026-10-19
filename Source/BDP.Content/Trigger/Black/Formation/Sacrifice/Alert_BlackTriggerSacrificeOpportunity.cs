using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>在个人生命危机时给出一次献祭提示。</summary>
    public sealed class Alert_BlackTriggerSacrificeOpportunity : Alert
    {
        /// <summary>原版警报系统要求无参构造。</summary>
        public Alert_BlackTriggerSacrificeOpportunity()
        {
            defaultPriority = AlertPriority.High;
            defaultLabel = "BDP_BlackTrigger_Sacrifice_AlertLabel".Translate();
            defaultExplanation = "BDP_BlackTrigger_Sacrifice_AlertExplanation".Translate();
        }

        /// <summary>危机存在时激活提示。</summary>
        public override AlertReport GetReport()
        {
            List<Pawn> culprits = new List<Pawn>();
            if (Find.Maps != null)
            {
                for (int mapIndex = 0; mapIndex < Find.Maps.Count; mapIndex++)
                {
                    List<Pawn> pawns = Find.Maps[mapIndex].mapPawns?.FreeColonistsSpawned;
                    if (pawns == null)
                    {
                        continue;
                    }
                    for (int pawnIndex = 0; pawnIndex < pawns.Count; pawnIndex++)
                    {
                        string reasonKey;
                        if (BlackTriggerPersonalCrisisEvaluator.IsCrisis(pawns[pawnIndex], out reasonKey))
                        {
                            culprits.Add(pawns[pawnIndex]);
                        }
                    }
                }
            }
            return AlertReport.CulpritsAre(culprits);
        }
    }
}
