using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class CompHaywire : ThingComp
{
	private int ticksLeft;

	private Effecter effecter;

	public void GoHaywire(int duration)
	{
		ticksLeft = Mathf.Max(duration, ticksLeft);
		HaywireManager.HaywireThings.Add(parent);
		if (effecter == null)
		{
			effecter = VPE_DefOf.VPE_Haywire.Spawn(parent, parent.Map);
			effecter.Trigger(parent, parent);
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		if (ticksLeft > 0)
		{
			effecter.EffectTick(parent, parent);
			ticksLeft--;
			if (ticksLeft <= 0)
			{
				HaywireManager.HaywireThings.Remove(parent);
				effecter.Cleanup();
				effecter = null;
			}
		}
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		base.PostDeSpawn(map, mode);
		if (HaywireManager.HaywireThings.Contains(parent))
		{
			HaywireManager.HaywireThings.Remove(parent);
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (ticksLeft > 0)
		{
			HaywireManager.HaywireThings.Add(parent);
			effecter = VPE_DefOf.VPE_Haywire.Spawn(parent, parent.Map);
			effecter.Trigger(parent, parent);
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ticksLeft, "haywireTicksLeft", 0);
	}
}
