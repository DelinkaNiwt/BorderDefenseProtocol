using System.Linq;
using RimWorld;
using Verse;

namespace NCL;

public class Comp_SkillRefresh : ThingComp
{
	private CompProperties_SkillRefresh Props => (CompProperties_SkillRefresh)props;

	public override void CompTick()
	{
		base.CompTick();
		if (parent is Pawn { Map: not null, Dead: false } owner && owner.IsHashIntervalTick(Props.checkIntervalTicks) && !AreTargetPawnsPresent(owner.Map))
		{
			RefreshAllAbilities(owner);
		}
	}

	private bool AreTargetPawnsPresent(Map map)
	{
		if (Props.targetPawnDefs.NullOrEmpty() || map == null)
		{
			return false;
		}
		return map.mapPawns.AllPawnsSpawned.Any((Pawn p) => !p.Dead && Props.targetPawnDefs.Contains(p.def));
	}

	private void RefreshAllAbilities(Pawn owner)
	{
		if (owner.abilities == null)
		{
			return;
		}
		foreach (Ability ability in owner.abilities.abilities)
		{
			ability.ResetCooldown();
			if (ability.UsesCharges)
			{
				ability.RemainingCharges = ability.maxCharges;
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
	}
}
