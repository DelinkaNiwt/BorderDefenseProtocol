using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public static class EquipmentUtility
{
	private static readonly SimpleCurve RecoilCurveAxisX = new SimpleCurve
	{
		new CurvePoint(0f, 0f),
		new CurvePoint(0.3f, 0.03f),
		new CurvePoint(0.6f, 0.045f),
		new CurvePoint(0.8f, 0.049f),
		new CurvePoint(1f, 0.05f)
	};

	private static readonly SimpleCurve RecoilCurveAxisY = new SimpleCurve
	{
		new CurvePoint(0f, 0f),
		new CurvePoint(0.3f, 0.03f),
		new CurvePoint(0.6f, 0.045f),
		new CurvePoint(0.8f, 0.049f),
		new CurvePoint(1f, 0.05f)
	};

	private static readonly SimpleCurve RecoilCurveRotation = new SimpleCurve
	{
		new CurvePoint(0f, 0f),
		new CurvePoint(1f, 3f),
		new CurvePoint(2f, 4f)
	};

	public static bool CanEquip(Thing thing, Pawn pawn)
	{
		string cantReason;
		return CanEquip(thing, pawn, out cantReason);
	}

	public static bool CanEquip(Thing thing, Pawn pawn, out string cantReason, bool checkBonded = true)
	{
		cantReason = null;
		CompBladelinkWeapon compBladelinkWeapon = thing.TryGetComp<CompBladelinkWeapon>();
		if (compBladelinkWeapon != null && compBladelinkWeapon.Biocodable && compBladelinkWeapon.CodedPawn != null && compBladelinkWeapon.CodedPawn != pawn)
		{
			cantReason = "BladelinkBondedToSomeoneElse".Translate();
			return false;
		}
		if (CompBiocodable.IsBiocoded(thing) && !CompBiocodable.IsBiocodedFor(thing, pawn))
		{
			cantReason = "BiocodedCodedForSomeoneElse".Translate();
			return false;
		}
		if (checkBonded && AlreadyBondedToWeapon(thing, pawn))
		{
			cantReason = "BladelinkAlreadyBondedMessage".Translate(pawn.Named("PAWN"), pawn.equipment.bondedWeapon.Named("BONDEDWEAPON"));
			return false;
		}
		if (RolePreventsFromUsing(pawn, thing, out cantReason))
		{
			return false;
		}
		if (thing.def.IsApparel && !thing.def.apparel.developmentalStageFilter.Has(pawn.DevelopmentalStage))
		{
			cantReason = "WrongDevelopmentalStageForClothing".Translate(pawn.DevelopmentalStage.ToString().Translate(), Find.ActiveLanguageWorker.WithIndefiniteArticlePostProcessed(thing.def.apparel.developmentalStageFilter.ToCommaListOr()));
			return false;
		}
		return true;
	}

	public static bool AlreadyBondedToWeapon(Thing thing, Pawn pawn)
	{
		CompBladelinkWeapon compBladelinkWeapon = thing.TryGetComp<CompBladelinkWeapon>();
		if (compBladelinkWeapon == null || !compBladelinkWeapon.Biocodable)
		{
			return false;
		}
		if (pawn.equipment.bondedWeapon != null)
		{
			return pawn.equipment.bondedWeapon != thing;
		}
		return false;
	}

	public static string GetPersonaWeaponConfirmationText(Thing item, Pawn p)
	{
		CompBladelinkWeapon compBladelinkWeapon = item.TryGetComp<CompBladelinkWeapon>();
		if (compBladelinkWeapon != null && compBladelinkWeapon.Biocodable && compBladelinkWeapon.CodedPawn != p)
		{
			TaggedString taggedString = "BladelinkEquipWarning".Translate();
			List<WeaponTraitDef> traitsListForReading = compBladelinkWeapon.TraitsListForReading;
			if (!traitsListForReading.NullOrEmpty())
			{
				taggedString += "\n\n" + "BladelinkEquipWarningTraits".Translate() + ":";
				for (int i = 0; i < traitsListForReading.Count; i++)
				{
					taggedString += "\n\n" + traitsListForReading[i].LabelCap + ": " + traitsListForReading[i].description;
				}
			}
			taggedString += "\n\n" + "RoyalWeaponEquipConfirmation".Translate();
			return taggedString;
		}
		return null;
	}

	public static bool IsBondedTo(Thing thing, Pawn pawn)
	{
		CompBladelinkWeapon compBladelinkWeapon = thing.TryGetComp<CompBladelinkWeapon>();
		if (compBladelinkWeapon != null)
		{
			return compBladelinkWeapon.CodedPawn == pawn;
		}
		return false;
	}

	public static bool QuestLodgerCanEquip(Thing thing, Pawn pawn)
	{
		if (pawn.equipment.Primary != null && !QuestLodgerCanUnequip(pawn.equipment.Primary, pawn))
		{
			return false;
		}
		if (CompBiocodable.IsBiocodedFor(thing, pawn))
		{
			return true;
		}
		if (AlreadyBondedToWeapon(thing, pawn))
		{
			return true;
		}
		return thing.def.IsWeapon;
	}

	public static bool RolePreventsFromUsing(Pawn pawn, Thing thing, out string reason)
	{
		if (ModsConfig.IdeologyActive && pawn.Ideo != null)
		{
			Precept_Role role = pawn.Ideo.GetRole(pawn);
			if (role != null && !role.CanEquip(pawn, thing, out reason))
			{
				return true;
			}
		}
		reason = null;
		return false;
	}

	public static bool QuestLodgerCanUnequip(Thing thing, Pawn pawn)
	{
		if (CompBiocodable.IsBiocodedFor(thing, pawn))
		{
			return false;
		}
		if (IsBondedTo(thing, pawn))
		{
			return false;
		}
		return true;
	}

	public static Verb_LaunchProjectile GetRecoilVerb(List<Verb> allWeaponVerbs)
	{
		Verb_LaunchProjectile verb_LaunchProjectile = null;
		foreach (Verb allWeaponVerb in allWeaponVerbs)
		{
			if (allWeaponVerb is Verb_LaunchProjectile verb_LaunchProjectile2 && (verb_LaunchProjectile == null || verb_LaunchProjectile.LastShotTick < verb_LaunchProjectile2.LastShotTick))
			{
				verb_LaunchProjectile = verb_LaunchProjectile2;
			}
		}
		return verb_LaunchProjectile;
	}

	public static void Recoil(ThingDef weaponDef, Verb_LaunchProjectile shootVerb, out Vector3 drawOffset, out float angleOffset, float aimAngle)
	{
		drawOffset = Vector3.zero;
		angleOffset = 0f;
		if (!(weaponDef.recoilPower > 0f) || shootVerb == null)
		{
			return;
		}
		Rand.PushState(shootVerb.LastShotTick);
		try
		{
			int num = Find.TickManager.TicksGame - shootVerb.LastShotTick;
			if ((float)num < weaponDef.recoilRelaxation)
			{
				float num2 = Mathf.Clamp01((float)num / weaponDef.recoilRelaxation);
				float num3 = Mathf.Lerp(weaponDef.recoilPower, 0f, num2);
				drawOffset = new Vector3(0f, 0f, 0f - RecoilCurveAxisY.Evaluate(num2)) * num3;
				drawOffset = drawOffset.RotatedBy(aimAngle);
			}
		}
		finally
		{
			Rand.PopState();
		}
	}

	public static void VerbRefresh(CompEquippable comp)
	{
		Verb primaryVerb = comp.PrimaryVerb;
		typeof(Verb).GetField("cachedTicksBetweenBurstShots", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(primaryVerb, null);
		typeof(Verb).GetField("cachedBurstShotCount", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(primaryVerb, null);
		primaryVerb.Reset();
	}

	public static void CompRefresh(CompEquippable comp)
	{
		if (comp is CompEquippableAbility obj)
		{
			typeof(CompEquippableAbility).GetField("ability", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(obj, null);
		}
	}
}
