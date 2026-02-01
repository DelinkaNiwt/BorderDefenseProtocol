using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class WeaponTool
{
	internal static bool HasAnyWeapon(this Pawn pawn)
	{
		return pawn.HasEquipmentTracker() && !pawn.equipment.AllEquipmentListForReading.NullOrEmpty();
	}

	internal static bool IsEquippedByPawn(this Pawn pawn, ThingWithComps w)
	{
		return pawn.HasAnyWeapon() && pawn.equipment.AllEquipmentListForReading.Contains(w);
	}

	internal static bool HasAnyWeaponTags(this ThingDef weapon)
	{
		return weapon != null && !weapon.weaponTags.NullOrEmpty();
	}

	internal static bool DoesExplosion(this ThingDef weapon)
	{
		return weapon.HasVerb() && weapon.Verbs[0].CausesExplosion;
	}

	internal static bool HasVerb(this ThingDef weapon)
	{
		return weapon != null && !weapon.Verbs.NullOrEmpty();
	}

	internal static bool HasProjectile(this ThingDef weapon)
	{
		return weapon != null && !weapon.Verbs.NullOrEmpty() && weapon.Verbs[0].defaultProjectile != null && weapon.Verbs[0].defaultProjectile.projectile != null;
	}

	internal static bool HasSoundcast(this ThingDef weapon)
	{
		return weapon != null && !weapon.Verbs.NullOrEmpty() && weapon.Verbs[0].soundCast != null;
	}

	internal static ThingWithComps RandomEquippedWeapon(this Pawn pawn)
	{
		return pawn.HasAnyWeapon() ? pawn.equipment.AllEquipmentListForReading.RandomElement() : null;
	}

	internal static ThingWithComps ThisOrFirstWeapon(this Pawn p, ThingWithComps w)
	{
		return (!p.HasAnyWeapon()) ? null : (p.IsEquippedByPawn(w) ? w : p.equipment.AllEquipmentListForReading.FirstOrDefault());
	}

	internal static string GetAllWeaponsAsSeparatedString(this Pawn p)
	{
		if (!p.HasEquipmentTracker() || p.equipment.AllEquipmentListForReading.NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (ThingWithComps item in p.equipment.AllEquipmentListForReading)
		{
			text += item.GetAsSeparatedString();
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetWeaponsFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasEquipmentTracker())
		{
			return;
		}
		try
		{
			p.equipment.DestroyAllEquipment();
			if (s.NullOrEmpty())
			{
				return;
			}
			string[] array = s.SplitNo(":");
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				string[] array3 = s2.SplitNo("|");
				if (array3.Length >= 6)
				{
					string styledefname = ((array3.Length >= 7) ? array3[6] : "");
					ThingWithComps thingWithComps = GenerateWeapon(Selected.ByName(array3[0], array3[1], styledefname, array3[2].HexStringToColor(), array3[3].AsInt32(), array3[4].AsInt32()));
					if (thingWithComps != null)
					{
						thingWithComps.HitPoints = array3[5].AsInt32();
						p.equipment.AddEquipment(thingWithComps);
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void DestroyAllEquipment(this Pawn pawn)
	{
		if (pawn.HasEquipmentTracker())
		{
			pawn.equipment.DestroyAllEquipment();
		}
	}

	internal static void DestroyEquipment(this Pawn pawn, ThingWithComps weapon)
	{
		if (pawn.HasEquipmentTracker())
		{
			pawn.equipment.DestroyEquipment(weapon);
		}
	}

	internal static List<ThingWithComps> ListOfCopyWeapons(this Pawn pawn)
	{
		return pawn.HasAnyWeapon() ? pawn.equipment.AllEquipmentListForReading.ListFullCopy() : null;
	}

	internal static void PasteCopyWeapons(this Pawn pawn, List<ThingWithComps> l)
	{
		if (pawn.HasEquipmentTracker())
		{
			pawn.DestroyAllEquipment();
			if (!l.NullOrEmpty())
			{
				bool firstWeapon = true;
				foreach (ThingWithComps item in l)
				{
					ThingWithComps w = GenerateWeapon(Selected.ByThing(item));
					pawn.AddWeaponToEquipment(w, firstWeapon);
					firstWeapon = false;
				}
			}
		}
		CEditor.API.UpdateGraphics();
	}

	internal static void CreateAndWearEquipment(this Pawn pawn, Selected s, bool firstWeapon)
	{
		if (pawn.HasEquipmentTracker())
		{
			ThingWithComps w = GenerateWeapon(s);
			pawn.AddWeaponToEquipment(w, firstWeapon);
		}
	}

	internal static void AddWeaponToEquipment(this Pawn pawn, ThingWithComps w, bool firstWeapon, bool destroyOld = true)
	{
		if (!pawn.HasEquipmentTracker() || w == null)
		{
			return;
		}
		if (firstWeapon)
		{
			if (pawn.equipment.Primary != null)
			{
				if (destroyOld)
				{
					pawn.equipment.Primary.Destroy();
				}
				else
				{
					ThingWithComps primary = pawn.equipment.Primary;
					pawn.equipment.Remove(primary);
					pawn.AddItemToInventory(primary);
				}
			}
			pawn.equipment.AddEquipment(w);
		}
		else if (CEditor.IsDualWieldActive)
		{
			try
			{
				Type aType = Reflect.GetAType("DualWield", "Ext_Pawn_EquipmentTracker");
				aType.CallMethod("MakeRoomForOffHand", new object[2] { pawn.equipment, w });
				aType.CallMethod("AddOffHandEquipment", new object[2] { pawn.equipment, w });
			}
			catch
			{
			}
		}
	}

	internal static ThingWithComps GenerateRandomWeapon(bool originalColors = true)
	{
		WeaponType weaponType = ((CEditor.zufallswert.Next(0, 100) > 50) ? WeaponType.Ranged : WeaponType.Melee);
		HashSet<ThingDef> l = ListOfWeapons(null, weaponType);
		return GenerateWeapon(Selected.Random(l, originalColors));
	}

	internal static ThingWithComps GenerateWeapon(Selected s)
	{
		if (s == null || s.thingDef == null)
		{
			return null;
		}
		ThingDef thingDef = DefTool.ThingDef(s.thingDef.defName);
		if (thingDef == null || !thingDef.IsWeapon)
		{
			return null;
		}
		s.stuff = s.thingDef.ThisOrDefaultStuff(s.stuff);
		if (!s.thingDef.MadeFromStuff)
		{
			s.stuff = null;
		}
		ThingWithComps thingWithComps = (ThingWithComps)ThingMaker.MakeThing(s.thingDef, s.stuff);
		thingWithComps.HitPoints = thingWithComps.MaxHitPoints;
		thingWithComps.SetQuality(s.quality);
		thingWithComps.SetDrawColor(s.DrawColor);
		thingWithComps.stackCount = s.stackVal;
		if (s.style != null)
		{
			thingWithComps.StyleDef = s.style;
			thingWithComps.StyleDef.color = s.style.color;
		}
		return thingWithComps;
	}

	internal static bool IsTurret(this ThingDef t)
	{
		if (t == null)
		{
			return false;
		}
		return t.thingClass == typeof(Building_TurretGun) || (t.thingClass != null && CEditor.IsCombatExtendedActive && t.thingClass.ToString().Contains("Building_TurretGun")) || (t.building != null && t.building.turretGunDef != null);
	}

	internal static bool IsTurretGun(this ThingDef t)
	{
		return t != null && t.weaponTags != null && t.weaponTags.Contains("TurretGun");
	}

	internal static bool IsBullet(this ThingDef t)
	{
		return t != null && t.projectile != null;
	}

	internal static void PasteWeaponTag(this ThingDef weapon, List<string> l)
	{
		if (weapon == null || l.NullOrEmpty())
		{
			return;
		}
		if (weapon.weaponTags == null)
		{
			weapon.weaponTags = new List<string>();
		}
		foreach (string item in l)
		{
			weapon.AddWeaponTag(item);
		}
	}

	internal static bool HasWeaponTag(this ThingDef weapon, string tag)
	{
		return weapon.HasAnyWeaponTags() && weapon.weaponTags.Contains(tag);
	}

	internal static void AddWeaponTag(this ThingDef t, string tag)
	{
		if (t != null && tag != null)
		{
			if (t.weaponTags == null)
			{
				t.weaponTags = new List<string>();
			}
			if (!t.HasWeaponTag(tag))
			{
				t.weaponTags.Add(tag);
			}
			t.ResolveReferences();
		}
	}

	internal static void RemoveWeaponTag(this ThingDef weapon, string tag)
	{
		if (!weapon.HasAnyWeaponTags())
		{
			return;
		}
		foreach (string weaponTag in weapon.weaponTags)
		{
			if (weaponTag == tag)
			{
				weapon.weaponTags.Remove(tag);
				break;
			}
		}
		weapon.ResolveReferences();
	}

	internal static void SetBullet(this ThingDef weapon, ThingDef bullet)
	{
		if (weapon.HasVerb())
		{
			weapon.Verbs[0].defaultProjectile = bullet;
			if (weapon.Verbs[0].defaultProjectile != null)
			{
				weapon.Verbs[0].defaultProjectile.ResolveReferences();
			}
		}
	}

	internal static string GetBulletDefName(this ThingDef weapon)
	{
		return (!weapon.HasProjectile()) ? "" : weapon.Verbs[0].defaultProjectile.defName;
	}

	internal static string GetSoundCastDefName(this ThingDef weapon)
	{
		return (!weapon.HasSoundcast()) ? "" : weapon.Verbs[0].soundCast.defName;
	}

	internal static float GetBulletExplosionRadius(this ThingDef weapon)
	{
		return (!weapon.HasProjectile()) ? 0f : weapon.Verbs[0].defaultProjectile.projectile.explosionRadius;
	}

	internal static float GetBulletStoppingPower(this ThingDef weapon)
	{
		return (!weapon.HasProjectile()) ? 0f : weapon.Verbs[0].defaultProjectile.projectile.stoppingPower;
	}

	internal static float GetBulletSpeed(this ThingDef weapon)
	{
		return (!weapon.HasProjectile()) ? 0f : weapon.Verbs[0].defaultProjectile.projectile.speed;
	}

	internal static void SetBulletSpeed(this ThingDef weapon, float speed)
	{
		if (weapon.HasProjectile())
		{
			weapon.Verbs[0].defaultProjectile.projectile.speed = speed;
		}
	}

	internal static void SetStoppingPower(this ThingDef weapon, float power)
	{
		if (weapon.HasProjectile())
		{
			weapon.Verbs[0].defaultProjectile.projectile.stoppingPower = power;
		}
	}

	internal static void SetExplosionRadius(this ThingDef weapon, float radius)
	{
		if (weapon.HasProjectile())
		{
			weapon.Verbs[0].defaultProjectile.projectile.explosionRadius = radius;
		}
	}

	internal static bool GetWeaponTargetGround(this ThingDef weapon)
	{
		return weapon.HasVerb() && weapon.Verbs[0].targetParams != null && weapon.Verbs[0].targetParams.canTargetLocations;
	}

	internal static int GetDmg(this ThingDef weapon)
	{
		if (weapon == null)
		{
			return 0;
		}
		if (weapon.IsMeleeWeapon)
		{
			float statValue = weapon.GetStatValue(StatDefOf.MeleeWeapon_DamageMultiplier);
			statValue = (weapon.HasStat(StatDefOf.MeleeWeapon_DamageMultiplier) ? statValue : 1f);
			return (weapon.tools != null) ? ((int)(weapon.tools[0].power * statValue)) : 0;
		}
		float statValue2 = weapon.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier);
		statValue2 = (weapon.HasStat(StatDefOf.RangedWeapon_DamageMultiplier) ? statValue2 : 1f);
		if (weapon.IsTurret())
		{
			return (weapon.building.turretGunDef.Verbs.Count > 0 && weapon.building.turretGunDef.Verbs[0].defaultProjectile != null) ? weapon.building.turretGunDef.Verbs[0].defaultProjectile.projectile.GetDamageAmount(statValue2, null) : 0;
		}
		return (weapon.Verbs.Count > 0 && weapon.Verbs[0].defaultProjectile != null && weapon.Verbs[0].defaultProjectile.projectile != null) ? weapon.Verbs[0].defaultProjectile.projectile.GetDamageAmount(statValue2, null) : 0;
	}

	internal static void SetTargetParams(this ThingDef weapon, bool canTargetGround)
	{
		if (weapon.HasVerb())
		{
			if (weapon.Verbs[0].targetParams == null)
			{
				weapon.Verbs[0].targetParams = new TargetingParameters();
			}
			weapon.Verbs[0].targetParams.canTargetLocations = canTargetGround;
		}
	}

	internal static ThingDef GetTurretDef(this ThingDef gun)
	{
		if (gun == null || gun.defName.NullOrEmpty())
		{
			return null;
		}
		Dictionary<string, ThingDef> dicGunAndTurret = PresetObject.DicGunAndTurret;
		if (dicGunAndTurret != null)
		{
			string text = dicGunAndTurret.KeyByValue(gun);
			if (!text.NullOrEmpty())
			{
				return DefTool.ThingDef(text);
			}
			if (dicGunAndTurret.ContainsKey(gun.defName))
			{
				return gun;
			}
			return null;
		}
		return null;
	}

	internal static ThingDef GetRealWeaponDef(this ThingDef gun)
	{
		if (gun == null || gun.defName.NullOrEmpty())
		{
			return null;
		}
		Dictionary<string, ThingDef> dicGunAndTurret = PresetObject.DicGunAndTurret;
		if (dicGunAndTurret != null)
		{
			string str = dicGunAndTurret.KeyByValue(gun);
			if (!str.NullOrEmpty())
			{
				return gun;
			}
			if (dicGunAndTurret.ContainsKey(gun.defName))
			{
				return dicGunAndTurret[gun.defName];
			}
			return gun;
		}
		return null;
	}

	internal static Selected SelectorForPawnSpecificWeapon(Pawn pawn)
	{
		try
		{
			string modname = pawn.kindDef.GetModName();
			WeaponType weaponType = (WeaponType)CEditor.zufallswert.Next(0, 2);
			HashSet<ThingDef> source = ListOfWeapons(null, weaponType);
			HashSet<ThingDef> hashSet = ListOfWeapons(modname, weaponType);
			HashSet<ThingDef> l = ((!hashSet.NullOrEmpty() && hashSet.Count >= 4) ? hashSet : source.Where((ThingDef td) => td.IsFromMod(modname) || (td.weaponTags != null && td.weaponTags.Select((string t) => pawn.kindDef.weaponTags.Contains(t)) != null)).ToHashSet());
			return Selected.Random(l, originalColors: true);
		}
		catch
		{
			return null;
		}
	}

	internal static bool IsPrimaryWeapon(this Pawn pawn, ThingWithComps w)
	{
		return w != null && w == CEditor.API.Pawn.equipment.Primary;
	}

	internal static void Reequip(this Pawn pawn, Selected selected, int primary = 0, bool pawnSpecific = false)
	{
		if (!pawn.HasEquipmentTracker())
		{
			return;
		}
		primary = ((primary < 0) ? CEditor.zufallswert.Next(0, (!CEditor.IsDualWieldActive) ? 1 : 3) : primary);
		int num = pawn.equipment.AllEquipmentListForReading.CountAllowNull();
		if (primary != 0 && num > 0)
		{
			for (int i = 0; i < pawn.equipment.AllEquipmentListForReading.CountAllowNull(); i++)
			{
				ThingWithComps thingWithComps = pawn.equipment.AllEquipmentListForReading[i];
				if (pawn.equipment.Primary == thingWithComps && (primary == 0 || primary >= 2))
				{
					pawn.equipment.DestroyEquipment(thingWithComps);
				}
				if (pawn.equipment.Primary != thingWithComps && (primary == 1 || primary >= 2))
				{
					pawn.equipment.DestroyEquipment(thingWithComps);
				}
			}
		}
		if (pawnSpecific && selected == null)
		{
			selected = SelectorForPawnSpecificWeapon(pawn);
		}
		ThingWithComps thingWithComps2 = null;
		ThingWithComps thingWithComps3 = null;
		int num2 = 3;
		do
		{
			thingWithComps2 = ((selected == null) ? GenerateRandomWeapon() : GenerateWeapon(selected));
			if (primary <= 0 || (primary == 1 && pawn.equipment.Primary.DestroyedOrNull()) || CEditor.InStartingScreen)
			{
				pawn.AddWeaponToEquipment(thingWithComps2, firstWeapon: true);
			}
			else
			{
				switch (primary)
				{
				case 1:
					pawn.AddWeaponToEquipment(thingWithComps2, firstWeapon: false);
					break;
				case 2:
					pawn.AddWeaponToEquipment(thingWithComps2, firstWeapon: true);
					thingWithComps3 = GenerateRandomWeapon();
					pawn.AddWeaponToEquipment(thingWithComps3, firstWeapon: false);
					break;
				}
			}
			num2--;
			primary = CEditor.zufallswert.Next(0, (!CEditor.IsDualWieldActive) ? 1 : 3);
		}
		while (!pawn.IsEquippedByPawn(thingWithComps2) && !pawn.IsEquippedByPawn(thingWithComps3) && num2 > 0);
		if (selected != null)
		{
			if (primary == 1 && pawn.equipment.Primary != null)
			{
				pawn.equipment.Primary.DrawColor = selected.DrawColor;
			}
			if (primary == 2 && thingWithComps3 != null)
			{
				thingWithComps3.DrawColor = selected.DrawColor;
			}
		}
	}

	internal static void SetCompRefuelable(ThingDef turretDef, ThingDef weapon, int value)
	{
		if (!CEditor.IsCombatExtendedActive && turretDef != null && turretDef.comps != null)
		{
			CompProperties compByType = turretDef.GetCompByType(typeof(CompProperties_Refuelable));
			if (compByType != null)
			{
				(compByType as CompProperties_Refuelable).fuelCapacity = value;
			}
		}
		else if (CEditor.IsCombatExtendedActive && weapon != null && weapon.comps != null)
		{
			weapon.GetCompByType("CombatExtended.CompProperties_AmmoUser")?.SetMemberValue("magazineSize", value);
		}
	}

	internal static void SetCompReloadTime(ThingDef turretDef, ThingDef weapon, float value)
	{
		if (CEditor.IsCombatExtendedActive && weapon != null && weapon.comps != null)
		{
			weapon.GetCompByType("CombatExtended.CompProperties_AmmoUser")?.SetMemberValue("reloadTime", value);
		}
	}

	internal static float GetCompReloadTime(ThingDef turretDef, ThingDef weapon)
	{
		if (CEditor.IsCombatExtendedActive && weapon != null && weapon.comps != null)
		{
			CompProperties compByType = weapon.GetCompByType("CombatExtended.CompProperties_AmmoUser");
			if (compByType != null)
			{
				return compByType.GetMemberValue("reloadTime", 0f);
			}
		}
		return 0f;
	}

	internal static CompProperties_Explosive GetCompExplosive(ThingDef t)
	{
		return (CompProperties_Explosive)t.GetCompByType(typeof(CompProperties_Explosive));
	}

	internal static int GetCompRefuelable(ThingDef turretDef, ThingDef weapon)
	{
		if (!CEditor.IsCombatExtendedActive && turretDef != null && turretDef.comps != null)
		{
			CompProperties compByType = turretDef.GetCompByType(typeof(CompProperties_Refuelable));
			if (compByType != null)
			{
				return (int)(compByType as CompProperties_Refuelable).fuelCapacity;
			}
		}
		else if (CEditor.IsCombatExtendedActive && weapon != null && weapon.comps != null)
		{
			CompProperties compByType2 = weapon.GetCompByType("CombatExtended.CompProperties_AmmoUser");
			if (compByType2 != null)
			{
				return compByType2.GetMemberValue("magazineSize", 0);
			}
		}
		return 0;
	}

	internal static string GetNameForWeaponType(WeaponType type)
	{
		return type switch
		{
			WeaponType.Melee => Label.MELEE, 
			WeaponType.Ranged => Label.RANGED, 
			WeaponType.Turret => Label.TURRET, 
			WeaponType.TurretGun => Label.TURRETGUN, 
			_ => "", 
		};
	}

	internal static List<ThingDef> ListOfTurrets()
	{
		return (from td in DefDatabase<ThingDef>.AllDefs
			where !td.defName.NullOrEmpty() && !td.label.NullOrEmpty() && td.IsTurret()
			orderby td.label
			select td).ToList();
	}

	internal static HashSet<ThingDef> ListOfWeapons(string modname, WeaponType weaponType)
	{
		Dictionary<string, ThingDef> dicTurrets = PresetObject.DicGunAndTurret;
		bool bAll1 = modname.NullOrEmpty();
		return (from td in DefDatabase<ThingDef>.AllDefs
			where !td.label.NullOrEmpty() && ((td.IsMeleeWeapon && weaponType == WeaponType.Melee) || (td.IsRangedWeapon && weaponType == WeaponType.Ranged && !dicTurrets.Values.Contains(td)) || (td.IsTurret() && weaponType == WeaponType.Turret) || (td.IsRangedWeapon && weaponType == WeaponType.TurretGun && dicTurrets.Values.Contains(td))) && (bAll1 || td.IsFromMod(modname))
			orderby td.label
			select td).ToHashSet();
	}

	internal static HashSet<ThingDef> ListOfBullets(ThingCategoryDef tc, string modname)
	{
		bool bAll1 = modname.NullOrEmpty();
		bool bAll2 = tc == null;
		return (from td in DefDatabase<ThingDef>.AllDefs
			where td.IsBullet() && !td.label.NullOrEmpty() && (bAll1 || td.IsFromMod(modname)) && (bAll2 || (!td.thingCategories.NullOrEmpty() && td.thingCategories.Contains(tc)))
			orderby td.label
			select td).ToHashSet();
	}

	internal static List<SoundDef> ListOfSounds(bool all)
	{
		if (all)
		{
			List<SoundDef> list = (from td in DefDatabase<SoundDef>.AllDefs
				where !td.sustain
				orderby td.defName
				select td).ToList();
			list.RemoveDuplicates();
			return list;
		}
		List<SoundDef> list2 = (from td in DefDatabase<ThingDef>.AllDefs
			where td.IsWeapon && td.HasSoundcast()
			orderby td.Verbs[0].soundCast.defName
			select td.Verbs[0].soundCast).ToList();
		list2.RemoveDuplicates();
		return list2;
	}
}
