using System.Collections.Generic;
using Verse;

namespace NCLWorm;

public class GameComp_NCLWorm : GameComponent
{
	public bool firstCall = true;

	public bool inWormWar = false;

	public bool OutWar = false;

	public int wartime = 300000;

	public int tradetime = 0;

	public int ReLongTime = 0;

	public List<string> Usedcalltools = new List<string>();

	public GameComp_NCLWorm(Game game)
	{
	}

	public override void GameComponentTick()
	{
		base.GameComponentTick();
		if (Find.TickManager.TicksGame % 2000 != 0)
		{
			return;
		}
		if (tradetime > 0)
		{
			tradetime -= 2000;
		}
		if (ReLongTime > 0)
		{
			tradetime -= 2000;
		}
		if (!inWormWar)
		{
			return;
		}
		foreach (Map playerHomeMap in Current.Game.PlayerHomeMaps)
		{
			if (playerHomeMap.weatherManager.curWeather.defName != "DryThunderstorm")
			{
				playerHomeMap.weatherManager.curWeather = DefDatabase<WeatherDef>.GetNamed("DryThunderstorm");
			}
		}
		wartime -= 2000;
		if (wartime <= 0)
		{
			inWormWar = false;
			wartime = 300000;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref firstCall, "firstCall", defaultValue: false);
		Scribe_Values.Look(ref inWormWar, "inWormWar", defaultValue: false);
		Scribe_Values.Look(ref wartime, "wartime", 0);
		Scribe_Values.Look(ref tradetime, "tradetime", 0);
		Scribe_Values.Look(ref ReLongTime, "ReLongTime", 0);
		Scribe_Collections.Look(ref Usedcalltools, "Usedcalltools", LookMode.Value);
	}
}
