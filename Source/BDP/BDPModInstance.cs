using UnityEngine;
using Verse;

namespace BDP
{
    /// <summary>
    /// BDP模组的主实例类。
    /// 继承自Verse.Mod，负责管理mod设置界面和配置数据。
    /// </summary>
    public class BDPModInstance : Mod
    {
        /// <summary>
        /// 模组设置数据的静态访问点。
        /// 其他代码可以通过 BDPModInstance.Settings 访问配置。
        /// </summary>
        public static BDPModSettings Settings { get; private set; }

        /// <summary>
        /// 构造函数，在mod加载时由RimWorld调用。
        /// </summary>
        /// <param name="content">mod的内容包信息</param>
        public BDPModInstance(ModContentPack content) : base(content)
        {
            // 获取或创建设置实例
            Settings = GetSettings<BDPModSettings>();
        }

        /// <summary>
        /// 绘制mod设置界面的内容。
        /// 当玩家在"选项 > Mod设置"中打开BDP的设置页面时调用。
        /// </summary>
        /// <param name="inRect">可用的绘制区域</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // ═══════════════════════════════════════════════════════════════
            // 标题
            // ═══════════════════════════════════════════════════════════════
            Text.Font = GameFont.Medium;
            listingStandard.Label("边境防卫协议 - 模组设置");
            Text.Font = GameFont.Small;
            listingStandard.Gap(12f);

            // ═══════════════════════════════════════════════════════════════
            // 战斗体装备模式
            // ═══════════════════════════════════════════════════════════════
            listingStandard.Label("战斗体装备模式");
            listingStandard.Gap(4f);
            if (listingStandard.RadioButton(
                "预设装备（默认）",
                Settings.combatApparelMode == CombatApparelMode.Preset,
                tooltip: "激活战斗体时穿上 XML 中预设的战斗体专属装备。"))
                Settings.combatApparelMode = CombatApparelMode.Preset;
            if (listingStandard.RadioButton(
                "镜像原身服装",
                Settings.combatApparelMode == CombatApparelMode.MirrorOriginal,
                tooltip: "激活战斗体时生成与原身服装外观一致的副本，退出时销毁。"))
                Settings.combatApparelMode = CombatApparelMode.MirrorOriginal;
            listingStandard.Gap(12f);

            // ═══════════════════════════════════════════════════════════════
            // 调试选项
            // ═══════════════════════════════════════════════════════════════
            listingStandard.CheckboxLabeled(
                "启用调试日志",
                ref Settings.enableDebugLogging,
                "开启后会在日志中输出更多调试信息，用于排查问题。"
            );
            listingStandard.Gap(4f);
            listingStandard.CheckboxLabeled(
                "启用枪口位置可视化（需开发者模式）",
                ref Settings.enableMuzzleDebugVisual,
                "射击时在地图上绘制枪口、武器、发射位置的彩色标记点，用于调试枪口偏移配置。"
            );
            listingStandard.Gap(12f);

            // ═══════════════════════════════════════════════════════════════
            // 说明文本
            // ═══════════════════════════════════════════════════════════════
            Text.Font = GameFont.Tiny;
            listingStandard.Label("提示：更多配置选项将在后续版本中添加。");
            Text.Font = GameFont.Small;
            listingStandard.Gap(12f);

            // ═══════════════════════════════════════════════════════════════
            // 重置按钮
            // ═══════════════════════════════════════════════════════════════
            if (listingStandard.ButtonText("重置为默认值"))
            {
                Settings.combatApparelMode = CombatApparelMode.Preset;
                Settings.enableDebugLogging = false;
                Settings.enableMuzzleDebugVisual = false;
            }

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// 返回mod设置的名称，显示在mod设置列表中。
        /// </summary>
        public override string SettingsCategory()
        {
            return "边境防卫协议";
        }
    }
}
