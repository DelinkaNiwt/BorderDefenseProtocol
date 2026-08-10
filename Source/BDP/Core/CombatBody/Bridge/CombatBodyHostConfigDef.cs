using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体宿主通用配置 Def。
    /// 承载不随单个 Pawn/ThingDef 变化的战斗体全局玩法参数。
    /// </summary>
    public class CombatBodyHostConfigDef : Def
    {
        /// <summary>
        /// 战斗体激活后的每秒 Trion 维持消耗。
        /// </summary>
        public float maintenanceDrainPerSecond = 1f;

        /// <summary>
        /// 一次成功手动生成或解除后，禁止再次手动切换的游戏 tick 数。
        /// </summary>
        public int manualTransformLockTicks = 12;

        /// <summary>
        /// 战斗体前台衣物模式；缺少 XML 配置时安全回退为镜像原身。
        /// </summary>
        public CombatBodyFrontMode frontMode = CombatBodyFrontMode.MirrorOriginal;

        /// <summary>
        /// 预设模式使用的战斗体前台预设 Def 名称。
        /// </summary>
        public string frontPresetDefName = null;
    }

    /// <summary>
    /// 战斗体宿主配置解析器。
    /// 从 DefDatabase 读取配置，带安全 fallback。
    /// </summary>
    internal static class CombatBodyHostConfigResolver
    {
        /// <summary>
        /// 默认配置 Def 名称。
        /// </summary>
        private const string DefaultConfigDefName = "BDP_DefaultCombatBodyHostConfig";

        /// <summary>
        /// Def 未加载时使用的安全回退配置。
        /// </summary>
        private static readonly CombatBodyHostConfigDef fallback = new CombatBodyHostConfigDef();

        /// <summary>
        /// 读取当前战斗体宿主通用配置。
        /// </summary>
        internal static CombatBodyHostConfigDef Resolve()
        {
            return DefDatabase<CombatBodyHostConfigDef>.GetNamedSilentFail(DefaultConfigDefName) ?? fallback;
        }
    }
}
