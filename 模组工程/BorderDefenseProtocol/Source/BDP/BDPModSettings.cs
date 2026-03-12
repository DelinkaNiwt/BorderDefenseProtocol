using Verse;

namespace BDP
{
    /// <summary>
    /// 战斗体激活时的换装模式。
    /// </summary>
    public enum CombatApparelMode
    {
        /// <summary>选项A：使用 XML 中预设的战斗体专属装备。</summary>
        Preset,
        /// <summary>选项B：按原身服装动态生成外观一致的副本，退出时销毁。</summary>
        MirrorOriginal
    }

    /// <summary>
    /// BDP模组的配置设置类。
    /// 用于存储和持久化玩家在mod选项界面中配置的各项参数。
    /// </summary>
    public class BDPModSettings : ModSettings
    {
        // ═══════════════════════════════════════════════════════════════
        // 配置项
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 是否启用调试日志。
        /// </summary>
        public bool enableDebugLogging = false;

        /// <summary>
        /// 战斗体激活时的换装模式。
        /// </summary>
        public CombatApparelMode combatApparelMode = CombatApparelMode.Preset;

        // ═══════════════════════════════════════════════════════════════
        // 数据持久化
        // ═══════════════════════════════════════════════════════════════

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableDebugLogging, "enableDebugLogging", false);
            Scribe_Values.Look(ref combatApparelMode, "combatApparelMode", CombatApparelMode.Preset);
        }
    }
}
