using RimWorld;
using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// 内容层独立的 Trion 睡眠恢复配置，不附加到基础层属性。
    /// 两种睡眠倍率均由 XML 提供，Core 只接收计算结果。
    /// </summary>
    public sealed class TrionRecoverySettingsDef : Def
    {
        /// <summary>没有运行核心许可时，实际睡眠相对于基准恢复率的倍率。</summary>
        public float sleepWithoutCoreMultiplier;

        /// <summary>同时具有运行核心许可与实际睡眠时，相对于基准恢复率的倍率。</summary>
        public float sleepWithCoreMultiplier;
    }

    /// <summary>睡眠恢复配置的原版定义引用入口。</summary>
    [DefOf]
    public static class TrionRecoverySettingsDefOf
    {
        /// <summary>正式内容的睡眠恢复倍率配置。</summary>
        public static TrionRecoverySettingsDef BDP_TrionRecoverySettings;

        /// <summary>确保第一次读取前完成原版定义绑定。</summary>
        static TrionRecoverySettingsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TrionRecoverySettingsDefOf));
        }
    }
}
