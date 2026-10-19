using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.CombatBody.External
{
    /// <summary>
    /// 战斗体前台动态服装的一条中性外观记录。
    /// </summary>
    public sealed class CombatBodyFrontAppearanceEntry
    {
        /// <summary>
        /// 要创建的服装定义。
        /// </summary>
        public ThingDef ApparelDef;

        /// <summary>
        /// 服装创建时使用的实际材料定义。
        /// </summary>
        public ThingDef StuffDef;

        /// <summary>
        /// 当前服装的颜色值。
        /// </summary>
        public Color Color = Color.white;

        /// <summary>
        /// 当前服装是否有可用的染色组件和颜色。
        /// </summary>
        public bool HasColor;

        /// <summary>
        /// 当前服装使用的风格定义。
        /// </summary>
        public ThingStyleDef StyleDef;
    }
}
