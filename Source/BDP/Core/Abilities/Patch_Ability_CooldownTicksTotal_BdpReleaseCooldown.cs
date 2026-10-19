using HarmonyLib;
using RimWorld;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// 为使用通用来源级冷却的 Ability 提供稳定的进度条总时长。
    /// 原版剩余时间仍由 Ability 自己计算，避免改变冷却结束时刻。
    /// </summary>
    [HarmonyPatch(typeof(Ability), nameof(Ability.CooldownTicksTotal), MethodType.Getter)]
    public static class Patch_Ability_CooldownTicksTotal_BdpReleaseCooldown
    {
        /// <summary>
        /// 用来源级账本保存的释放时总时长覆盖原版临时恢复时长。
        /// </summary>
        public static void Postfix(Ability __instance, ref int __result)
        {
            int durationTicks;
            if (AbilityReleaseCooldownService.TryGetCooldownDuration(__instance, out durationTicks)
                && durationTicks > 0)
            {
                __result = durationTicks;
            }
        }
    }
}
