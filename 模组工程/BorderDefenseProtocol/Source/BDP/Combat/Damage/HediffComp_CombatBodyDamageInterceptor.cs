using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体伤害拦截器 Comp。
    ///
    /// 入口已迁移到 Patch_DamageWorker_FinalizeAndAddInjury（2026-03-04）。
    /// 此类保留供后续扩展使用（避免 XML 引用报错）。
    ///
    /// 架构说明：
    /// - 原先通过 Harmony Postfix patch ThingWithComps.PreApplyDamage 调用
    /// - 现已改为直接 Prefix patch DamageWorker_AddInjury.FinalizeAndAddInjury
    /// - 新拦截点时机更晚，可直接读取原版计算结果（部位、护甲后伤害）
    /// </summary>
    public class HediffComp_CombatBodyDamageInterceptor : HediffComp
    {
        // 保留空类，供后续扩展
    }
}
