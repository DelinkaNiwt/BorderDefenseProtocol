namespace BDP.Trigger
{
    /// <summary>
    /// 芯片主类别（互斥，单一值）。
    /// 用于信息面板的顶层分类展示，与categories（多维度标签列表）区分。
    /// categories是多维度关键词标签（一个芯片可有多个），主类别是单一值。
    /// </summary>
    public enum ChipPrimaryCategory
    {
        /// <summary>未分类（默认值，兼容旧数据）。</summary>
        Unspecified = 0,

        /// <summary>远程武器类 - 远距离攻击芯片。</summary>
        RangedWeapon = 1,

        /// <summary>近战武器类 - 近身战斗芯片。</summary>
        MeleeWeapon = 2,

        /// <summary>防御类 - 护盾、防护等防御芯片。</summary>
        Defense = 3,

        /// <summary>能力类 - 授予特殊能力的芯片（如蚱蜢）。</summary>
        Ability = 4,

        /// <summary>被动类 - 被动触发的系统芯片（如紧急脱离）。</summary>
        Passive = 5

        // 预留扩展：未来可添加 Utility = 6, Combo = 7 等
    }
}
