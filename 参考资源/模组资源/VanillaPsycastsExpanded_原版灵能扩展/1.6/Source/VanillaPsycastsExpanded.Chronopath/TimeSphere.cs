using RimWorld;
using UnityEngine;
using VEF.Buildings;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Chronopath;

[StaticConstructorOnStartup]
public class TimeSphere : Thing
{
	private static readonly Material DistortionMat = DistortedMaterialsPool.DistortedMaterial("Things/Mote/Black", "Things/Mote/PsycastDistortionMask", 0.1f, 1.5f);

	public int Duration;

	public float Radius;

	private int startTick;

	private Sustainer sustainer;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			startTick = Find.TickManager.TicksGame;
		}
		foreach (Thing item in GenRadial.RadialDistinctThingsAround(base.Position, base.Map, Radius, useCenter: true))
		{
			if (item is Pawn pawn)
			{
				Faction faction = item.Faction;
				if (faction != null && !faction.IsPlayer && !pawn.Faction.HostileTo(Faction.OfPlayer))
				{
					pawn.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -75, canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.UsedHarmfulAbility);
				}
			}
		}
	}

	protected override void Tick()
	{
		if (this.IsHashIntervalTick(60))
		{
			foreach (Thing item in GenRadial.RadialDistinctThingsAround(base.Position, base.Map, Radius, useCenter: true))
			{
				if (item is Pawn pawn)
				{
					AbilityExtension_Age.Age(pawn, 1f);
				}
				if (item is Plant plant)
				{
					if (plant.Growth < 1f)
					{
						plant.Growth = 1f;
					}
					else if (plant.def.useHitPoints)
					{
						item.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 0.01f * (float)item.MaxHitPoints));
					}
					else
					{
						plant.Age = int.MaxValue;
					}
				}
				if (item is Building && item.def.useHitPoints)
				{
					item.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 0.01f * (float)item.MaxHitPoints));
				}
			}
		}
		if (sustainer == null)
		{
			sustainer = VPE_DefOf.VPE_TimeSphere_Sustainer.TrySpawnSustainer(this);
		}
		else
		{
			sustainer.Maintain();
		}
		if (Find.TickManager.TicksGame >= startTick + Duration)
		{
			Destroy();
		}
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		sustainer.End();
		base.Destroy(mode);
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		drawLoc.y = AltitudeLayer.MoteOverheadLow.AltitudeFor();
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawLoc, Quaternion.AngleAxis(0f, Vector3.up), Vector3.one * Radius * 1.75f), DistortionMat, 0);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref Radius, "radius", 0f);
		Scribe_Values.Look(ref Duration, "duration", 0);
		Scribe_Values.Look(ref startTick, "startTick", 0);
	}
}
