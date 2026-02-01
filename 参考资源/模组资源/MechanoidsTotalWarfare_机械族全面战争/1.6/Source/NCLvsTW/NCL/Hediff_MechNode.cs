using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class Hediff_MechNode : Hediff
{
	private const int BandNodeCheckInterval = 60;

	private int cachedTunedBandNodesCount;

	private HediffStage curStage;

	public int AdditionalBandwidth => cachedTunedBandNodesCount;

	public override bool ShouldRemove => cachedTunedBandNodesCount == 0;

	public override HediffStage CurStage
	{
		get
		{
			if (curStage == null && cachedTunedBandNodesCount > 0)
			{
				StatModifier statModifier = new StatModifier();
				statModifier.stat = StatDefOf.MechBandwidth;
				statModifier.value = cachedTunedBandNodesCount;
				curStage = new HediffStage();
				curStage.statOffsets = new List<StatModifier> { statModifier };
			}
			return curStage;
		}
	}

	public override void PostTick()
	{
		base.PostTick();
		if (pawn.IsHashIntervalTick(60))
		{
			RecacheBandNodes();
		}
	}

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		RecacheBandNodes();
	}

	public void RecacheBandNodes()
	{
		int num = cachedTunedBandNodesCount;
		cachedTunedBandNodesCount = 0;
		bool foundNode = false;
		List<Map> maps = Find.Maps;
		for (int i = 0; i < maps.Count; i++)
		{
			List<Building> allBuildings = maps[i].listerBuildings.allBuildingsColonist;
			foreach (Building thing in allBuildings)
			{
				CompBandNode bandComp = thing.TryGetComp<CompBandNode>();
				if (bandComp == null)
				{
					continue;
				}
				CompPowerTrader powerComp = thing.TryGetComp<CompPowerTrader>();
				if (bandComp.tunedTo == pawn && powerComp != null && powerComp.PowerOn)
				{
					foundNode = true;
					CompUpgradableBandNode upgradableComp = thing.TryGetComp<CompUpgradableBandNode>();
					if (upgradableComp != null)
					{
						cachedTunedBandNodesCount += upgradableComp.GetBandwidthPoints();
					}
					else
					{
						cachedTunedBandNodesCount += TotalWarfareSettings.AdvancedNodeCount;
					}
				}
			}
		}
		if (num != cachedTunedBandNodesCount || !foundNode)
		{
			curStage = null;
			pawn.mechanitor?.Notify_BandwidthChanged();
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref cachedTunedBandNodesCount, "cachedTunedBandNodesCount", 0);
	}
}
