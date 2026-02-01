using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace NiceInventoryTab;

public class VanillaExpandedFrameworkIntegration
{
	public static Assembly VEFAss;

	public static StatDef VEF_VerbRangeFactor;

	public static void PostPatch()
	{
		VEFAss = ModIntegration.TryGetExternalAssembly("oskarpotocki.vanillafactionsexpanded.core", "VEF");
		if (VEFAss == null)
		{
			ModIntegration.VEFActive = false;
			return;
		}
		VEF_VerbRangeFactor = DefDatabase<StatDef>.AllDefs.FirstOrDefault((StatDef x) => x.defName == "VEF_VerbRangeFactor");
	}

	public static bool? UsableWithShields(ThingDef weap)
	{
		if (ModIntegration.VEFActive && weap.IsRangedWeapon)
		{
			return (bool)Utils.InvokeMethod(VEFAss, "VEF.Apparels.ShieldUtility", "UsableWithShields", null, new object[1] { weap }, BindingFlags.Static | BindingFlags.Public);
		}
		return null;
	}
}
