using System.Collections.Generic;
using Verse;

namespace BDP.Core
{
    /// <summary>
    /// BorderDefenseProtocol模组的通用DefModExtension。
    /// 可用于任何Def类型（NeedDef、HediffDef、ThingDef等）的扩展配置。
    /// 根据实际使用的Def类型，只需配置相关字段即可，未使用的字段可以留空。
    /// </summary>
    public class BDPDefExtension : DefModExtension
    {
        // ── Need相关配置（预留） ──

        /// <summary>
        /// 阈值百分比列表。
        /// 原用于Need条上显示标记线，Need_Trion移除后暂无使用者。
        /// 保留字段供未来Gizmo或其他系统复用。
        /// </summary>
        public List<float> thresholdPercents;

        // ── Hediff相关配置 ──
        // 未来如果需要扩展Hediff，可以在这里添加字段
        // 例如：public float customSeverityMultiplier;

        // ── Thing相关配置 ──
        // 未来如果需要扩展Thing，可以在这里添加字段
        // 例如：public float trionStorageCapacity;

        // ── 通用配置 ──
        // 可以添加跨Def类型的通用配置
        // 例如：public bool enableDebugLogging;
    }
}
