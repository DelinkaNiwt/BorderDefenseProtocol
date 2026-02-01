using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
	public class HediffCompProperties_HitArmor : HediffCompProperties
	{
		public HediffCompProperties_HitArmor()
		{
			this.compClass = typeof(HediffComp_HitArmor);
		}

		public HediffDef hediffToAdd;

		public int duration;
	}

	public class HediffComp_HitArmor : HediffComp
    {
        public HediffCompProperties_HitArmor Props
        {
            get
            {
                return (HediffCompProperties_HitArmor)this.props;
            }
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
			if (totalDamageDealt == 0)
            {
				return;
            }
			Pawn pawn = this.Pawn;
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
			}
		}
    }
}
