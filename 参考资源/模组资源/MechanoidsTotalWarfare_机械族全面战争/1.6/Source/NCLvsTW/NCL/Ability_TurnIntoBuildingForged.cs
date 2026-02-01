using RimWorld;
using Verse;

namespace NCL;

public class Ability_TurnIntoBuildingForged : Ability
{
	public Ability_TurnIntoBuildingForged(Pawn pawn)
		: base(pawn)
	{
	}

	public Ability_TurnIntoBuildingForged(Pawn pawn, AbilityDef def)
		: base(pawn, def)
	{
	}

	public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (!base.Activate(target, dest))
		{
			return false;
		}
		if (pawn.Map == null)
		{
			Log.Error("Tried to turn " + pawn.Label + " into building but pawn is not on any map");
			return false;
		}
		IntVec3 position = pawn.Position;
		Map map = pawn.Map;
		Building_PawnContainerForged building = Building_PawnContainerForged.MakeSleepingBuilding(pawn);
		if (building == null)
		{
			Log.Error("Failed to create building container");
			return false;
		}
		if (pawn.Spawned)
		{
			pawn.DeSpawn();
		}
		GenSpawn.Spawn(building, position, map);
		FleckMaker.ThrowDustPuff(position, map, 2f);
		return true;
	}

	public override bool CanApplyOn(LocalTargetInfo target)
	{
		return target.Pawn == pawn && base.CanApplyOn(target);
	}
}
