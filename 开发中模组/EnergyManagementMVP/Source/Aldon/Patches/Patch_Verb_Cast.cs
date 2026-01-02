using HarmonyLib;
using Verse;
using RimWorld;

namespace Aldon.Energy.Patches
{
    /// <summary>
    /// Patch: 在开枪完成后消耗能量
    /// 钩子点: Verb.TryCastNextBurstShot() 的 Postfix（开枪动作完成后）
    ///
    /// 设计说明：
    /// - 使用TryCastNextBurstShot而不是TryCastShot，因为前者是实际执行开枪的地方
    /// - 使用Postfix在开枪成功后再消耗能量
    /// - 如果能量不足，下次开枪时才会失败（不会阻止本次开枪）
    /// - 权限检查在TryConsume中进行
    ///
    /// 兼容性风险: 可能与其他修改Verb的模组冲突
    /// 版本依赖: RimWorld 1.6+ (Verb系统)
    /// </summary>
    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public static class Patch_Verb_TryCastNextBurstShot
    {
        /// <summary>
        /// 在Verb.TryCastNextBurstShot()执行完成后，尝试消耗能量
        /// </summary>
        static void Postfix(Verb __instance)
        {
            // 防守式编程：检查所有可能的null
            if (__instance == null) return;

            // 获取射手
            Pawn shooter = __instance.caster as Pawn;
            if (shooter == null) return;  // 不是Pawn就返回

            // 获取能量组件
            AldonEnergyComp energyComp = shooter.GetComp<AldonEnergyComp>();
            if (energyComp == null) return;  // 没有能量组件就返回

            // 尝试消耗能量（10点/发）
            // 如果消耗失败（权限不足或能量不足），TryConsume内部会显示消息
            bool consumed = energyComp.TryConsume(10);

            // 开发模式下输出详细日志
            if (Prefs.DevMode)
            {
                if (consumed)
                {
                    Log.Message($"[Aldon] {shooter.Name.ToStringShort} 开枪消耗能量成功: -10点");
                }
                else
                {
                    Log.Message($"[Aldon] {shooter.Name.ToStringShort} 能量消耗失败（权限或能量不足）");
                }
            }
        }
    }
}
