using System;
using RimWorld;
using Verse;

namespace GD3
{
	public class HediffCompApoBerserk : HediffComp
	{
		public HediffCompProperties_ApoBerserk Props
		{
			get
			{
				return (HediffCompProperties_ApoBerserk)this.props;
			}
		}

		public override void CompPostTick(ref float severityAdjustment)
		{
			Hediff hediff = this.parent;
			if (hediff.Severity >= 1.0 && !hediff.pawn.Dead && hediff.pawn != null)
			{
				bool flag = hediff.pawn.mindState.mentalStateHandler.InMentalState;
				if (flag)
                {
					if (hediff.pawn.mindState.mentalStateHandler.CurStateDef != MentalStateDefOf.Berserk)
                    {
						hediff.pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
						hediff.pawn.mindState.mentalStateHandler.Reset();
					}
                }
				if (hediff.pawn.mindState.mentalStateHandler.CurState == null)
                {
					hediff.pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "CausedByBlackApocriton".Translate(hediff.pawn), true, false, false, null, false, false);
				}

			}
		}
	}
}
