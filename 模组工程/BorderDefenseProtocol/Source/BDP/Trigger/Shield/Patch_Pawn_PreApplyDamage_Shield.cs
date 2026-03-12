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
            // 伤害已被其他系统吸收，跳过
            if (absorbed) return;

            // 健康系统检查 + 快速退出：无hediff则必无护盾
            if (__instance?.health?.hediffSet == null) return;

            // 遍历所有hediff，查找护盾
            foreach (var hediff in __instance.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_BDPShield shieldHediff)
                {
                    var shieldComp = shieldHediff.TryGetComp<HediffComp_BDPShield>();
                    if (shieldComp == null) continue;

                    if (shieldComp.TryBlockDamage(ref dinfo))
                    {
                        absorbed = true;
                        return;
                    }
                }
            }
        }
    }
}
