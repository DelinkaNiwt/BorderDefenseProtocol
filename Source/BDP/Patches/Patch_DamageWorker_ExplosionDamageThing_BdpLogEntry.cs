using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BDP.Core.CombatLog;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>只替换爆炸条目构造点，BDP准入由工厂检查当前爆炸上下文。</summary>
    [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageThing")]
    internal static class Patch_DamageWorker_ExplosionDamageThing_BdpLogEntry
    {
        /// <summary>保留原版逐目标筛选和承伤链，只传入当前爆炸供准入判断。</summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();
            var constructor = AccessTools.Constructor(typeof(BattleLogEntry_ExplosionImpact), new[]
                { typeof(Thing), typeof(Thing), typeof(ThingDef), typeof(ThingDef), typeof(DamageDef) });
            int count = code.Count(item => item.opcode == OpCodes.Newobj && Equals(item.operand, constructor));
            if (constructor == null || count != 1) throw new InvalidOperationException("BDP explosion log constructor count: " + count);
            foreach (CodeInstruction instruction in code)
            {
                if (instruction.opcode != OpCodes.Newobj || !Equals(instruction.operand, constructor))
                {
                    yield return instruction;
                    continue;
                }
                CodeInstruction loadExplosion = new CodeInstruction(OpCodes.Ldarg_1);
                loadExplosion.labels.AddRange(instruction.labels);
                loadExplosion.blocks.AddRange(instruction.blocks);
                yield return loadExplosion;
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleLogEntryFactoryAccess),
                    nameof(BattleLogEntryFactoryAccess.CreateExplosionImpactFromVanilla)));
            }
        }
    }
}
