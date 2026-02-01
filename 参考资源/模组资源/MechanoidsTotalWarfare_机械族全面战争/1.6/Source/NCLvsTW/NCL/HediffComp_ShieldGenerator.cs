using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class HediffComp_ShieldGenerator : HediffComp
{
	public HediffCompProperties_ShieldGenerator Props => (HediffCompProperties_ShieldGenerator)props;

	public bool CanApply
	{
		get
		{
			Pawn pawn = base.Pawn;
			return pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed;
		}
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		Pawn pawn = base.Pawn;
		if (!CanApply)
		{
			return;
		}
		List<Thing> projectiles = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
		if (projectiles.Count <= 0)
		{
			return;
		}
		foreach (Thing thing in projectiles)
		{
			if (thing is Projectile projectile && projectile.Position.DistanceTo(pawn.Position) <= Props.range && IsHostileProjectile(projectile, pawn))
			{
				GenerateShield(pawn.Position, pawn.Map, pawn.Faction, projectile.def.projectile.flyOverhead);
				pawn.health.RemoveHediff(parent);
				break;
			}
		}
	}

	private bool IsHostileProjectile(Projectile projectile, Pawn owner)
	{
		if (projectile.Launcher == null || projectile.Launcher.Faction == null)
		{
			return true;
		}
		Faction faction = owner.Faction;
		if (faction != null && faction.IsPlayer)
		{
			return !projectile.Launcher.Faction.HostileTo(owner.Faction);
		}
		return projectile.Launcher.Faction.HostileTo(owner.Faction);
	}

	public void GenerateShield(IntVec3 pos, Map map, Faction f, bool flyOverhead)
	{
		if (pos.IsValid && pos.InBounds(map))
		{
			ThingDef shieldDef = (flyOverhead ? NCLDefOf.NCL_FullAngelShieldProjector : NCLDefOf.NCL_LowAngelShieldProjector);
			Thing shield = ThingMaker.MakeThing(shieldDef);
			shield.SetFaction(f);
			GenPlace.TryPlaceThing(shield, pos, map, ThingPlaceMode.Near);
			SpawnEffect(shield);
		}
	}

	private static void SpawnEffect(Thing projector)
	{
		FleckMaker.Static(projector.TrueCenter(), projector.Map, FleckDefOf.BroadshieldActivation);
	}
}
