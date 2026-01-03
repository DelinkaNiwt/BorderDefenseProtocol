using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test;

public class Comp_FloatSwordAttack : HediffComp
{
	private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

	private int lastAttackTargetTick = 0;

	public CompProperties_FloatSwordAttack Props => (CompProperties_FloatSwordAttack)props;

	public Pawn Holder => base.Pawn;

	public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;

	public int LastAttackTargetTick => lastAttackTargetTick;

	public void Tick()
	{
		if (Find.TickManager.TicksGame % 300 == 0)
		{
			Log.Message(Holder);
			if (Holder != null && Holder.drafter.Drafted)
			{
				List<Thing> obj = Holder.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn);
				Log.Message(obj);
			}
		}
	}
}
