using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;

namespace GD3
{
	public class CompProperties_HitArmor : CompProperties
	{
		public CompProperties_HitArmor()
		{
			this.compClass = typeof(CompHitArmor);
		}

		public HediffDef hediffToAdd;

		public int duration;

		public int limitOfTimes = -1;
	}

	public class CompHitArmor : ThingComp
    {
		public CompProperties_HitArmor Props
		{
			get
			{
				return (CompProperties_HitArmor)this.props;
			}
		}

		public bool CanRepair
        {
            get
            {
				if (Props.limitOfTimes == -1)
                {
					return false;
                }
				return timesLeft < Props.limitOfTimes;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (Props.limitOfTimes >= 0 && timesLeft == -1)
            {
				timesLeft = Props.limitOfTimes;
			}
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
			if (totalDamageDealt == 0 || !dinfo.Def.harmsHealth)
			{
				return;
			}
			if (Props.limitOfTimes >= 0 && timesLeft == 0)
            {
				return;
            }
			Pawn pawn = this.parent as Pawn;
			bool flag = pawn != null && pawn.Spawned;
			if (flag && !GDSettings.hitArmorCanNotApply)
			{
				Hediff hitArmor = pawn.health?.hediffSet?.GetFirstHediffOfDef(Props.hediffToAdd);
				if (hitArmor != null)
				{
					HediffComp_Disappears hediffComp_Disappears = hitArmor.TryGetComp<HediffComp_Disappears>();
					if (hediffComp_Disappears != null)
					{
						hediffComp_Disappears.ticksToDisappear = this.Props.duration;
					}
				}
				else
				{
					Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffToAdd, pawn);
					HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
					if (hediffComp_Disappears != null)
					{
						hediffComp_Disappears.ticksToDisappear = this.Props.duration;
					}
					pawn.health.AddHediff(hediff);
				}
				if (Props.limitOfTimes >= 0 && timesLeft > 0)
                {
					timesLeft--;
                }
			}
		}

		public void Notify_RepairMech()
        {
			repairTick++;
			if (repairTick >= 10)
            {
				repairTick = 0;
				timesLeft = Math.Min(timesLeft + 1, Props.limitOfTimes);
            }
		}

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
			if (Find.Selector.SingleSelectedThing == parent && Props.limitOfTimes >= 0)
			{
				HitArmorGizmo gizmo_hitArmor = new HitArmorGizmo(this);
				yield return gizmo_hitArmor;
			}
		}

        public override void PostExposeData()
        {
			Scribe_Values.Look(ref timesLeft, "timesLeft", -1);
			Scribe_Values.Look(ref repairTick, "repairTick");
		}

		public int timesLeft = -1;

		public int repairTick;
    }
}
