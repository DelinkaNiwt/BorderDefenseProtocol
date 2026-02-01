using HarmonyLib;
using Verse;

namespace TowerLaserDefense.HarmonyPatches;

[StaticConstructorOnStartup]
public class StoreProjectiles
{
	[HarmonyPatch(typeof(ThingWithComps), "SpawnSetup")]
	private static class ProjectilesSpawn_PostFix
	{
		[HarmonyPostfix]
		private static void Postfix(ThingWithComps __instance)
		{
			if (__instance is Projectile item)
			{
				GameComponent_BulletsCache.BulletsCache.Add(item);
			}
		}
	}

	[HarmonyPatch(typeof(ThingWithComps), "DeSpawn")]
	private static class ProjectilesDeSpawn_PostFix
	{
		[HarmonyPostfix]
		private static void Postfix(ThingWithComps __instance)
		{
			if (__instance is Projectile item)
			{
				GameComponent_BulletsCache.BulletsCache.Remove(item);
			}
		}
	}
}
