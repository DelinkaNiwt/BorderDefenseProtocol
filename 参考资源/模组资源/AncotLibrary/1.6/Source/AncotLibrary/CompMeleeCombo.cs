using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompMeleeCombo : ThingComp
{
	public int comboNum = 0;

	public CompProperties_MeleeCombo Props => (CompProperties_MeleeCombo)props;

	protected Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return (parent?.ParentHolder as Pawn_EquipmentTracker)?.pawn;
			}
			return wearer;
		}
	}

	public CompWeaponCharge compCharge => parent.TryGetComp<CompWeaponCharge>();

	public void TryComboOnce()
	{
		if (Rand.Chance(Props.comboChance) && comboNum < Props.maxCombo)
		{
			if (Props.useWeaponCharge)
			{
				CompWeaponCharge compWeaponCharge = compCharge;
				if (compWeaponCharge == null || !compWeaponCharge.CanBeUsed)
				{
					goto IL_00bb;
				}
			}
			compCharge?.UsedOnce();
			comboNum++;
			PawnOwner.stances.SetStance(new Stance_Cooldown(Props.comboStanceTick, PawnOwner.CurJob?.targetA.Thing ?? null, null));
			return;
		}
		goto IL_00bb;
		IL_00bb:
		comboNum = 0;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref comboNum, "comboNum", 0);
	}
}
