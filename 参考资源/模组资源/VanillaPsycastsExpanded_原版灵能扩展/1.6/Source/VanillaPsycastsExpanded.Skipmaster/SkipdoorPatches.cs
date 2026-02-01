using System.Linq;
using HarmonyLib;
using RimWorld;
using VEF.Buildings;
using Verse;

namespace VanillaPsycastsExpanded.Skipmaster;

[HarmonyPatch]
public static class SkipdoorPatches
{
	[HarmonyPatch(typeof(Pawn), "Kill")]
	[HarmonyPrefix]
	public static void Pawn_Kill_Prefix(Pawn __instance)
	{
		Faction faction = __instance.Faction;
		if (faction == null || !faction.IsPlayer)
		{
			return;
		}
		foreach (Skipdoor item in WorldComponent_DoorTeleporterManager.Instance.DoorTeleporters.OfType<Skipdoor>().ToList())
		{
			if (item.Pawn == __instance)
			{
				GenExplosion.DoExplosion(((Thing)(object)item).Position, ((Thing)(object)item).Map, 4.9f, DamageDefOf.Bomb, (Thing)(object)item, 35);
				if (!((Thing)(object)item).Destroyed)
				{
					((Thing)(object)item).Destroy(DestroyMode.Vanish);
				}
			}
		}
	}
}
