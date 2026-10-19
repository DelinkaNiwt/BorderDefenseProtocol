using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Royalty
{
    /// <summary>
    /// 只放行皇室超凡内源基因的自然遗传，后续概率与组合检查仍由原版负责。
    /// 不改提取器、基因植入、整人复制或共享基因定义。
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_PregnancyUtility_RoyalBloodlineInheritance
    {
        /// <summary>唯一可获遗传例外的皇室基因标识，不放宽其他超凡基因。</summary>
        private const string RoyalBloodlineDefName = "BDP_Gene_RoyalBloodline";

        /// <summary>定位包含父母候选筛选的原版方法，避免命中其他同名重载。</summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(PregnancyUtility), nameof(PregnancyUtility.GetInheritedGenes),
                new[] { typeof(Pawn), typeof(Pawn), typeof(bool).MakeByRefType() });
        }

        /// <summary>
        /// 仅在遗传候选检查中，将皇室基因视为允许遗传；其他基因返回真实超凡值。
        /// 返回值只用于本方法的判断，绝不改写 biostatArc（超凡属性）。
        /// </summary>
        private static int GetArchiteValueForInheritance(GeneDef geneDef)
        {
            return geneDef.defName == RoyalBloodlineDefName ? 0 : geneDef.biostatArc;
        }

        /// <summary>替换父方和母方两处超凡读取，保留其余原版指令、标签与异常块。</summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            FieldInfo arcField = AccessTools.Field(typeof(GeneDef), nameof(GeneDef.biostatArc));
            MethodInfo replacement = AccessTools.Method(typeof(Patch_PregnancyUtility_RoyalBloodlineInheritance),
                nameof(GetArchiteValueForInheritance));
            List<int> matches = new List<int>();
            for (int index = 0; index < codes.Count; index++)
            {
                if (codes[index].LoadsField(arcField)) matches.Add(index);
            }

            // 两处必须完整匹配，防止版本或其他补丁变化后只让一方父母获得遗传例外。
            if (matches.Count != 2)
            {
                Log.Warning("BDP_RoyalBloodlineInheritancePatchMismatch".Translate(matches.Count));
                return codes;
            }

            foreach (int index in matches)
            {
                codes[index] = new CodeInstruction(codes[index])
                {
                    opcode = OpCodes.Call,
                    operand = replacement
                };
            }
            return codes;
        }
    }
}
