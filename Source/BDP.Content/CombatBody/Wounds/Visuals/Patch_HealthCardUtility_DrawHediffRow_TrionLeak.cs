using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.Wounds;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.CombatBody.Wounds.Visuals
{
    /// <summary>仅在健康页原血滴槽显示战斗体泄露；不改变生理流血或包扎状态图标。</summary>
    [HarmonyPatch(typeof(HealthCardUtility), "DrawHediffRow")]
    internal static class Patch_HealthCardUtility_DrawHediffRow_TrionLeak
    {
        /// <summary>原版视觉值为 0.6 时不收缩 20×20 槽位；此值不是资源消耗量。</summary>
        private const float FullSlotVisualRate = 0.6f;
        /// <summary>原版视觉值0.12对应12像素，确保低泄露仍可辨识。</summary>
        private const float MinimumVisualRate = 0.12f;
        /// <summary>组内每秒泄露30点时达到20像素上限。</summary>
        private const float FullSizeDrainPerSecond = 30f;

        /// <summary>由原版启动流程在主线程初始化静态贴图，避免资源首次请求时发生线程或时序不确定。</summary>
        [StaticConstructorOnStartup]
        private static class LeakTexture
        {
            /// <summary>用户选定的方颗粒泄露贴图，保留自带颜色及透明度。</summary>
            internal static readonly Texture2D Value;

            /// <summary>显式初始化在启动阶段加载贴图，普通人物仍继续使用原版图标。</summary>
            static LeakTexture()
            {
                Value = ContentFinder<Texture2D>.Get("UI/Icons/Health/TrionLeak");
            }
        }

        /// <summary>准入只覆盖战斗体伤口生命周期；其他人物及其他健康状态保留原版。</summary>
        private static bool IsCombatBodyWound(Hediff hediff)
        {
            if (!(hediff is Hediff_Injury) && !(hediff is Hediff_MissingPart)) return false;
            return CombatBodyMaterialAccess.Resolve(hediff.pawn)?.HasCombatBodyWoundRuntime == true;
        }

        /// <summary>泄露登记决定图标开关，不能读取被战斗体压为零的生理流血率。</summary>
        private static bool ShouldShowIcon(Hediff hediff)
        {
            if (!IsCombatBodyWound(hediff)) return hediff.Bleeding;
            CombatBodyWoundTrionInfoHediffComp info = hediff.TryGetComp<CombatBodyWoundTrionInfoHediffComp>();
            return info != null && info.TryGetPublishedDrainPerSecond(out float rate) && rate > 0f;
        }

        /// <summary>只选择当前伤口的局部贴图，不切换原版共享血滴资源。</summary>
        private static Texture2D ResolveIcon(Texture2D original, Hediff hediff)
        {
            return IsCombatBodyWound(hediff) ? LeakTexture.Value : original;
        }

        /// <summary>组内逐伤口合计有效泄露，线性映射到12～20像素；原版伤口仍累计血量。</summary>
        private static float AccumulateIconSize(float current, float rate, Hediff hediff)
        {
            if (!IsCombatBodyWound(hediff)) return current + rate;
            CombatBodyWoundTrionInfoHediffComp info = hediff.TryGetComp<CombatBodyWoundTrionInfoHediffComp>();
            if (info == null || !info.TryGetPublishedDrainPerSecond(out float drain)) return current;
            // 最小值只加一次。累计值上限0.6，禁止沿原版外推而把图标撑出槽位。
            return Mathf.Min(FullSlotVisualRate, Mathf.Max(current, MinimumVisualRate)
                + (FullSlotVisualRate - MinimumVisualRate) * drain / FullSizeDrainPerSecond);
        }

        /// <summary>
        /// 完整匹配显示开关、贴图与尺寸三个点后才替换，保留原版分组、排列和包扎槽。
        /// 伤口局部变量从实际读取指令提取，禁止写死编译器生成的变量编号。
        /// </summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            MethodInfo bleeding = AccessTools.PropertyGetter(typeof(Hediff), nameof(Hediff.Bleeding));
            MethodInfo rate = AccessTools.PropertyGetter(typeof(Hediff), nameof(Hediff.BleedRate));
            FieldInfo icon = AccessTools.Field(typeof(HealthCardUtility), "BleedingIcon");
            List<int> bleedingReads = new List<int>();
            List<int> rateReads = new List<int>();
            List<int> iconReads = new List<int>();
            for (int index = 0; index < codes.Count; index++)
            {
                if (codes[index].Calls(bleeding)) bleedingReads.Add(index);
                if (codes[index].Calls(rate)) rateReads.Add(index);
                if (codes[index].LoadsField(icon)) iconReads.Add(index);
            }

            // 三处任何一处失配都不部分应用，避免显示条件改变而贴图仍是血滴。
            if (bleedingReads.Count != 1 || rateReads.Count != 1 || iconReads.Count != 1)
                return Unavailable(codes);
            int bleedingIndex = bleedingReads[0];
            int rateIndex = rateReads[0];
            int iconIndex = iconReads[0];
            if (bleedingIndex == 0 || rateIndex == 0 || rateIndex + 1 >= codes.Count
                || !IsLocalLoad(codes[bleedingIndex - 1])
                || codes[rateIndex - 1].opcode != codes[bleedingIndex - 1].opcode
                || !Equals(codes[rateIndex - 1].operand, codes[bleedingIndex - 1].operand)
                || codes[rateIndex + 1].opcode != OpCodes.Add
                || !(bleedingIndex < iconIndex && iconIndex < rateIndex))
                return Unavailable(codes);

            CodeInstruction woundLoad = codes[bleedingIndex - 1];
            List<CodeInstruction> result = new List<CodeInstruction>();
            for (int index = 0; index < codes.Count; index++)
            {
                CodeInstruction code = codes[index];
                if (index == bleedingIndex)
                {
                    result.Add(new CodeInstruction(code) { opcode = OpCodes.Call,
                        operand = AccessTools.Method(typeof(Patch_HealthCardUtility_DrawHediffRow_TrionLeak), nameof(ShouldShowIcon)) });
                    continue;
                }
                if (index == rateIndex + 1)
                {
                    // 栈上的原累计与本伤口流血率保留，只把加法交给界面尺寸选择。
                    CodeInstruction load = new CodeInstruction(woundLoad.opcode, woundLoad.operand);
                    load.labels.AddRange(code.labels);
                    load.blocks.AddRange(code.blocks);
                    result.Add(load);
                    result.Add(new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Patch_HealthCardUtility_DrawHediffRow_TrionLeak), nameof(AccumulateIconSize))));
                    continue;
                }
                result.Add(code);
                if (index == iconIndex)
                {
                    result.Add(new CodeInstruction(woundLoad.opcode, woundLoad.operand));
                    result.Add(new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Patch_HealthCardUtility_DrawHediffRow_TrionLeak), nameof(ResolveIcon))));
                }
            }
            return result;
        }

        /// <summary>只接受实际局部变量读取，不复制可能带副作用的取值操作。</summary>
        private static bool IsLocalLoad(CodeInstruction code)
        {
            return code.opcode == OpCodes.Ldloc || code.opcode == OpCodes.Ldloc_S
                || code.opcode == OpCodes.Ldloc_0 || code.opcode == OpCodes.Ldloc_1
                || code.opcode == OpCodes.Ldloc_2 || code.opcode == OpCodes.Ldloc_3;
        }

        /// <summary>版本或其他补丁改变接入位置时保留原版，并给出可定位的中文告警。</summary>
        private static IEnumerable<CodeInstruction> Unavailable(List<CodeInstruction> codes)
        {
            Log.Warning("BDP_TrionLeakIconPatchUnavailable".Translate());
            return codes;
        }
    }
}
