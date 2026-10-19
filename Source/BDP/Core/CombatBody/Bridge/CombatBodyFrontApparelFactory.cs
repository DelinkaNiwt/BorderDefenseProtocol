using BDP.Core.CombatBody.External;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体前台服装创建工厂。
    /// 统一镜像、动态预设和 Content 预览所需的外观复制逻辑。
    /// </summary>
    public static class CombatBodyFrontApparelFactory
    {
        /// <summary>
        /// 按动态外观条目创建一件独立服装副本。
        /// </summary>
        public static Apparel Create(CombatBodyFrontAppearanceEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            return Create(
                entry.ApparelDef,
                entry.StuffDef,
                entry.HasColor,
                entry.Color,
                entry.StyleDef);
        }

        /// <summary>
        /// 按定义、材料、颜色和风格创建一件独立服装副本。
        /// </summary>
        public static Apparel Create(
            ThingDef apparelDef,
            ThingDef stuffDef,
            bool hasColor,
            Color color,
            ThingStyleDef styleDef)
        {
            if (apparelDef == null)
            {
                return null;
            }

            Apparel apparel = ThingMaker.MakeThing(apparelDef, stuffDef) as Apparel;
            if (apparel == null)
            {
                return null;
            }

            if (hasColor)
            {
                ApplyColor(apparel, color);
            }

            if (styleDef != null)
            {
                apparel.StyleDef = styleDef;
            }

            return apparel;
        }

        /// <summary>
        /// 按当前原身服装创建镜像副本。
        /// </summary>
        public static Apparel CreateMirrored(Apparel original)
        {
            if (original == null)
            {
                return null;
            }

            CompColorable colorable = original.TryGetComp<CompColorable>();
            bool hasColor = colorable != null && colorable.Active;
            Color color = hasColor ? colorable.Color : Color.white;
            return Create(
                original.def,
                original.Stuff,
                hasColor,
                color,
                original.StyleDef);
        }

        /// <summary>
        /// 把颜色写回新建服装的可染色组件。
        /// </summary>
        private static void ApplyColor(Apparel apparel, Color color)
        {
            if (apparel == null)
            {
                return;
            }

            CompColorable colorable = apparel.TryGetComp<CompColorable>();
            if (colorable != null && colorable.Active)
            {
                colorable.SetColor(color);
            }
        }
    }
}
