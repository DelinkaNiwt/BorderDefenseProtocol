using System.Collections.Generic;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// HediffDef扩展: 破裂检测配置
    ///
    /// 通过modExtensions挂在BDP_CombatBodyActive的HediffDef上，
    /// 由Hediff_CombatBodyActive通过def.GetModExtension读取。
    ///
    /// 配置参数:
    /// - criticalParts: 关键部位列表(BodyPartDef.defName)
    ///   这些部位被摧毁时触发战斗体破裂
    /// </summary>
    public class HediffExtension_RuptureConfig : DefModExtension
    {
        /// <summary>
        /// 关键部位列表(BodyPartDef.defName)
        /// 这些部位被摧毁时触发破裂
        /// 示例: Head, Brain, Heart, Neck, Torso
        /// </summary>
        public List<string> criticalParts = new List<string>();
    }
}
