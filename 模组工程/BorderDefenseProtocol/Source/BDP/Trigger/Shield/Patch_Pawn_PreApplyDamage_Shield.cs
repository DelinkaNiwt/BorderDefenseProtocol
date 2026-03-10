using HarmonyLib;
using Verse;

namespace BDP.Trigger.Shield
{
    /// <summary>
    /// Postfix patch on Pawn.PreApplyDamage
    ///
    /// 用途: 护盾系统伤害拦截
    ///
    /// 实现说明:
    /// - 在PreApplyDamage执行之后检查护盾
    /// - 如果护盾成功抵挡，修改absorbed参数为true
    /// - 这样可以让原方法正常执行（避免out参数问题），但标记伤害已被吸收
    ///
    /// 注意：
    /// - 不能使用Prefix返回false，因为会导致out参数无法传递
    /// - 必须使用Postfix修改absorbed参数
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
    public static class Patch_Pawn_PreApplyDamage_Shield
    {
        /// <summary>
        /// Postfix: 在原版PreApplyDamage之后执行
        /// 优先级: Normal（默认）
        /// </summary>
        public static void Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            // 如果伤害已被其他系统吸收，跳过
            if (absorbed)
            {
                Log.Message($"[BDP-Shield-Postfix] {__instance.LabelShort} 伤害已被其他系统吸收");
                return;
            }

            // 调试日志：记录进入Postfix
            Log.Message($"[BDP-Shield-Postfix] {__instance.LabelShort} 受到伤害: " +
                       $"类型={dinfo.Def?.defName}, 伤害={dinfo.Amount:F1}, 角度={dinfo.Angle:F1}°");

            // 健康系统检查
            if (__instance.health?.hediffSet == null) return;

            // 遍历所有hediff，查找护盾
            foreach (var hediff in __instance.health.hediffSet.hediffs)
            {
                // 检查是否是护盾hediff
                if (hediff is Hediff_BDPShield shieldHediff)
                {
                    // 获取护盾组件
                    var shieldComp = shieldHediff.TryGetComp<HediffComp_BDPShield>();
                    if (shieldComp == null) continue;

                    // 尝试抵挡伤害
                    if (shieldComp.TryBlockDamage(ref dinfo))
                    {
                        absorbed = true;
                        Log.Message($"[BDP-Shield-Postfix] 护盾成功抵挡！标记absorbed=true");
                        return; // 伤害已被吸收，不再检查其他护盾
                    }
                }
            }

            // 没有护盾抵挡
            Log.Message($"[BDP-Shield-Postfix] 无护盾抵挡");
        }
    }
}
