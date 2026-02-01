using System.Reflection;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

[StaticConstructorOnStartup]
public static class NonUnoPinataIntegration
{
	public static Assembly NUPAss;

	public static MethodInfo StripCheckerGetCheckerMI;

	public static MethodInfo SetShouldStripMI;

	public static FieldInfo ShouldStripFI;

	public static Texture2D Strip_Thing;

	public static Texture2D Strip_Thing_Cancel;

	internal static bool CanStrip(Pawn pawn, Thing item)
	{
		if (item.Destroyed)
		{
			return false;
		}
		if (pawn.Corpse == null && !StrippableUtility.CanBeStrippedByColony(pawn))
		{
			return false;
		}
		return StripCheckerGetCheckerMI.Invoke(null, new object[2] { item, false }) != null;
	}

	internal static bool ShouldStrip(Thing item)
	{
		object obj = StripCheckerGetCheckerMI.Invoke(null, new object[2] { item, false });
		return (bool)ShouldStripFI.GetValue(obj);
	}

	internal static void DoPatch()
	{
		StripCheckerGetCheckerMI = NUPAss.GetType("NonUnoPinata.CompStripChecker").GetMethod("GetChecker", BindingFlags.Static | BindingFlags.Public);
		SetShouldStripMI = NUPAss.GetType("NonUnoPinata.NUPUtility").GetMethod("SetShouldStrip", BindingFlags.Static | BindingFlags.Public);
		ShouldStripFI = NUPAss.GetType("NonUnoPinata.CompStripChecker").GetField("ShouldStrip", BindingFlags.Instance | BindingFlags.Public);
		if (StripCheckerGetCheckerMI == null || ShouldStripFI == null || SetShouldStripMI == null)
		{
			Log.Warning(ModIntegration.ModLogPrefix + "NonUnoPinata method not found! Integration disabled.");
			ModIntegration.NUPActive = false;
		}
	}

	internal static void SetShouldStrip(bool v, Pawn pawn, Thing item)
	{
		Corpse corpse = pawn.Corpse;
		ThingWithComps thingWithComps = ((corpse == null) ? ((ThingWithComps)pawn) : ((ThingWithComps)corpse));
		object obj = StripCheckerGetCheckerMI.Invoke(null, new object[2] { item, false });
		SetShouldStripMI.Invoke(null, new object[4] { v, obj, pawn, thingWithComps });
	}

	internal static void CacheTex()
	{
		if (!(Strip_Thing_Cancel != null) || !(Strip_Thing != null))
		{
			Strip_Thing_Cancel = ContentFinder<Texture2D>.Get("UI/Icons/Strip_Thing_Cancel");
			Strip_Thing = ContentFinder<Texture2D>.Get("UI/Icons/Strip_Thing");
		}
	}
}
