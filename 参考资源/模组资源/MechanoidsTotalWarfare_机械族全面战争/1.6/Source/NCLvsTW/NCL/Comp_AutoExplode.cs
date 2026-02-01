using Verse;

namespace NCL;

public class Comp_AutoExplode : ThingComp
{
	private int ticksRemaining;

	private bool activated = false;

	public CompProperties_AutoExplode Props => (CompProperties_AutoExplode)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			ticksRemaining = Props.fuseTicks;
		}
		activated = true;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (activated && parent.Spawned)
		{
			ticksRemaining--;
			if (ticksRemaining <= 0)
			{
				Detonate();
			}
		}
	}

	private void Detonate()
	{
		if (!parent.Destroyed)
		{
			GenExplosion.DoExplosion(parent.Position, parent.Map, Props.explosiveRadius, Props.damageType, parent, Props.damAmount, Props.armorPenetration, null, null, null, null, null, 0f, 0, null, null, 255, applyDamageToExplosionCellsNeighbors: true, null, 0f, 0, 0.5f);
			parent.Destroy();
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", Props.fuseTicks);
		Scribe_Values.Look(ref activated, "activated", defaultValue: false);
	}

	public override string CompInspectStringExtra()
	{
		if (activated && ticksRemaining > 0)
		{
			float secondsRemaining = (float)ticksRemaining / 60f;
			return string.IsNullOrEmpty(Props.customCountdownText) ? ((string)"NCL.AutoExplodeCountdown".Translate(secondsRemaining.ToString("0.0"))) : string.Format(Props.customCountdownText, secondsRemaining.ToString("0.0"));
		}
		return base.CompInspectStringExtra();
	}
}
