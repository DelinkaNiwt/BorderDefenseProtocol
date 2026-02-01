using RimWorld;
using Verse;

namespace NCL;

public class Comp_AloneDetonator : ThingComp
{
	private int ticksRemaining;

	private bool activated = false;

	private bool isCountingDown = false;

	private int nextCheckTick = 0;

	public CompProperties_AloneDetonator Props => (CompProperties_AloneDetonator)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			ticksRemaining = Props.fuseTicks;
			activated = true;
			nextCheckTick = Find.TickManager.TicksGame;
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		if (!activated || !parent.Spawned)
		{
			return;
		}
		if (Find.TickManager.TicksGame >= nextCheckTick)
		{
			CheckFactionPresence();
			nextCheckTick = Find.TickManager.TicksGame + Props.checkInterval;
		}
		if (isCountingDown)
		{
			ticksRemaining--;
			if (ticksRemaining <= 0)
			{
				Detonate();
			}
		}
	}

	private void CheckFactionPresence()
	{
		bool hasFactionMember = HasFactionMemberOnMap();
		if (!hasFactionMember && !isCountingDown)
		{
			isCountingDown = true;
			Messages.Message("NCL.AloneDetonatorActivated".Translate(parent.Label), parent, MessageTypeDefOf.ThreatBig);
		}
		else if (hasFactionMember && isCountingDown)
		{
			isCountingDown = false;
			ticksRemaining = Props.fuseTicks;
			Messages.Message("NCL.AloneDetonatorDeactivated".Translate(parent.Label), parent, MessageTypeDefOf.PositiveEvent);
		}
	}

	private bool HasFactionMemberOnMap()
	{
		if (parent.Faction == null)
		{
			return false;
		}
		Map map = parent.Map;
		if (map == null)
		{
			return false;
		}
		foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
		{
			if (pawn.Faction == parent.Faction && pawn.HostFaction == null && (Props.includeDownedPawns || !pawn.Downed) && !pawn.Dead)
			{
				return true;
			}
		}
		return false;
	}

	private void Detonate()
	{
		if (!parent.Destroyed)
		{
			IntVec3 position = parent.Position;
			Map map = parent.Map;
			parent.Destroy();
			if (Props.spawnExplosionEffect)
			{
				GenExplosion.DoExplosion(position, map, Props.explosiveRadius, Props.damageType, null, Props.damAmount, Props.armorPenetration, null, null, null, null, null, 0f, 0, null, null, 255, applyDamageToExplosionCellsNeighbors: true, null, 0f, 0, 0.5f);
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", Props.fuseTicks);
		Scribe_Values.Look(ref activated, "activated", defaultValue: false);
		Scribe_Values.Look(ref isCountingDown, "isCountingDown", defaultValue: false);
		Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", 0);
	}

	public override string CompInspectStringExtra()
	{
		if (isCountingDown && ticksRemaining > 0)
		{
			float secondsRemaining = (float)ticksRemaining / 60f;
			return string.IsNullOrEmpty(Props.customCountdownText) ? ((string)"NCL.AloneDetonatorCountdown".Translate(secondsRemaining.ToString("0.0"))) : string.Format(Props.customCountdownText, secondsRemaining.ToString("0.0"));
		}
		return base.CompInspectStringExtra();
	}
}
