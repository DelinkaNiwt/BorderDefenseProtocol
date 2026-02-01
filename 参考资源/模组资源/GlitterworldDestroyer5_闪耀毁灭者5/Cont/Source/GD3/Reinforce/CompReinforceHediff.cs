using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;

namespace GD3
{
	public class CompReinforceHediff : ThingComp
	{
		public bool applied;

		public CompProperties_ReinforceHediff Props
		{
			get
			{
				return (CompProperties_ReinforceHediff)this.props;
			}
		}

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
			if (GDSettings.ReinforceNotApply || !Find.World.GetComponent<MainComponent>().triggered2 || respawningAfterLoad || applied)
            {
				return;
            }
			Pawn pawn = this.parent as Pawn;
			if (pawn.IsBlackMechanoid())
            {
				return;
            }
			BodyPartRecord body = pawn.health.hediffSet.GetBrain();
			bool flag = body != null && (pawn.Faction == null || pawn.Faction.HostileTo(Faction.OfPlayer));
			if (!flag)
            {
				return;
            }
			HediffDef def;
			if ((pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsRangedWeapon) || pawn.IsFlyingMech())
            {
				def = this.Props.hediffsRange.RandomElement();
			}
            else
            {
				def = this.Props.hediffsMelee.RandomElement();
			}
			Random random = new Random();
			if (random.Next(99) <= 0)
            {
				def = GDDefOf.Reinforce_Scrap;
			}
			if (pawn.health?.hediffSet?.GetFirstHediffOfDef(def) == null)
            {
				Hediff hediff = HediffMaker.MakeHediff(def, pawn);
				pawn.health.AddHediff(hediff, body);
				applied = true;
			}
		}

        public override void PostExposeData()
        {
			Scribe_Values.Look(ref applied, "applied");
        }
    }
}
