using System;
using RimWorld;
using Verse;

namespace GD3
{
	public class HediffCompTerror : HediffComp
	{
		public HediffCompProperties_Terror Props
		{
			get
			{
				return (HediffCompProperties_Terror)this.props;
			}
		}

		public override void CompPostTick(ref float severityAdjustment)
		{
			Hediff hediff = this.parent;
			if (hediff.Severity >= 1.0 && !hediff.pawn.Downed && !hediff.pawn.Dead && hediff.pawn != null)
			{
				bool flag = hediff.pawn.mindState.mentalStateHandler.InMentalState;
				if (flag)
				{
					return;
				}
				if (hediff.pawn.mindState.mentalStateHandler.CurState == null)
				{
					int num = Rand.Range(0, 10);
					if (num <= 5)
                    {
						hediff.pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_Sad, "CausedByTerror".Translate(), true, false, false, null, false, false);
					}
                    else
                    {
						hediff.pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "CausedByTerror".Translate(), true, false, false, null, false, false);
					}
				}

			}
		}
	}
}
