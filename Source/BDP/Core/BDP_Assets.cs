using UnityEngine;
using Verse;

namespace BDP.Core
{
    /// <summary>
    /// BDP模组的静态资源加载器。
    /// 集中管理所有纹理、音效等资源的加载。
    ///
    /// 注意：所有Unity资源（Texture2D等）必须在主线程加载，
    /// 因此需要StaticConstructorOnStartup特性。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class BDP_Assets
    {
        // ═══════════════════════════════════════════
        //  UI图标
        // ═══════════════════════════════════════════

        /// <summary>
        /// 战斗体激活状态下的流血图标。
        /// 用于健康面板UI，替代原版流血图标。
        /// </summary>
        public static readonly Texture2D CombatBodyBleedingIcon =
            ContentFinder<Texture2D>.Get("UI/Icons/Medical/CombatBodyBleeding");
    }
}
