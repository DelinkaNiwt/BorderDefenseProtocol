using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 引导飞行配置——标记类，挂在投射物ThingDef的modExtensions上。
    /// 存在即启用引导飞行模块（GuidedModule）。
    /// 实际引导参数（maxAnchors/anchorSpread）由Verb层的WeaponChipConfig管理。
    /// </summary>
    public class BDPGuidedConfig : DefModExtension
    {
        // 标记类，无字段。存在即表示该投射物支持引导飞行。
    }
}
