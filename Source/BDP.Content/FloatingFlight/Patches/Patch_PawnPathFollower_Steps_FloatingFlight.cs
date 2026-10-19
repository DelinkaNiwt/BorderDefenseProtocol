using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BDP.Content.FloatingFlight.Pathing;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.FloatingFlight.Patches
{
    /// <summary>
    /// 修正 Pawn_PathFollower 的逐格执行阶段。
    /// 只在四个原版 Walkable 调用点前插入浮空旁路，保留原版调用和全局格子规则。
    /// </summary>
    internal static class Patch_PawnPathFollower_Steps_FloatingFlight
    {
        /// <summary>
        /// 判断是否可以绕过当前方法中的原版通行失败分支。
        /// 这里只负责 BDP（边境防御协议）浮空旁路，非浮空状态必须回到原版 WalkableBy。
        /// </summary>
        private static bool CanBypassWalkableCheck(IntVec3 cell, Map map, Pawn pawn)
        {
            if (pawn == null || map == null || pawn.Map != map)
            {
                return false;
            }

            if (!FloatingFlightUtility.IsActuallyFlying(pawn) || cell.Roofed(map))
            {
                return false;
            }

            PathingContext context = FloatingFlightPathUtility.GetContext(
                map,
                FloatingFlightPathUtility.ResolvePathKind(pawn));
            return context?.pathGrid != null && context.pathGrid.Walkable(cell);
        }

        /// <summary>判断当前格是否属于露天实际飞行路径。</summary>
        private static bool IsOpenAirFlight(Pawn pawn, IntVec3 cell)
        {
            return pawn != null
                && pawn.Map != null
                && FloatingFlightUtility.IsActuallyFlying(pawn)
                && cell.InBounds(pawn.Map)
                && !cell.Roofed(pawn.Map);
        }

        /// <summary>定位原版 GenGrid.WalkableBy 的精确签名。</summary>
        private static MethodInfo ResolveVanillaWalkableBy()
        {
            return AccessTools.Method(
                typeof(GenGrid),
                "Walkable" + "By",
                new[] { typeof(IntVec3), typeof(Map), typeof(Pawn) });
        }

        /// <summary>判断通行结果后的原版成功分支。</summary>
        private static bool IsTrueBranch(CodeInstruction instruction)
        {
            return instruction != null
                && (instruction.opcode == OpCodes.Brtrue || instruction.opcode == OpCodes.Brtrue_S)
                && instruction.operand is Label;
        }

        /// <summary>插入旁路前保存原版通行调用的三个参数。</summary>
        private static List<CodeInstruction> BuildFlightBypass(
            ILGenerator generator,
            CodeInstruction walkabilityCall,
            Label successLabel,
            MethodInfo bypassMethod)
        {
            LocalBuilder pawnLocal = generator.DeclareLocal(typeof(Pawn));
            LocalBuilder mapLocal = generator.DeclareLocal(typeof(Map));
            LocalBuilder cellLocal = generator.DeclareLocal(typeof(IntVec3));

            List<CodeInstruction> injected = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Stloc, pawnLocal),
                new CodeInstruction(OpCodes.Stloc, mapLocal),
                new CodeInstruction(OpCodes.Stloc, cellLocal),
                new CodeInstruction(OpCodes.Ldloc, cellLocal),
                new CodeInstruction(OpCodes.Ldloc, mapLocal),
                new CodeInstruction(OpCodes.Ldloc, pawnLocal),
                new CodeInstruction(OpCodes.Call, bypassMethod),
                new CodeInstruction(OpCodes.Brtrue, successLabel),
                new CodeInstruction(OpCodes.Ldloc, cellLocal),
                new CodeInstruction(OpCodes.Ldloc, mapLocal),
                new CodeInstruction(OpCodes.Ldloc, pawnLocal)
            };

            // 将原调用上的标签和异常块移到保存参数的第一条指令，避免入口分支跳过保存过程。
            injected[0].MoveLabelsFrom(walkabilityCall);
            injected[0].MoveBlocksFrom(walkabilityCall);
            return injected;
        }

        /// <summary>只识别原版精确通行调用；未知同签名函数不能证明可被飞行旁路跳过。</summary>
        private static int FindWalkabilityCall(
            List<CodeInstruction> codes,
            MethodInfo vanillaWalkableBy)
        {
            for (int index = 0; index < codes.Count - 1; index++)
            {
                MethodInfo called = codes[index].operand as MethodInfo;
                if (called == vanillaWalkableBy && IsTrueBranch(codes[index + 1]))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// 在原版逐格通行检查前插入浮空旁路。
        /// 原版 WalkableBy 调用本身保持不变，给其它 Harmony（补丁框架）补丁留下稳定锚点。
        /// </summary>
        private static IEnumerable<CodeInstruction> InsertFlightBypass(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase originalMethod)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo vanillaWalkableBy = ResolveVanillaWalkableBy();
            MethodInfo bypassMethod = AccessTools.Method(
                typeof(Patch_PawnPathFollower_Steps_FloatingFlight),
                nameof(CanBypassWalkableCheck));
            int index = FindWalkabilityCall(codes, vanillaWalkableBy);
            if (generator == null || vanillaWalkableBy == null || bypassMethod == null || index < 0)
            {
                Log.Warning(
                    $"BDP 浮空路径跟随兼容补丁未找到可安全包裹的通行调用：{originalMethod?.Name}，保留原始指令。");
                return codes;
            }

            CodeInstruction branch = codes[index + 1];
            Label successLabel = (Label)branch.operand;
            codes.InsertRange(
                index,
                BuildFlightBypass(generator, codes[index], successLabel, bypassMethod));

            return codes;
        }

        /// <summary>PawnCanOccupy（Pawn 是否能占据当前格）的窄范围补丁。</summary>
        [HarmonyPatch(typeof(Pawn_PathFollower), "PawnCanOccupy")]
        private static class PawnCanOccupyPatch
        {
            /// <summary>露天飞行时允许当前 Pawn 处于刚穿过的建筑或关闭门格。</summary>
            private static bool Prefix(
                Pawn ___pawn,
                IntVec3 c,
                ref bool __result)
            {
                if (!IsOpenAirFlight(___pawn, c))
                {
                    return true;
                }

                __result = true;
                return false;
            }

            /// <summary>在 PawnCanOccupy 的原版通行检查前插入浮空旁路。</summary>
            private static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> instructions,
                ILGenerator generator,
                MethodBase __originalMethod)
            {
                return InsertFlightBypass(instructions, generator, __originalMethod);
            }
        }

        /// <summary>IsNextCellWalkable（下一格通行检查）的窄范围补丁。</summary>
        [HarmonyPatch(typeof(Pawn_PathFollower), "IsNextCellWalkable")]
        private static class IsNextCellWalkablePatch
        {
            /// <summary>在 IsNextCellWalkable 的原版通行检查前插入浮空旁路。</summary>
            private static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> instructions,
                ILGenerator generator,
                MethodBase __originalMethod)
            {
                return InsertFlightBypass(instructions, generator, __originalMethod);
            }
        }

        /// <summary>SetupMoveIntoNextCell（准备进入下一格）的窄范围补丁。</summary>
        [HarmonyPatch(typeof(Pawn_PathFollower), "SetupMoveIntoNextCell")]
        private static class SetupMoveIntoNextCellPatch
        {
            /// <summary>在 SetupMoveIntoNextCell 的原版通行检查前插入浮空旁路。</summary>
            private static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> instructions,
                ILGenerator generator,
                MethodBase __originalMethod)
            {
                return InsertFlightBypass(instructions, generator, __originalMethod);
            }
        }

        /// <summary>NeedNewPath（是否需要重新寻路）的窄范围补丁。</summary>
        [HarmonyPatch(typeof(Pawn_PathFollower), "NeedNewPath")]
        private static class NeedNewPathPatch
        {
            /// <summary>在 NeedNewPath 的原版通行检查前插入浮空旁路。</summary>
            private static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> instructions,
                ILGenerator generator,
                MethodBase __originalMethod)
            {
                return InsertFlightBypass(instructions, generator, __originalMethod);
            }
        }

        /// <summary>浮空露天时不把建筑和门视为路径跟随阻挡物。</summary>
        [HarmonyPatch(typeof(Pawn_PathFollower), "BuildingBlockingNextPathCell")]
        private static class BuildingBlockingNextPathCellPatch
        {
            /// <summary>让露天浮空直接穿过建筑占用格。</summary>
            private static bool Prefix(
                Pawn ___pawn,
                IntVec3 ___nextCell,
                ref Building __result)
            {
                if (!IsOpenAirFlight(___pawn, ___nextCell))
                {
                    return true;
                }

                __result = null;
                return false;
            }
        }

        /// <summary>浮空露天时不等待或手动开启门。</summary>
        [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.NextCellDoorToWaitForOrManuallyOpen))]
        private static class NextCellDoorPatch
        {
            /// <summary>让露天浮空跳过门的等待与开门流程。</summary>
            private static bool Prefix(
                Pawn ___pawn,
                IntVec3 ___nextCell,
                ref Building_Door __result)
            {
                if (!IsOpenAirFlight(___pawn, ___nextCell))
                {
                    return true;
                }

                __result = null;
                return false;
            }
        }

        /// <summary>到达屋顶边界时先暂停路径并切换为地面状态。</summary>
        [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
        private static class TryEnterNextPathCellPatch
        {
            /// <summary>在进入屋顶格前请求降落，保留原 Job 和目的地。</summary>
            private static bool Prefix(
                Pawn ___pawn,
                IntVec3 ___nextCell)
            {
                if (___pawn == null
                    || ___pawn.Map == null
                    || !___nextCell.InBounds(___pawn.Map)
                    || !___nextCell.Roofed(___pawn.Map))
                {
                    if (___pawn != null
                        && FloatingFlightLedgerService.TryGetRecord(
                            ___pawn,
                            out FloatingFlightRecord existingRecord)
                        && existingRecord.GroundedForRoofSegment)
                    {
                        existingRecord.GroundedForRoofSegment = false;
                    }

                    return true;
                }

                bool actualFlying = FloatingFlightUtility.IsActuallyFlying(___pawn);
                FloatingFlightLedgerService.TryGetRecord(___pawn, out FloatingFlightRecord record);
                // 普通人物不能因进入屋顶创建业务记录；已移除 Hediff 的真实落地过渡仍需等待。
                if (!actualFlying && record?.GroundedForRoofSegment != true)
                {
                    return true;
                }

                // 始终浮空的屋顶节点只可能来自旧路径或上下文切换竞争；不进入屋顶，
                // 用原版 ResetToCurrentPosition 保留目标并重新请求当前浮空路径。
                if (FloatingFlightModeResolver.ResolveEffectiveMode(___pawn)
                    == FloatingFlightMode.Always
                    && actualFlying)
                {
                    ___pawn.pather?.ResetToCurrentPosition();
                    return false;
                }

                if (record?.GroundedForRoofSegment == true
                    && FloatingFlightStateController.GetPresentationFactor(
                        ___pawn,
                        record,
                        FloatingFlightStateController.CurrentTick) > 0.001f)
                {
                    return false;
                }

                if (record?.GroundedForRoofSegment == true)
                {
                    record.GroundedForRoofSegment = false;
                    return true;
                }

                if (!actualFlying)
                {
                    return true;
                }

                record = FloatingFlightLedgerService.GetOrCreateRecord(___pawn);
                if (record != null)
                {
                    record.GroundedForRoofSegment = true;
                }
                FloatingFlightStateController.RequestActualFlight(___pawn, record, false);

                return false;
            }
        }
    }
}
