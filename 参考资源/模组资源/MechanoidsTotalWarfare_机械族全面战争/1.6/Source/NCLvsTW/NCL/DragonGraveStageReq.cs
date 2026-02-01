using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class DragonGraveStageReq
{
	public List<ThingDefCountClass> Things;

	public int timeDuration = 6000;

	public List<ThingDefCountRangeClass> Rewards;

	public List<ThingDefCountRangeClass> OutRewards;

	[MustTranslate]
	public string completeMessage;

	public GraphicData graphic;

	public List<FactionDef> raidFactions;

	public IntRange raidPointRange;

	public List<PawnKindDefCount> boss;

	public float rechargePower = 1000f;

	public bool extraCenterDrop;

	public bool extraMechCluster;

	public bool raidWhenCountdownStart;
}
