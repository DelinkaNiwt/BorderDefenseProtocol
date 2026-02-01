using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompAuraEffect : ThingComp
{
	private int ticksUntilParticleSpawn;

	private List<Mote> activeMotes = new List<Mote>();

	private Effecter effecter;

	private int ticksUntilEffecterTrigger;

	private CompProperties_AuraEffect Props => (CompProperties_AuraEffect)props;

	private Pawn TargetPawn => parent as Pawn;

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		ticksUntilParticleSpawn = Props.spawnIntervalTicks;
		if (Props.effecterDef != null)
		{
			effecter = Props.effecterDef.Spawn();
		}
		ticksUntilEffecterTrigger = Props.effecterTriggerInterval;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (TargetPawn != null && TargetPawn.Spawned)
		{
			UpdateParticleSystem();
			UpdateEffecter();
		}
	}

	private void UpdateParticleSystem()
	{
		if (--ticksUntilParticleSpawn <= 0)
		{
			ticksUntilParticleSpawn = Props.spawnIntervalTicks;
			SpawnParticles();
		}
		UpdateExistingMotes();
	}

	private void SpawnParticles()
	{
		Map map = TargetPawn.Map;
		if (map == null)
		{
			return;
		}
		Vector3 basePos = TargetPawn.DrawPos + Props.offset;
		if (Props.fleckDef != null)
		{
			for (int i = 0; i < Props.fleckCount; i++)
			{
				Vector3 fleckPos = basePos + GetRandomOffset();
				FleckMaker.Static(fleckPos, map, Props.fleckDef, Props.scale);
			}
		}
		if (Props.moteDef == null)
		{
			return;
		}
		Vector3 motePos = basePos + GetRandomOffset();
		if (motePos.ShouldSpawnMotesAt(map))
		{
			Mote mote = (Mote)ThingMaker.MakeThing(Props.moteDef);
			mote.exactPosition = motePos;
			mote.Scale = Props.scale;
			mote.instanceColor = Props.particleColor;
			if (mote is MoteAttached attachedMote)
			{
				attachedMote.Attach(TargetPawn);
			}
			else
			{
				mote.LinkMote(TargetPawn);
			}
			GenSpawn.Spawn(mote, motePos.ToIntVec3(), map);
			activeMotes.Add(mote);
		}
	}

	private Vector3 GetRandomOffset()
	{
		return Quaternion.AngleAxis(Rand.Range(0, 360), Vector3.up) * Vector3.forward * Props.spawnRadius.RandomInRange;
	}

	private void UpdateExistingMotes()
	{
		activeMotes.RemoveAll((Mote mote2) => mote2?.Destroyed ?? true);
		foreach (Mote mote in activeMotes)
		{
			if (!(mote is MoteAttached))
			{
				mote.exactPosition = TargetPawn.DrawPos + Props.offset;
			}
		}
	}

	private void UpdateEffecter()
	{
		if (effecter != null)
		{
			effecter.EffectTick(TargetPawn, new TargetInfo(TargetPawn.Position, TargetPawn.Map));
			if (--ticksUntilEffecterTrigger <= 0)
			{
				ticksUntilEffecterTrigger = Props.effecterTriggerInterval;
				effecter.Trigger(TargetPawn, new TargetInfo(TargetPawn.Position, TargetPawn.Map));
			}
		}
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		Cleanup();
	}

	private void Cleanup()
	{
		effecter?.Cleanup();
		effecter = null;
		activeMotes.Clear();
	}
}
