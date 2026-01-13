using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TurbojetBackpack;

public class Ability_MagazineLaunch : Ability
{
	private CompAbility_Magazine magazineComp;

	public CompAbility_Magazine MagazineComp
	{
		get
		{
			if (magazineComp == null && comps != null)
			{
				for (int i = 0; i < comps.Count; i++)
				{
					if (comps[i] is CompAbility_Magazine compAbility_Magazine)
					{
						magazineComp = compAbility_Magazine;
						break;
					}
				}
			}
			return magazineComp;
		}
	}

	public Ability_MagazineLaunch(Pawn pawn)
		: base(pawn)
	{
	}

	public Ability_MagazineLaunch(Pawn pawn, AbilityDef def)
		: base(pawn, def)
	{
	}

	public override bool GizmoDisabled(out string reason)
	{
		if (base.GizmoDisabled(out reason))
		{
			return true;
		}
		if (MagazineComp != null && MagazineComp.Charges <= 0)
		{
			reason = "Turbojet_MagazineEmpty".Translate();
			return true;
		}
		return false;
	}

	public override IEnumerable<Command> GetGizmos()
	{
		if (MagazineComp == null)
		{
			foreach (Command gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			yield break;
		}
		foreach (Command cmd in base.GetGizmos())
		{
			if (cmd is Command_Ability)
			{
				yield return new Command_AbilityMagazine(this, pawn, MagazineComp)
				{
					defaultLabel = def.label
				};
			}
			else
			{
				yield return cmd;
			}
		}
	}
}
