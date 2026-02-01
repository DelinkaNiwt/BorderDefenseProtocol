using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
    public class HediffDynamicIDF : HediffWithComps
    {
        public static int stayTick = 600;

        public Dictionary<int, float> cachedDamage = new Dictionary<int, float>();

        private List<int> tmpInt = new List<int>();

        private List<float> tmpFloat = new List<float>();

        private HediffStage curStage;

        public override HediffStage CurStage
        {
            get
            {
                if (curStage == null && ShouldApply)
                {
                    List<StatModifier> modifiers = new List<StatModifier>();
                    StatModifier incomingFactor = new StatModifier();
                    incomingFactor.stat = StatDefOf.IncomingDamageFactor;
                    incomingFactor.value = (float)Math.Exp(-0.0014391f * cachedDamage.Values.Sum());
                    modifiers.Add(incomingFactor);
                    
                    curStage = new HediffStage();
                    curStage.statFactors = modifiers;
                }
                return curStage;
            }
        }

        public bool ShouldApply
        {
            get
            {
                if (pawn.Dead)
                {
                    return false;
                }
                return true;
            }
        }

        public override void PostTick()
        {
            base.PostTick();
            if (pawn.IsHashIntervalTick(60) && ShouldApply)
            {
                Recache();
            }
        }

        public void Recache()
        {
            if (!cachedDamage.NullOrEmpty())
            {
                curStage = null;
                for (int i = 0; i < cachedDamage.Count; i++)
                {
                    if (cachedDamage.ElementAt(i).Key <= Find.TickManager.TicksGame)
                    {
                        cachedDamage.Remove(cachedDamage.ElementAt(i).Key);
                    }
                }
            }
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (dinfo.Def.harmsHealth && totalDamageDealt > 0)
            {
                int num = Find.TickManager.TicksGame + stayTick;
                while (cachedDamage != null && cachedDamage.Any(pair => pair.Key == num))
                {
                    num++;
                }

                cachedDamage.Add(num, totalDamageDealt);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref cachedDamage, "cachedDamage", LookMode.Value, LookMode.Value, ref tmpInt, ref tmpFloat);
        }
    }
}
