using RimWorld;
using Verse;
using Verse.Sound;

namespace TurbojetBackpack;

public class CompAbility_Magazine : CompAbilityEffect
{
	private int charges;

	private int ticksToNextCharge;

	public new CompProperties_AbilityMagazine Props => (CompProperties_AbilityMagazine)props;

	public int Charges => charges;

	public int MaxCharges => Props.maxCharges;

	public float ReloadProgress => 1f - (float)ticksToNextCharge / (float)Props.reloadTicks;

	public override void Initialize(AbilityCompProperties props)
	{
		base.Initialize(props);
		charges = Props.maxCharges;
		ticksToNextCharge = 0;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (charges < Props.maxCharges)
		{
			ticksToNextCharge--;
			if (ticksToNextCharge <= 0)
			{
				ReloadOneCharge();
			}
		}
	}

	private void ReloadOneCharge()
	{
		charges++;
		if (Props.soundReload != null && parent.pawn.Spawned && parent.pawn.IsColonistPlayerControlled)
		{
			Props.soundReload.PlayOneShot(new TargetInfo(parent.pawn.Position, parent.pawn.Map));
		}
		if (charges < Props.maxCharges)
		{
			ticksToNextCharge = Props.reloadTicks;
		}
		else
		{
			ticksToNextCharge = 0;
		}
	}

	public bool TryConsumeCharge()
	{
		if (charges > 0)
		{
			charges--;
			if (ticksToNextCharge <= 0)
			{
				ticksToNextCharge = Props.reloadTicks;
			}
			return true;
		}
		return false;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref charges, "charges", Props.maxCharges);
		Scribe_Values.Look(ref ticksToNextCharge, "ticksToNextCharge", 0);
	}
}
