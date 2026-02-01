using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

public class Command_AbilityToggle : Command_Ability
{
	public IAbilityToggle Toggle => base.ability as IAbilityToggle;

	public override string Label
	{
		get
		{
			if (!Toggle.Toggle)
			{
				return ((Command)this).Label;
			}
			return Toggle.OffLabel;
		}
	}

	public Command_AbilityToggle(Pawn pawn, Ability ability)
		: base(pawn, ability)
	{
		if (Toggle.Toggle)
		{
			((Gizmo)this).disabled = false;
			((Gizmo)this).disabledReason = null;
		}
	}

	public override void ProcessInput(Event ev)
	{
		Toggle.Toggle = !Toggle.Toggle;
	}
}
