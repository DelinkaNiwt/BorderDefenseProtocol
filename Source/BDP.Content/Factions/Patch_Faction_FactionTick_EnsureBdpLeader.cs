using HarmonyLib;
using RimWorld;

namespace BDP.Content.Factions
{
    /// <summary>只为近界宙域正式人类派系补齐旧存档中缺失的原版领袖。</summary>
    [HarmonyPatch(typeof(Faction), nameof(Faction.FactionTick))]
    internal static class Patch_Faction_FactionTick_EnsureBdpLeader
    {
        /// <summary>需要领袖的近界宙域派系标识。</summary>
        private const string NearRealmFactionDefName = "BDP_Faction_NearRealm";

        /// <summary>原版检查领袖前，使用该派系定义的固定领袖类型修复空领袖。</summary>
        [HarmonyPrefix]
        private static void Prefix(Faction __instance)
        {
            if (__instance == null || __instance.leader != null || __instance.IsPlayer
                || __instance.Hidden || __instance.temporary
                || __instance.def?.humanlikeFaction != true)
            {
                return;
            }

            string factionDefName = __instance.def.defName;
            if (factionDefName != NearRealmFactionDefName)
            {
                return;
            }

            __instance.TryGenerateNewLeader();
        }
    }
}
