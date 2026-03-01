using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// FireMode Harmony Patch 集合（v9.1 根本修复）。
    ///
    /// 修复原因：
    ///   旧方案直接修改 ticksToImpact，但视觉位置公式
    ///   DistanceCoveredFraction = 1 - ticksToImpact / StartingTicksToImpact
    ///   中 StartingTicksToImpact 由 distance/SpeedTilesPerTick 实时计算，不受影响，
    ///   导致子弹视觉位置错乱。
    ///
    /// 新方案：
    ///   Patch A — Postfix Projectile.Launch：修改 destination 使 StartingTicksToImpact
    ///             自然变化，再通过 Bullet_BDP.ReinitFlight 重新初始化 ticksToImpact/lifetime。
    ///   Patch B — Postfix Projectile.get_DamageAmount：实时乘以伤害倍率，无需序列化。
    /// </summary>

    // ── Patch A：速度修正 ──────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Launch),
        new[] { typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo),
                typeof(LocalTargetInfo), typeof(ProjectileHitFlags),
                typeof(bool), typeof(Thing), typeof(ThingDef) })]
    public static class Patch_Projectile_Launch_FireModeSpeed
    {
        [HarmonyPostfix]
        public static void Postfix(Projectile __instance)
        {
            // 仅处理 Bullet_BDP（其他弹道跳过，零副作用）
            var bdp = __instance as Bullet_BDP;
            if (bdp == null) return;

            // equipment 是 protected 字段，用 Traverse 访问
            var equipment = Traverse.Create(__instance).Field("equipment").GetValue<Thing>();
            var fm = equipment?.TryGetComp<CompFireMode>();
            if (fm == null) return;

            bdp.DispatchSpeedModifiers(fm.Speed);
        }
    }

    // 鈹€鈹€ Patch A2锛氳StartingTicksToImpact鎰熺煡鍙戝皠閫熷害鍊嶇巼 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
    [HarmonyPatch(typeof(Projectile), "get_StartingTicksToImpact")]
    public static class Patch_Projectile_StartingTicksToImpact_FireModeSpeed
    {
        [HarmonyPostfix]
        public static void Postfix(Projectile __instance, ref float __result)
        {
            if (!(__instance is Bullet_BDP bdp)) return;

            float speedMult = bdp.LaunchSpeedMult;
            if (Mathf.Abs(speedMult - 1f) < 0.001f) return;
            if (speedMult <= 0.001f) speedMult = 0.001f;

            __result = Mathf.Max(0.001f, __result / speedMult);
        }
    }

    // ── Patch B：伤害修正 ──────────────────────────────────────────────────────
    [HarmonyPatch(typeof(Projectile), "get_DamageAmount")]
    public static class Patch_Projectile_DamageAmount_FireMode
    {
        [HarmonyPostfix]
        public static void Postfix(Projectile __instance, ref int __result)
        {
            // equipment 为 null 时直接跳过，零副作用
            var equipment = Traverse.Create(__instance).Field("equipment").GetValue<Thing>();
            var fm = equipment?.TryGetComp<CompFireMode>();
            if (fm == null || Mathf.Abs(fm.Damage - 1f) < 0.001f) return;
            __result = Mathf.Max(1, Mathf.RoundToInt(__result * fm.Damage));
        }
    }
}
