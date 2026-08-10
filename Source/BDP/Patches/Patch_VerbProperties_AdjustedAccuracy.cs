using System;
using BDP.Core.Trigger;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// BDP 芯片 VerbProps 精度字段修复补丁。
    ///
    /// 问题：原版 VerbProperties.AdjustedAccuracy 是 private 方法，当 equipment != null
    /// 时完全不读 VerbProps 自身的 accuracyTouch/Short/Medium/Long 字段，而是向
    /// equipment 查询 stat（AccuracyTouch 等 StatDef）。BDP 的 equipment 是 TriggerBody
    /// （CompTriggerBody : CompEquippable），其 ThingDef 未定义精度 statBases →
    /// GetStatValue 返回 defaultBaseValue=1.0（100%）。芯片 XML 里定义的精度值被
    /// 完全绕过，导致所有距离命中率相同。
    ///
    /// 修复：当 equipment 是 BDP TriggerBody 时，跳过 equipment.GetStatValue 查询，
    /// 直接返回 VerbProps 自身的精度字段值（来自芯片 XML 反序列化）。
    ///
    /// 设计说明：选择 patch AdjustedAccuracy 而非其调用者 GetHitChanceFactor，是因为
    /// AdjustedAccuracy 只决定"精度数据从哪来"（装备 stat vs VerbProps 字段），不改动
    /// 距离分档插值算法。这样原版更新距离公式、其他模组对 GetHitChanceFactor 或
    /// ShotReport 的补丁都能继续正常生效——兼容性最优。
    /// </summary>
    [HarmonyPatch(typeof(VerbProperties), "AdjustedAccuracy")]
    public static class Patch_VerbProperties_AdjustedAccuracy
    {
        /// <summary>
        /// 在 BDP TriggerBody 作为装备时，用 VerbProps 自身精度字段替代装备 stat 查询。
        /// 非 BDP 装备或无装备时保持原版行为不变。
        /// </summary>
        /// <param name="__instance">芯片的 VerbProperties 实例，其 accuracyTouch/Short/Medium/Long 字段已从 XML 正确反序列化。</param>
        /// <param name="cat">当前距离段（贴身/近距/中距/远距），由上层 GetHitChanceFactor 根据距离分档传入。</param>
        /// <param name="equipment">Verb.EquipmentSource —— BDP 场景下为 TriggerBody。</param>
        /// <param name="__result">AdjustedAccuracy 的返回值，仅在补丁拦截时由本方法设置。</param>
        /// <returns>true 时继续执行原版逻辑（equipment.GetStatValue）；false 时跳过原版，直接使用 __result。</returns>
        private static bool Prefix(
            VerbProperties __instance,
            RangeCategory cat,
            Thing equipment,
            ref float __result)
        {
            // 无装备：走原版逻辑（原版也读 VerbProps 自身字段，不需要拦截）
            if (equipment == null)
            {
                return true;
            }

            // 非 BDP 装备（如原版枪械）：走原版逻辑，从装备 statBases 读精度
            CompTriggerBody triggerBody = equipment.TryGetComp<CompTriggerBody>();
            if (triggerBody == null)
            {
                return true;
            }

            // BDP TriggerBody 作为装备时：直接读 VerbProps 自身的精度字段
            // 这些字段在芯片 XML 反序列化时已正确赋值，只是原版 private 方法
            // 在有装备时主动跳过了它们。
            // 上层 GetHitChanceFactor 负责距离分档插值逻辑，这里只返回指定段的基础精度值。
            switch (cat)
            {
                case RangeCategory.Touch:
                    __result = __instance.accuracyTouch;
                    break;
                case RangeCategory.Short:
                    __result = __instance.accuracyShort;
                    break;
                case RangeCategory.Medium:
                    __result = __instance.accuracyMedium;
                    break;
                case RangeCategory.Long:
                    __result = __instance.accuracyLong;
                    break;
                default:
                    __result = 1f;
                    break;
            }
            return false;
        }
    }
}
