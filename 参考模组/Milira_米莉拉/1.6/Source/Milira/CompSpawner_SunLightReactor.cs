using RimWorld;
using Verse;

namespace Milira;

public class CompSpawner_SunLightReactor : CompSpawner
{
	private int ticksUntilSpawn;

	private CompExplosive compExplosive => parent.TryGetComp<CompExplosive>();

	public CompProperties_Spawner_SunLightReactor Props => (CompProperties_Spawner_SunLightReactor)props;

	private bool HasFuel => parent.GetComp<CompRefuelable>()?.HasFuel ?? false;

	private bool PowerOn => parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (!respawningAfterLoad)
		{
			ResetCountdown();
		}
	}

	public override void CompTick()
	{
		TickInterval(1);
	}

	public override void CompTickRare()
	{
		TickInterval(250);
	}

	private void TickInterval(int interval)
	{
		if (!parent.Spawned)
		{
			return;
		}
		CompCanBeDormant comp = parent.GetComp<CompCanBeDormant>();
		if (comp != null)
		{
			if (!comp.Awake)
			{
				return;
			}
		}
		else if (parent.Position.Fogged(parent.Map))
		{
			return;
		}
		if ((!Props.requiresFuel || HasFuel) && (!base.PropsSpawner.requiresPower || PowerOn))
		{
			ticksUntilSpawn -= interval;
			CheckShouldSpawn();
			compExplosive.StartWick();
		}
	}

	private void CheckShouldSpawn()
	{
		if (ticksUntilSpawn <= 0)
		{
			ResetCountdown();
			TryDoSpawn();
		}
	}

	private void ResetCountdown()
	{
		ticksUntilSpawn = base.PropsSpawner.spawnIntervalRange.RandomInRange;
	}
}
