using System.Collections.Generic;
using Verse;

namespace TowerLaserDefense;

public class GameComponent_BulletsCache : GameComponent
{
	public static List<Thing> BulletsCache = new List<Thing>();

	public GameComponent_BulletsCache(Game game)
	{
	}

	public override void GameComponentTick()
	{
		for (int num = BulletsCache.Count - 1; num >= 0; num--)
		{
			Thing thing = BulletsCache[num];
			if (thing == null || thing.Destroyed || !thing.Spawned)
			{
				BulletsCache.RemoveAt(num);
			}
			else
			{
				foreach (LaserDefenceCore instance in LaserDefenceCore.Instances)
				{
					if (instance?.Parent == null || instance.Parent.Destroyed || !instance.Parent.Spawned || !instance.TryLockTarget(thing))
					{
						continue;
					}
					BulletsCache.RemoveAt(num);
					break;
				}
			}
		}
	}

	public override void ExposeData()
	{
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			BulletsCache.RemoveAll((Thing b) => b?.Destroyed ?? true);
		}
		else if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			BulletsCache.Clear();
			LaserDefenceCore.Instances.Clear();
		}
	}

	public override void LoadedGame()
	{
		base.LoadedGame();
		LaserDefenceCore.CleanupAllInstances();
		BulletsCache.RemoveAll((Thing b) => b == null || b.Destroyed || !b.Spawned);
	}

	public override void StartedNewGame()
	{
		base.StartedNewGame();
		BulletsCache.Clear();
		LaserDefenceCore.Instances.Clear();
	}

	public static void ClearStaticCache()
	{
		BulletsCache.Clear();
		LaserDefenceCore.Instances.Clear();
	}
}
