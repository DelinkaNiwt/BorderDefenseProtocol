using System;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompBlock : ThingComp
{
	public int blockStanceTicksRemaining = -1;

	public CompProperties_Block Props => (CompProperties_Block)props;

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

	public override void CompTickInterval(int delta)
	{
		if (blockStanceTicksRemaining > 0)
		{
			blockStanceTicksRemaining -= delta;
		}
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		absorbed = false;
		float num = (float)(PawnOwner?.skills?.GetSkill(SkillDefOf.Melee)?.Level).GetValueOrDefault() * Props.meleeSkillBonusChance;
		if (PawnOwner.Faction != null)
		{
			Faction faction = dinfo.Instigator.Faction;
			if (faction != null && !faction.HostileTo(PawnOwner.Faction))
			{
				return;
			}
		}
		if (((!dinfo.Def.isExplosive && !dinfo.Def.isRanged) || Props.blockRanged) && (Props.blockMelee || dinfo.Def.isExplosive || dinfo.Def.isRanged) && !(PawnOwner.stances.curStance is Stance_Warmup) && Rand.Chance(Math.Min(Props.baseBlockChance + num, Props.maxBlockChance)) && (!Props.useWeaponCharge || (compCharge != null && compCharge.CanBeUsed)))
		{
			compCharge?.UsedOnce();
			AncotDefOf.Ancot_ShieldBlock.Spawn().Trigger(PawnOwner, dinfo.Instigator ?? PawnOwner);
			MoteMaker.ThrowText(PawnOwner.DrawPos, PawnOwner.Map, "Ancot.TextMote_Block".Translate(), 1.9f);
			absorbed = true;
			PawnOwner.stances.SetStance(new Stance_Cooldown(Props.blockStanceTick, dinfo.Instigator ?? null, null));
			blockStanceTicksRemaining = Props.blockStanceTick;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref blockStanceTicksRemaining, "blockStanceTicksRemaining", 0);
	}
}
