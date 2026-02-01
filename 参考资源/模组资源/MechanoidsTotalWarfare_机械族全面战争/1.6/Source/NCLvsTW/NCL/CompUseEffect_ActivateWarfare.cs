using RimWorld;
using Verse;

namespace NCL;

public class CompUseEffect_ActivateWarfare : CompUseEffect
{
	public CompProperties_UseEffect_ActivateWarfare Props => (CompProperties_UseEffect_ActivateWarfare)props;

	public override void DoEffect(Pawn usedBy)
	{
		if (parent is Building_TotalWarfareActivator { Activated: false } activator)
		{
			activator.ActivateTotalWarfare();
			if (Props.activateEffect != null)
			{
				Props.activateEffect.Spawn(parent.Position, parent.Map).Cleanup();
			}
		}
	}
}
