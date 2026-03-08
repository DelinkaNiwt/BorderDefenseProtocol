using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BDP.Core;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Combat.Patches
{
    /// <summary>
    /// Patch HealthCardUtility.DrawHediffRow 以自定义战斗体激活时的流血图标。
    ///
    /// 原版逻辑：
    /// - 第651行：if (item3.Bleeding) bleedingIcon = BleedingIcon;
    /// - 使用静态字段 BleedingIcon（红色水滴）
    ///
    /// Patch效果：
    /// - 当Pawn有BDP_CombatBodyActive时，使用自定义图标
    /// - 其他情况使用原版图标
    /// </summary>
    [HarmonyPatch(typeof(HealthCardUtility), "DrawHediffRow")]
    public static class Patch_HealthCardUtility_DrawHediffRow
    {
        // 存储当前正在渲染的Pawn（用于Transpiler中判断）
        private static Pawn currentPawn;

        /// <summary>
        /// Prefix：捕获当前渲染的Pawn。
        /// </summary>
        [HarmonyPrefix]
        static void Prefix(Pawn pawn)
        {
            currentPawn = pawn;
        }

        /// <summary>
        /// Transpiler：替换 BleedingIcon 静态字段的读取为方法调用。
        ///
        /// 原指令：ldsfld Texture2D HealthCardUtility::BleedingIcon
        /// 替换为：call Texture2D GetBleedingIcon()
        /// </summary>
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var bleedingIconField = AccessTools.Field(typeof(HealthCardUtility), "BleedingIcon");
            var getIconMethod = AccessTools.Method(typeof(Patch_HealthCardUtility_DrawHediffRow),
                nameof(GetBleedingIcon));

            int patchCount = 0;

            for (int i = 0; i < codes.Count; i++)
            {
                // 查找 ldsfld BleedingIcon 指令
                if (codes[i].LoadsField(bleedingIconField))
                {
                    // 替换为调用我们的方法
                    codes[i] = new CodeInstruction(OpCodes.Call, getIconMethod);
                    patchCount++;
                }
            }

            if (patchCount == 0)
            {
                Log.Warning("[BDP] Patch_HealthCardUtility_DrawHediffRow: 未找到BleedingIcon字段读取，Patch可能失效");
            }
            else
            {
                Log.Message($"[BDP] Patch_HealthCardUtility_DrawHediffRow: 成功替换 {patchCount} 处BleedingIcon读取");
            }

            return codes;
        }

        /// <summary>
        /// 根据Pawn状态返回对应的流血图标。
        /// 战斗体激活时返回自定义图标，否则返回原版图标。
        /// </summary>
        static Texture2D GetBleedingIcon()
        {
            // 判断当前Pawn是否有战斗体激活
            if (currentPawn?.health?.hediffSet?.HasHediff(BDP_DefOf.BDP_CombatBodyActive) == true)
            {
                return BDP.Core.BDP_Assets.CombatBodyBleedingIcon;
            }

            // 返回原版图标（通过反射读取静态字段）
            var field = typeof(HealthCardUtility).GetField("BleedingIcon",
                BindingFlags.NonPublic | BindingFlags.Static);
            return (Texture2D)field?.GetValue(null);
        }
    }
}
