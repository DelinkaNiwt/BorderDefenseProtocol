namespace BDP.Trigger
{
    /// <summary>
    /// 触发体Info Card显示配置。
    /// 控制哪些stat条目要显示，哪些要隐藏。
    /// 修改这里的true/false即可配置显示内容，无需改其他代码。
    /// </summary>
    public static class TriggerBodyDisplayConfig
    {
        // ═══════════════════════════════════════════
        //  基础信息区块
        // ═══════════════════════════════════════════

        /// <summary>显示"当前配置"条目（左手: XX / 右手: XX）</summary>
        public static bool ShowCurrentConfig = true;

        /// <summary>显示"装载芯片"条目（槽位列表）</summary>
        public static bool ShowLoadedChips = true;

        // ═══════════════════════════════════════════
        //  武器参数区块（合并左右手）
        // ═══════════════════════════════════════════

        /// <summary>显示伤害（左手: XX / 右手: XX）</summary>
        public static bool ShowDamage = true;

        /// <summary>显示射程</summary>
        public static bool ShowRange = true;

        /// <summary>显示预热时间</summary>
        public static bool ShowWarmup = true;

        /// <summary>显示子弹飞行速度</summary>
        public static bool ShowProjectileSpeed = true;

        /// <summary>显示连射次数</summary>
        public static bool ShowBurstCount = true;

        /// <summary>显示连射速率（rpm）</summary>
        public static bool ShowBurstRate = true;

        /// <summary>显示Trion消耗/发</summary>
        public static bool ShowTrionCost = true;

        /// <summary>显示齐射散布</summary>
        public static bool ShowVolleySpread = true;

        /// <summary>显示变化弹支持</summary>
        public static bool ShowGuidedSupport = true;

        /// <summary>显示穿透力</summary>
        public static bool ShowPassthroughPower = true;

        /// <summary>显示冷却时间</summary>
        public static bool ShowCooldown = true;

        /// <summary>显示护甲穿透率</summary>
        public static bool ShowArmorPenetration = true;

        /// <summary>显示抑止能力</summary>
        public static bool ShowStoppingPower = true;

        /// <summary>显示精度（近/短/中/远）</summary>
        public static bool ShowAccuracy = true;

        /// <summary>显示近战Tool</summary>
        public static bool ShowMeleeTools = true;

        // ═══════════════════════════════════════════
        //  非武器芯片
        // ═══════════════════════════════════════════

        /// <summary>显示Hediff芯片效果</summary>
        public static bool ShowHediffChips = true;

        /// <summary>显示Ability芯片技能</summary>
        public static bool ShowAbilityChips = true;
    }
}
