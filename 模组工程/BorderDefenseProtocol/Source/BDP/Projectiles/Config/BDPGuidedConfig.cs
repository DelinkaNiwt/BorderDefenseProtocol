using Verse;

namespace BDP.Projectiles.Config
{
    /// <summary>
    /// 引导飞行配置——标记类，挂在投射物ThingDef的modExtensions上。
    /// 存在即启用引导飞行模块（GuidedModule）。
    /// 实际引导参数（maxAnchors/anchorSpread）由Verb层的WeaponChipConfig管理。
    /// </summary>
    public class BDPGuidedConfig : DefModExtension
    {
        // 标记类，存在即表示该投射物支持引导飞行。

        /// <summary>自动绕行最大障碍层数（迭代深度）。1=退化为单墙行为。</summary>
        public int maxRouteDepth = 3;

        /// <summary>自动绕行每堵墙的锚点数上限（1~5）。
        /// 1=取最远单点，3=均匀3段，5=保持旧行为。</summary>
        public int anchorsPerWall = 3;
    }
}
