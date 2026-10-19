using System.Collections.Generic;
using BDP.Core.CombatBody;
using BDP.Core.Genes;
using BDP.Core.Trion;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Factions
{
    /// <summary>
    /// 只为近界宙域生成的人类 Pawn 补齐腺体、固定原型并通过正式入口激活战斗体。
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    internal static class Patch_PawnGenerator_GeneratePawn_NearRealmCombatBody
    {
        /// <summary>目标追兵派系 Def 名称；字符串判断避免其它派系受此业务逻辑影响。</summary>
        private const string NearRealmFactionDefName = "BDP_Faction_NearRealm";

        /// <summary>近界成员固定装备的原型触发体 Def 名称。</summary>
        private const string PrototypeTriggerBodyDefName = "BDP_TriggerBody_NeighborPrototype";

        /// <summary>PawnGenerator 返回近界成员后，在其抵达前准备有效战斗体。</summary>
        [HarmonyPostfix]
        private static void Postfix(Pawn __result)
        {
            if (__result?.Faction?.def?.defName != NearRealmFactionDefName)
            {
                return;
            }

            if (!TryInitializeCombatBody(__result))
            {
                Log.Error("BDP_NearRealm_CombatBodyInitializationFailed".Translate(__result.LabelShortCap));
            }
        }

        /// <summary>确保近界成员具备 Trion 腺体、固定原型触发体和正式激活状态。</summary>
        private static bool TryInitializeCombatBody(Pawn pawn)
        {
            if (pawn?.genes == null || pawn.equipment == null)
            {
                return false;
            }

            GeneDef glandDef = TrionGeneDefOf.BDP_Gene_TrionGland;
            if (glandDef == null)
            {
                return false;
            }

            if (pawn.genes.GetGene(glandDef) == null)
            {
                pawn.genes.AddGene(glandDef, false);
            }

            if (!TrionGlandEligibility.HasActiveTrionGland(pawn))
            {
                return false;
            }

            ThingDef triggerBodyDef = DefDatabase<ThingDef>.GetNamedSilentFail(PrototypeTriggerBodyDefName);
            if (triggerBodyDef == null)
            {
                return false;
            }

            if (pawn.equipment.Primary?.def != triggerBodyDef)
            {
                // 近界成员的已生成主武器只由 PawnKind 选择；统一替换为固定原型，避免激活错误装备。
                // PawnKind 的武器在腺体注入前生成，资格检查会把已生成的原型转入背包；优先取回，避免再造一件。
                ThingWithComps generatedTriggerBody = TakeGeneratedTriggerBodyFromInventory(pawn, triggerBodyDef);
                pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                pawn.equipment.AddEquipment(generatedTriggerBody ?? (ThingWithComps)ThingMaker.MakeThing(triggerBodyDef));
            }

            if (CombatBodySurfaceAccess.ResolveReader(pawn)?.Phase == CombatBodyPhase.Active)
            {
                return true;
            }

            CompTrion resource = pawn.GetComp<CompTrion>();
            ICombatBodyCommands commands = CombatBodySurfaceAccess.ResolveCommands(pawn);
            if (resource == null || commands == null)
            {
                return false;
            }

            // Pawn 尚未落地图；沿用皇室追兵的无地图资源初始化，再调用正式激活事务。
            resource.PostSpawnSetup(false);
            return commands.TryActivate()
                && CombatBodySurfaceAccess.ResolveReader(pawn)?.Phase == CombatBodyPhase.Active;
        }

        /// <summary>取回 PawnKind 已生成、因腺体尚未注入而转入背包的近界原型触发体。</summary>
        private static ThingWithComps TakeGeneratedTriggerBodyFromInventory(Pawn pawn, ThingDef triggerBodyDef)
        {
            ThingOwner inventory = pawn?.inventory?.innerContainer;
            if (inventory == null)
            {
                return null;
            }

            List<Thing> inventorySnapshot = new List<Thing>();
            foreach (Thing item in inventory)
            {
                inventorySnapshot.Add(item);
            }

            for (int index = 0; index < inventorySnapshot.Count; index++)
            {
                ThingWithComps triggerBody = inventorySnapshot[index] as ThingWithComps;
                if (triggerBody?.def == triggerBodyDef && inventory.Remove(triggerBody))
                {
                    return triggerBody;
                }
            }

            return null;
        }
    }
}
