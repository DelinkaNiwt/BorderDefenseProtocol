using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class CompAbilityEffect_SwordShower : CompAbilityEffect
{
	public new CompProperties_AbilitySwordShower Props => (CompProperties_AbilitySwordShower)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Pawn pawn = parent.pawn;
		SkillDummy_Sword skillDummy_Sword = (SkillDummy_Sword)ThingMaker.MakeThing(CMC_Def.CMC_SkillDummy);
		GenSpawn.Spawn(skillDummy_Sword, pawn.Position, pawn.Map);
		skillDummy_Sword.Insert(pawn);
		skillDummy_Sword.IsSword = true;
	}

	public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
	{
		if (target.Cell.Roofed(parent.pawn.Map))
		{
			if (throwMessages)
			{
				Messages.Message("CannotUseAbility".Translate(parent.def.label) + ": " + "AbilityRoofed".Translate(), target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return true;
	}

	public override void OnGizmoUpdate()
	{
	}
}
