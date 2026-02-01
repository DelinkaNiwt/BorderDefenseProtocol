using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class GameComponent_PsycastsManager : GameComponent
{
	public List<GoodwillImpactDelayed> goodwillImpacts = new List<GoodwillImpactDelayed>();

	private bool inited;

	public List<(Thing thing, int tick)> removeAfterTicks = new List<(Thing, int)>();

	private List<Thing> removeAfterTicks_things;

	private List<int> removeAfterTicks_ticks;

	public GameComponent_PsycastsManager(Game game)
	{
	}

	public override void GameComponentTick()
	{
		base.GameComponentTick();
		for (int num = goodwillImpacts.Count - 1; num >= 0; num--)
		{
			GoodwillImpactDelayed goodwillImpactDelayed = goodwillImpacts[num];
			if (Find.TickManager.TicksGame >= goodwillImpactDelayed.impactInTicks)
			{
				goodwillImpactDelayed.DoImpact();
				goodwillImpacts.RemoveAt(num);
			}
		}
		for (int num2 = removeAfterTicks.Count - 1; num2 >= 0; num2--)
		{
			Thing item = removeAfterTicks[num2].thing;
			int item2 = removeAfterTicks[num2].tick;
			if ((item == null || item.Destroyed) ? true : false)
			{
				removeAfterTicks.RemoveAt(num2);
			}
			else if (Find.TickManager.TicksGame >= item2)
			{
				item.Destroy();
				removeAfterTicks.RemoveAt(num2);
			}
		}
	}

	public override void StartedNewGame()
	{
		base.StartedNewGame();
		inited = true;
	}

	public override void LoadedGame()
	{
		base.LoadedGame();
		if (inited)
		{
			return;
		}
		Log.Message("[VPE] Added to existing save, adding PsyLinks.");
		inited = true;
		foreach (Pawn item in Find.WorldPawns.AllPawnsAliveOrDead.Concat(Find.Maps.SelectMany((Map map) => map.mapPawns.AllPawns)))
		{
			List<Hediff_Psylink> resultHediffs = new List<Hediff_Psylink>();
			item?.health?.hediffSet?.GetHediffs(ref resultHediffs);
			Hediff_Psylink hediff_Psylink = resultHediffs.OrderByDescending((Hediff_Psylink p) => p.level).FirstOrDefault();
			if (hediff_Psylink != null && item.Psycasts() == null)
			{
				((Hediff_PsycastAbilities)(object)item.health.AddHediff(VPE_DefOf.VPE_PsycastAbilityImplant, hediff_Psylink.Part)).InitializeFromPsylink(hediff_Psylink);
				item.abilities.abilities.RemoveAll((Ability ab) => ab is Psycast);
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref goodwillImpacts, "goodwillImpacts", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && goodwillImpacts == null)
		{
			goodwillImpacts = new List<GoodwillImpactDelayed>();
		}
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			removeAfterTicks_things = new List<Thing>();
			removeAfterTicks_ticks = new List<int>();
			for (int i = 0; i < removeAfterTicks.Count; i++)
			{
				removeAfterTicks_things.Add(removeAfterTicks[i].thing);
				removeAfterTicks_ticks.Add(removeAfterTicks[i].tick);
			}
		}
		Scribe_Collections.Look(ref removeAfterTicks_things, "removeAfterTick_things", LookMode.Reference);
		Scribe_Collections.Look(ref removeAfterTicks_ticks, "removeAfterTick_ticks", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			removeAfterTicks = new List<(Thing, int)>();
			for (int j = 0; j < removeAfterTicks_things.Count; j++)
			{
				removeAfterTicks.Add((removeAfterTicks_things[j], removeAfterTicks_ticks[j]));
			}
		}
		Scribe_Values.Look(ref inited, "inited", defaultValue: false);
	}
}
