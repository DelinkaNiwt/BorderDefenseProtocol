using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class CompAbilityPlaceBuilding : CompAbilityEffect
{
	public new CompProperties_AbilityPlaceBuilding Props => (CompProperties_AbilityPlaceBuilding)props;

	private Pawn Pawn => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override bool GizmoDisabled(out string reason)
	{
		Map map = Pawn.Map;
		reason = "";
		if (FindValidPosition(map, out var _))
		{
			return false;
		}
		reason = "Ancot.Ability_NoSpace".Translate();
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Map map = Pawn.Map;
		if (FindValidPosition(map, out var validPosition))
		{
			Thing thing = GenSpawn.Spawn(Props.building, validPosition, map);
			if (Props.setFaction)
			{
				thing.SetFaction(Pawn.Faction);
			}
			SpawnEffect(thing);
			parent.comps.OfType<CompAbilityUsedCount>().FirstOrDefault()?.UsedOnce();
		}
		else
		{
			Messages.Message("AbilityNotEnoughFreeSpace".Translate(), Pawn, MessageTypeDefOf.RejectInput, historical: false);
		}
	}

	private void SpawnEffect(Thing thing)
	{
		FleckMaker.Static(thing.TrueCenter(), thing.Map, Props.fleckOnPlace);
		Props.soundOnPlace.PlayOneShot(new TargetInfo(thing.Position, thing.Map));
	}

	private bool FindValidPosition(Map map, out IntVec3 validPosition)
	{
		int num = GenRadial.NumCellsInRadius(4f);
		for (int i = 0; i < num; i++)
		{
			IntVec3 intVec = Pawn.Position + GenRadial.RadialPattern[i];
			if (intVec.IsValid && intVec.InBounds(map))
			{
				validPosition = intVec;
				return true;
			}
		}
		validPosition = IntVec3.Invalid;
		return false;
	}
}
