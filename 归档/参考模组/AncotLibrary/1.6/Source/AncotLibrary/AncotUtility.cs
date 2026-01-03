using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public static class AncotUtility
{
	public static float QualityFactor(QualityCategory quality)
	{
		return quality switch
		{
			QualityCategory.Awful => 0.8f, 
			QualityCategory.Poor => 0.9f, 
			QualityCategory.Normal => 1f, 
			QualityCategory.Good => 1.1f, 
			QualityCategory.Excellent => 1.2f, 
			QualityCategory.Masterwork => 1.4f, 
			QualityCategory.Legendary => 1.65f, 
			_ => 1f, 
		};
	}

	public static float QualityFactorII(QualityCategory quality)
	{
		return quality switch
		{
			QualityCategory.Awful => 1f, 
			QualityCategory.Poor => 1f, 
			QualityCategory.Normal => 1f, 
			QualityCategory.Good => 1.1f, 
			QualityCategory.Excellent => 1.25f, 
			QualityCategory.Masterwork => 1.6f, 
			QualityCategory.Legendary => 2f, 
			_ => 1f, 
		};
	}

	public static void DoDamage(Thing thing, DamageDef damageDef, float damageAmount, float armorPenetration)
	{
		if (thing != null)
		{
			DamageInfo dinfo = new DamageInfo(damageDef, damageAmount, armorPenetration);
			thing.TakeDamage(dinfo);
		}
	}

	public static void DoDamage(Thing thing, DamageDef damageDef, float damageAmount, float armorPenetration, Thing instigator)
	{
		if (thing != null)
		{
			DamageInfo dinfo = new DamageInfo(damageDef, damageAmount, armorPenetration, -1f, instigator);
			thing.TakeDamage(dinfo);
		}
	}

	public static List<IntVec3> AffectedCellsRadial(IntVec3 target, Map map, float radius, bool useCenter)
	{
		List<IntVec3> list = new List<IntVec3>();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(target, radius, useCenter))
		{
			if (item.IsValid || item.InBounds(map))
			{
				list.Add(item);
			}
		}
		list = list.Distinct().ToList();
		list.RemoveAll((IntVec3 cell) => !CanUseCell(cell));
		return list;
		bool CanUseCell(IntVec3 c)
		{
			if (!c.InBounds(map))
			{
				return false;
			}
			return true;
		}
	}

	public static bool IsPawnAffected(Pawn pawn, Thing caster, bool applyAllyOnly, bool applyOnMech, bool applyOnMechOnly, bool ignoreCaster)
	{
		if (applyAllyOnly && pawn.Faction != caster.Faction)
		{
			return false;
		}
		if (!applyOnMech && pawn.RaceProps.IsMechanoid)
		{
			return false;
		}
		if (applyOnMechOnly && !pawn.RaceProps.IsMechanoid)
		{
			return false;
		}
		Pawn pawn2 = caster as Pawn;
		if (pawn2 != null && ignoreCaster && pawn == pawn2)
		{
			return false;
		}
		return true;
	}

	public static Color GetQualityColor(ThingWithComps ap)
	{
		if (!ap.TryGetQuality(out var qc))
		{
			return new Color(0f, 0f, 0f, 0f);
		}
		return qc switch
		{
			QualityCategory.Awful => AncotLibrarySettings.color_Awful, 
			QualityCategory.Poor => AncotLibrarySettings.color_Poor, 
			QualityCategory.Normal => AncotLibrarySettings.color_Normal, 
			QualityCategory.Good => AncotLibrarySettings.color_Good, 
			QualityCategory.Excellent => AncotLibrarySettings.color_Excellent, 
			QualityCategory.Masterwork => AncotLibrarySettings.color_Masterwork, 
			QualityCategory.Legendary => AncotLibrarySettings.color_Legendary, 
			_ => Color.white, 
		};
	}

	public static bool HasNamedBodyPart(BodyPartDef part, string partLabel, Pawn pawn)
	{
		return (part == null && partLabel.NullOrEmpty()) || GetBodyPart(part, partLabel, pawn) != null;
	}

	public static BodyPartRecord GetBodyPart(BodyPartDef part, string partLabel, Pawn pawn)
	{
		return pawn.health.hediffSet.GetNotMissingParts()?.FirstOrDefault((BodyPartRecord bpr) => IsBodyPart(bpr, part, partLabel));
	}

	public static bool IsBodyPart(BodyPartRecord bpr, BodyPartDef part, string partLabel)
	{
		return (partLabel.NullOrEmpty() || bpr.untranslatedCustomLabel == partLabel) && (part == null || bpr.def == part);
	}
}
