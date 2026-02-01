using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;

namespace GD3
{
    public class FloatMenuOptionProvider_ModifySavingMech : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        protected override bool RequiresManipulation => true;

        protected override bool CanSelfTarget => false;

        protected override bool AppliesInt(FloatMenuContext context)
        {
            return CanTakeOrder(context.FirstSelectedPawn) && !LordBlocksFloatMenu(context.FirstSelectedPawn);
        }

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            if (pawn == null)
            {
                yield break;
            }
            Pawn p = clickedThing as Pawn;
            if (p == null || p.health?.hediffSet == null)
            {
                yield break;
            }
            IEnumerable<HediffComp_SavingMech> comps = p.health.hediffSet.GetHediffComps<HediffComp_SavingMech>();
            if (comps.EnumerableNullOrEmpty())
            {
                yield break;
            }
            HediffComp_SavingMech comp = comps.First();
            bool flag = comp != null && comp.allowModify && !comp.modified;
            if (!flag)
            {
                yield break;
            }
            MenuOptionPriority priority = clickedThing == null ? MenuOptionPriority.VeryLow : MenuOptionPriority.GoHere;
            FloatMenuOption floatMenuOption = new FloatMenuOption("GD.ModifySavingMech".Translate(clickedThing.LabelShort), delegate
            {
                Job job = JobMaker.MakeJob(GDDefOf.GD_ModifySavingMech, clickedThing);
                job.count = 1;
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }, priority);
            yield return floatMenuOption;
        }

        private bool CanTakeOrder(Pawn pawn)
        {
            if (!pawn.IsColonistPlayerControlled)
            {
                return pawn.IsColonySubhumanPlayerControlled;
            }
            return true;
        }

        private bool LordBlocksFloatMenu(Pawn pawn)
        {
            return !(pawn.GetLord()?.AllowsFloatMenu(pawn) ?? ((AcceptanceReport)true));
        }
    }
}
