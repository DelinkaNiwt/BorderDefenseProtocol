using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BDP.Core.CombatLog;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>只替换近战条目构造点；工厂内部仅允许BDP动作使用扩展条目。</summary>
    [HarmonyPatch(typeof(Verb_MeleeAttack), nameof(Verb_MeleeAttack.CreateCombatLog))]
    internal static class Patch_Verb_MeleeAttack_CreateCombatLog_BdpEntry
    {
        /// <summary>保留原版判空、添加、返回和攻击算法，严格匹配唯一构造点。</summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();
            var constructor = AccessTools.Constructor(typeof(BattleLogEntry_MeleeCombat), new[]
            {
                typeof(RulePackDef), typeof(bool), typeof(Pawn), typeof(Thing), typeof(ImplementOwnerTypeDef),
                typeof(string), typeof(ThingDef), typeof(HediffDef), typeof(LogEntryDef)
            });
            int count = code.Count(item => item.opcode == OpCodes.Newobj && Equals(item.operand, constructor));
            if (constructor == null || count != 1) throw new InvalidOperationException("BDP melee log constructor count: " + count);
            foreach (CodeInstruction instruction in code)
            {
                if (instruction.opcode != OpCodes.Newobj || !Equals(instruction.operand, constructor))
                {
                    yield return instruction;
                    continue;
                }
                CodeInstruction loadVerb = new CodeInstruction(OpCodes.Ldarg_0);
                loadVerb.labels.AddRange(instruction.labels);
                loadVerb.blocks.AddRange(instruction.blocks);
                yield return loadVerb;
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(BattleLogEntryFactoryAccess), nameof(BattleLogEntryFactoryAccess.CreateMelee)));
            }
        }
    }
}
