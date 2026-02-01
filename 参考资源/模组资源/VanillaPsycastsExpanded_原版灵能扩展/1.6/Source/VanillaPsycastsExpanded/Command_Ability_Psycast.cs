using System.Globalization;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Command_Ability_Psycast : Command_Ability
{
	private readonly AbilityExtension_Psycast psycastExtension;

	public override string TopRightLabel
	{
		get
		{
			if (base.ability.AutoCast)
			{
				return null;
			}
			string text = string.Empty;
			float entropyUsedByPawn = psycastExtension.GetEntropyUsedByPawn(base.ability.pawn);
			if (entropyUsedByPawn > float.Epsilon)
			{
				text += "NeuralHeatLetter".Translate() + ": " + entropyUsedByPawn.ToString(CultureInfo.CurrentCulture) + "\n";
			}
			float psyfocusUsedByPawn = psycastExtension.GetPsyfocusUsedByPawn(base.ability.pawn);
			if (psyfocusUsedByPawn > float.Epsilon)
			{
				text += "PsyfocusLetter".Translate() + ": " + psyfocusUsedByPawn.ToStringPercent();
			}
			return text.TrimEndNewlines();
		}
	}

	public Command_Ability_Psycast(Pawn pawn, Ability ability)
		: base(pawn, ability)
	{
		psycastExtension = ((Def)(object)base.ability.def).GetModExtension<AbilityExtension_Psycast>();
		((Command)this).shrinkable = PsycastsMod.Settings.shrink;
	}
}
