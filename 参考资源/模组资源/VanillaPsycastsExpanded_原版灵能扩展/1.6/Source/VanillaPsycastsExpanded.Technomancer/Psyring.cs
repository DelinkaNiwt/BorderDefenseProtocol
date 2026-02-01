using System.Linq;
using HarmonyLib;
using RimWorld;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

[HarmonyPatch]
public class Psyring : Apparel
{
	private AbilityDef ability;

	private bool alreadyHad;

	public AbilityDef Ability => ability;

	public bool Added => !alreadyHad;

	public PsycasterPathDef Path => ability.Psycast().path;

	public override string Label => base.Label + " (" + ((Def)(object)ability).LabelCap + ")";

	public void Init(AbilityDef ability)
	{
		this.ability = ability;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look<AbilityDef>(ref ability, "ability");
		Scribe_Values.Look(ref alreadyHad, "alreadyHad", defaultValue: false);
	}

	public override void Notify_Equipped(Pawn pawn)
	{
		base.Notify_Equipped(pawn);
		if (ability == null)
		{
			Log.Warning("[VPE] Psyring present with no ability, destroying.");
			Destroy();
			return;
		}
		CompAbilities comp = ((ThingWithComps)pawn).GetComp<CompAbilities>();
		if (comp != null)
		{
			alreadyHad = comp.HasAbility(ability);
			if (!alreadyHad)
			{
				comp.GiveAbility(ability);
			}
		}
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		base.Notify_Unequipped(pawn);
		if (ability == null)
		{
			return;
		}
		if (!alreadyHad)
		{
			((ThingWithComps)pawn).GetComp<CompAbilities>().LearnedAbilities.RemoveAll((Ability ab) => ab.def == ability);
		}
		alreadyHad = false;
	}

	[HarmonyPatch(typeof(FloatMenuOptionProvider_Wear), "GetSingleOptionFor")]
	[HarmonyPostfix]
	public static void EquipConditions(Thing clickedThing, FloatMenuContext context, ref FloatMenuOption __result)
	{
		if (__result == null)
		{
			return;
		}
		Pawn firstSelectedPawn = context.FirstSelectedPawn;
		if (firstSelectedPawn.apparel != null && clickedThing is Psyring psyring && __result.Label.Contains("ForceWear".Translate(psyring.LabelShort, psyring)))
		{
			if (firstSelectedPawn.Psycasts() == null)
			{
				__result = new FloatMenuOption(string.Format("{0} ({1})", "CannotWear".Translate(psyring.LabelShort, psyring), "VPE.NotPsycaster".Translate()), null);
			}
			if (firstSelectedPawn.apparel.WornApparel.OfType<Psyring>().Any())
			{
				__result = new FloatMenuOption(string.Format("{0} ({1})", "CannotWear".Translate(psyring.LabelShort, psyring), "VPE.AlreadyPsyring".Translate()), null);
			}
		}
	}
}
