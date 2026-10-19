using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 个人服装模板中的一条服装外观记录。
    /// </summary>
    public sealed class CombatBodyAppearanceTemplateEntry : IExposable
    {
        /// <summary>
        /// 服装本身的定义。
        /// </summary>
        public ThingDef ApparelDef;

        /// <summary>
        /// 服装实际使用的材料定义。
        /// </summary>
        public ThingDef StuffDef;

        /// <summary>
        /// 当前记录是否包含有效染色。
        /// </summary>
        public bool HasColor;

        /// <summary>
        /// 服装染色值。
        /// </summary>
        public Color Color = Color.white;

        /// <summary>
        /// 服装使用的风格定义。
        /// </summary>
        public ThingStyleDef StyleDef;

        /// <summary>
        /// 保存或读取服装外观记录。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Defs.Look(ref ApparelDef, "apparelDef");
            Scribe_Defs.Look(ref StuffDef, "stuffDef");
            Scribe_Defs.Look(ref StyleDef, "styleDef");
            Scribe_Values.Look(ref HasColor, "hasColor", false);
            Scribe_Values.Look(ref Color, "color", Color.white);
        }
    }
}
