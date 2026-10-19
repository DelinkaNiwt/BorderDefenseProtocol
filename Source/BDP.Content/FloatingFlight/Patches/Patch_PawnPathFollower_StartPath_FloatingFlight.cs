using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BDP.Content.FloatingFlight.Pathing;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Patches
{
    /// <summary>修正 Pawn_PathFollower.StartPath 中不带 Pawn 的原版预检。</summary>
    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath))]
    internal static class Patch_PawnPathFollower_StartPath_FloatingFlight
    {
        /// <summary>给原版 StartPath 的窄调用点补回当前路径 Pawn。</summary>
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo original = AccessTools.Method(
                typeof(Reachability),
                nameof(Reachability.CanReach),
                new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) });
            MethodInfo replacement = AccessTools.Method(
                typeof(Patch_PawnPathFollower_StartPath_FloatingFlight),
                nameof(CanReachForStartPath));
            FieldInfo pawnField = AccessTools.Field(typeof(Pawn_PathFollower), "pawn");
            int matches = 0;

            for (int index = 0; index < codes.Count; index++)
            {
                if (codes[index].opcode != OpCodes.Callvirt || !Equals(codes[index].operand, original))
                {
                    continue;
                }

                codes.Insert(index, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(index + 1, new CodeInstruction(OpCodes.Ldfld, pawnField));
                codes[index + 2].opcode = OpCodes.Call;
                codes[index + 2].operand = replacement;
                matches++;
                index += 2;
            }

            if (matches != 1)
            {
                Log.Error($"BDP 浮空 StartPath 补丁未找到唯一 Reachability.CanReach 调用：{__originalMethod?.Name}，实际找到 {matches} 个。");
                return instructions;
            }

            return codes;
        }

        /// <summary>替代 StartPath 内部的无 Pawn Reachability 调用。</summary>
        private static bool CanReachForStartPath(
            Reachability reachability,
            IntVec3 start,
            LocalTargetInfo dest,
            PathEndMode peMode,
            TraverseParms original,
            Pawn pawn)
        {
            if (pawn != null && FloatingFlightPathUtility.ResolvePathKind(pawn) != FloatingFlightPathKind.Vanilla)
            {
                TraverseParms withPawn = original;
                withPawn.pawn = pawn;
                return FloatingFlightReachability.CanReach(pawn, start, dest, peMode, withPawn);
            }

            return reachability.CanReach(start, dest, peMode, original);
        }
    }
}
