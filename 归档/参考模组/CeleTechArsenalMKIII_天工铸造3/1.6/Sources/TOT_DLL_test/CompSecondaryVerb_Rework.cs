using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

internal class CompSecondaryVerb_Rework : ThingComp
{
	private Verb verbInt = null;

	private CompEquippable compEquippableInt;

	private bool isSecondaryVerbSelected;

	public CompProperties_SecondaryVerb_Rework Props => (CompProperties_SecondaryVerb_Rework)props;

	public bool IsSecondaryVerbSelected => isSecondaryVerbSelected;

	private CompEquippable EquipmentSource
	{
		get
		{
			if (compEquippableInt != null)
			{
				return compEquippableInt;
			}
			compEquippableInt = parent.TryGetComp<CompEquippable>();
			if (compEquippableInt == null)
			{
				Log.ErrorOnce(parent.LabelCap + " has CompSecondaryVerb but no CompEquippable", 50020);
			}
			return compEquippableInt;
		}
	}

	public Pawn CasterPawn => Verb.caster as Pawn;

	private Verb Verb
	{
		get
		{
			if (verbInt == null)
			{
				verbInt = EquipmentSource.PrimaryVerb;
			}
			return verbInt;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (CasterPawn == null || CasterPawn.Faction.Equals(Faction.OfPlayer))
		{
			string commandIcon = (IsSecondaryVerbSelected ? Props.secondaryCommandIcon : Props.mainCommandIcon);
			if (commandIcon == "")
			{
				commandIcon = "UI/Buttons/Reload";
			}
			yield return new Command_Action
			{
				action = SwitchVerb,
				defaultLabel = (IsSecondaryVerbSelected ? Props.secondaryWeaponLabel : Props.mainWeaponLabel),
				defaultDesc = Props.description,
				icon = ContentFinder<Texture2D>.Get(commandIcon, reportFailure: false)
			};
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref isSecondaryVerbSelected, "CMC_useSecondaryVerb", defaultValue: false);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			PostAmmoDataLoaded();
		}
	}

	private void SwitchVerb()
	{
		if (!IsSecondaryVerbSelected)
		{
			EquipmentSource.PrimaryVerb.verbProps = Props.verbProps;
			isSecondaryVerbSelected = true;
		}
		else
		{
			EquipmentSource.PrimaryVerb.verbProps = parent.def.Verbs[0];
			isSecondaryVerbSelected = false;
		}
		typeof(Verb).GetField("cachedTicksBetweenBurstShots", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(Verb, null);
		typeof(Verb).GetField("cachedBurstShotCount", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(Verb, null);
	}

	private void PostAmmoDataLoaded()
	{
		if (isSecondaryVerbSelected)
		{
			EquipmentSource.PrimaryVerb.verbProps = Props.verbProps;
		}
	}
}
