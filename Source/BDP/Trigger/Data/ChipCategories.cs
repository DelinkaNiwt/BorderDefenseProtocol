namespace BDP.Trigger
{
    /// <summary>
    /// 芯片类别标签常量（推荐使用，但不强制）。
    /// 用于多维度分类芯片，支持筛选、分组、批量设置等功能。
    /// 一个芯片可以有多个类别标签。
    /// </summary>
    public static class ChipCategories
    {
        // ═══════════════════════════════════════════════════════
        // 功能维度 - 芯片的主要功能类型
        // ═══════════════════════════════════════════════════════

        /// <summary>武器类 - 用于攻击的芯片</summary>
        public const string Weapon = "Weapon";

        /// <summary>护盾类 - 提供防护的芯片</summary>
        public const string Shield = "Shield";

        /// <summary>能力类 - 授予特殊能力的芯片</summary>
        public const string Ability = "Ability";

        /// <summary>辅助类 - 提供辅助功能的芯片</summary>
        public const string Utility = "Utility";

        /// <summary>系统类 - 系统级功能芯片（如紧急脱离）</summary>
        public const string System = "System";

        // ═══════════════════════════════════════════════════════
        // 战斗方式维度 - 芯片的战斗方式
        // ═══════════════════════════════════════════════════════

        /// <summary>近战 - 近身战斗</summary>
        public const string Melee = "Melee";

        /// <summary>远程 - 远距离攻击</summary>
        public const string Ranged = "Ranged";

        // ═══════════════════════════════════════════════════════
        // 距离维度 - 有效作战距离
        // ═══════════════════════════════════════════════════════

        /// <summary>近距离 - 0-15格</summary>
        public const string Close = "Close";

        /// <summary>中距离 - 15-30格</summary>
        public const string Medium = "Medium";

        /// <summary>远距离 - 30+格</summary>
        public const string Long = "Long";

        // ═══════════════════════════════════════════════════════
        // 伤害/能量类型维度 - 伤害或能量的性质
        // ═══════════════════════════════════════════════════════

        /// <summary>物理伤害 - 实体物理攻击</summary>
        public const string Physical = "Physical";

        /// <summary>能量伤害 - 能量类攻击</summary>
        public const string Energy = "Energy";

        /// <summary>爆炸伤害 - 爆炸类攻击</summary>
        public const string Explosive = "Explosive";

        // ═══════════════════════════════════════════════════════
        // 武器细分维度 - 具体武器类型
        // ═══════════════════════════════════════════════════════

        /// <summary>刀刃类 - 切割武器</summary>
        public const string Blade = "Blade";

        /// <summary>钝器类 - 钝击武器</summary>
        public const string Blunt = "Blunt";

        /// <summary>步枪类 - 常规步枪</summary>
        public const string Rifle = "Rifle";

        /// <summary>狙击类 - 狙击武器</summary>
        public const string Sniper = "Sniper";

        /// <summary>霰弹类 - 霰弹枪</summary>
        public const string Shotgun = "Shotgun";

        /// <summary>发射器类 - 投射武器</summary>
        public const string Launcher = "Launcher";

        /// <summary>方块类 - 生成并切分能量方块发射</summary>
        public const string Cube = "Cube";

        // ═══════════════════════════════════════════════════════
        // 特殊机制维度 - 芯片的特殊机制
        // ═══════════════════════════════════════════════════════

        /// <summary>追踪类 - 具有追踪能力</summary>
        public const string Tracking = "Tracking";

        /// <summary>引导类 - 具有引导飞行能力</summary>
        public const string Guided = "Guided";

        /// <summary>齐射类 - 支持齐射模式</summary>
        public const string Volley = "Volley";

        /// <summary>连射类 - 支持连续射击</summary>
        public const string Burst = "Burst";

        /// <summary>穿透类 - 具有穿透能力</summary>
        public const string Passthrough = "Passthrough";

        // ═══════════════════════════════════════════════════════
        // 消耗特性维度 - 芯片的消耗特征
        // ═══════════════════════════════════════════════════════

        /// <summary>高消耗 - 需要大量Trion</summary>
        public const string HighCost = "HighCost";

        /// <summary>持续消耗 - 激活期间持续消耗Trion</summary>
        public const string Continuous = "Continuous";

        /// <summary>被动触发 - 被动触发，无需主动激活</summary>
        public const string Passive = "Passive";
    }
}
