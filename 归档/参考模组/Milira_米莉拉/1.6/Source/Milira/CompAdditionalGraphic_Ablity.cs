using AncotLibrary;
using RimWorld;
using Verse;

namespace Milira;

public class CompAdditionalGraphic_Ablity : CompAdditionalGraphic
{
	public override void CompTick()
	{
		((ThingComp)this).CompTick();
		Pawn pawn = ((ThingComp)this).parent as Pawn;
		if (pawn.abilities.AllAbilitiesForReading.Any((Ability a) => a.def.category == MiliraDefOf.Milian_UnitAssist && (bool)a.CanCast && !a.GizmoDisabled(out var _)))
		{
			base.drawGraphic = true;
		}
		else
		{
			base.drawGraphic = false;
		}
	}
}
