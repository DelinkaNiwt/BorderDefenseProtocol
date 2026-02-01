using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Staticlord;

public class Vortex : ThingWithComps
{
	public const float RADIUS = 18.9f;

	public const int DURATION = 2500;

	private int startTick;

	private Sustainer sustainer;

	protected override void Tick()
	{
		base.Tick();
		if (sustainer == null)
		{
			sustainer = VPE_DefOf.VPE_Vortex_Sustainer.TrySpawnSustainer(this);
		}
		sustainer.Maintain();
		for (int i = 0; i < 3; i++)
		{
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(RandomLocation(), base.Map, VPE_DefOf.VPE_VortexSpark);
			dataStatic.rotation = Rand.Range(0f, 360f);
			base.Map.flecks.CreateFleck(dataStatic);
			FleckMaker.ThrowSmoke(RandomLocation(), base.Map, 4f);
		}
		if (Find.TickManager.TicksGame - startTick > 2500)
		{
			Destroy();
		}
		if (!this.IsHashIntervalTick(30))
		{
			return;
		}
		foreach (Pawn item in GenRadial.RadialDistinctThingsAround(base.Position, base.Map, 18.9f, useCenter: true).OfType<Pawn>())
		{
			if (item.RaceProps.IsMechanoid)
			{
				item.stances.stunner.StunFor(30, this, addBattleLog: false);
			}
			else if (item.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_Vortex) == null)
			{
				Hediff_Vortexed hediff_Vortexed = (Hediff_Vortexed)HediffMaker.MakeHediff(VPE_DefOf.VPE_Vortex, item);
				hediff_Vortexed.Vortex = this;
				item.health.AddHediff(hediff_Vortexed);
			}
		}
	}

	private Vector3 RandomLocation()
	{
		return DrawPos + new Vector3(Wrap(Mathf.Abs(Rand.Gaussian(0f, 18.9f)), 18.9f), 0f, 0f).RotatedBy(Rand.Range(0f, 360f));
	}

	public static float Wrap(float x, float max)
	{
		while (x > max)
		{
			x -= max;
		}
		return x;
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			startTick = Find.TickManager.TicksGame;
		}
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		base.DeSpawn(mode);
		sustainer.End();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref startTick, "startTick", 0);
	}
}
