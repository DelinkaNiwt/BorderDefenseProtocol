using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompAbilityPlaceBuildingInFront : CompAbilityEffect
{
	public new CompProperties_AbilityPlaceBuildingInFront Props => (CompProperties_AbilityPlaceBuildingInFront)props;

	public Pawn Pawn => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		IntVec3 validPosition;
		CellRect occupiedRect;
		return FindValidPosition(target, Pawn.Map, Props.building, out validPosition, out occupiedRect);
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Map map = Pawn.Map;
		if (FindValidPosition(target, map, Props.building, out var validPosition, out var _))
		{
			PlaceBuilding(target, validPosition, map);
			parent.comps.OfType<CompAbilityUsedCount>().FirstOrDefault()?.UsedOnce();
		}
		else if (Pawn.Faction.IsPlayer)
		{
			Messages.Message("AbilityNotEnoughFreeSpace".Translate(), Pawn, MessageTypeDefOf.RejectInput, historical: false);
		}
	}

	public virtual void PlaceBuilding(LocalTargetInfo target, IntVec3 position, Map map)
	{
		Thing thing = GenSpawn.Spawn(Props.building, position, map);
		thing.Rotation = Pawn.Rotation;
		if (Props.setFaction)
		{
			thing.SetFaction(Pawn.Faction);
		}
		SpawnEffect(thing);
	}

	public void SpawnEffect(Thing thing)
	{
		if (Props.effecter != null)
		{
			Effecter effecter = Props.effecter.Spawn();
			effecter.Trigger(new TargetInfo(thing.Position, thing.Map), new TargetInfo(thing.Position, thing.Map));
			effecter.Cleanup();
		}
	}

	private bool FindValidPosition(LocalTargetInfo target, Map map, ThingDef building, out IntVec3 validPosition, out CellRect occupiedRect)
	{
		Rot4 cardinalDirection = GetCardinalDirection(Pawn.Position, target.Cell);
		IntVec3 intVec = Pawn.Position + cardinalDirection.FacingCell;
		occupiedRect = GenAdj.OccupiedRect(intVec, cardinalDirection, building.Size);
		foreach (IntVec3 item in occupiedRect)
		{
			if (!item.InBounds(map) || !item.Walkable(map) || item.GetEdifice(map) != null)
			{
				validPosition = IntVec3.Invalid;
				return false;
			}
		}
		validPosition = intVec;
		return true;
	}

	private Rot4 GetCardinalDirection(IntVec3 pawnPos, IntVec3 targetPos)
	{
		IntVec3 intVec = targetPos - pawnPos;
		float num = Mathf.Atan2(intVec.z, intVec.x) * 57.29578f;
		if (num < -135f || num > 135f)
		{
			return Rot4.West;
		}
		if (num > -135f && num < -45f)
		{
			return Rot4.South;
		}
		if (num > -45f && num < 45f)
		{
			return Rot4.East;
		}
		return Rot4.North;
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		if (FindValidPosition(target, Pawn.Map, Props.building, out var _, out var occupiedRect))
		{
			GenDraw.DrawFieldEdges(occupiedRect.ToList());
		}
	}
}
