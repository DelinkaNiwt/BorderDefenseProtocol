using RimWorld.Planet;
using Verse;

namespace TOT_DLL_test;

public class GameComponent_CeleTech : GameComponent
{
	public static GameComponent_CeleTech Instance;

	public int LastAuxHr;

	public int LastArsHr;

	public int LastSSHr;

	public int MoneySpent;

	public MapParent ASEA_observedMap;

	public int FloatingGunMax
	{
		get
		{
			if (CMC_Def.CMC_FloatingGunVI.IsFinished)
			{
				return 3;
			}
			if (CMC_Def.CMC_FloatingGunV.IsFinished)
			{
				return 2;
			}
			return 1;
		}
	}

	public float FloatingGun_EnergyCap
	{
		get
		{
			if (CMC_Def.CMC_FloatingGunIV.IsFinished)
			{
				return 1.25f;
			}
			return 1f;
		}
	}

	public int FloatingGun_ChargingRate
	{
		get
		{
			if (CMC_Def.CMC_FloatingGunIII.IsFinished)
			{
				return 2;
			}
			return 1;
		}
	}

	public GameComponent_CeleTech(Game game)
	{
		Instance = this;
	}

	public GameComponent_CeleTech()
	{
		Instance = this;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref LastAuxHr, "Auxtradertick", 0);
		Scribe_Values.Look(ref LastArsHr, "Arstradertick", 0);
		Scribe_Values.Look(ref LastSSHr, "Slavetradertick", 0);
		Scribe_Values.Look(ref MoneySpent, "points", 0);
		Scribe_References.Look(ref ASEA_observedMap, "ASEA_ScannedMap");
	}

	public override void GameComponentTick()
	{
		base.GameComponentTick();
		if (Find.TickManager.TicksGame % 3600 == 0)
		{
			LastArsHr++;
			LastAuxHr++;
			LastSSHr++;
		}
	}
}
