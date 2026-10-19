using BDP.Content.Assembly.ChipManufacturing.Defs;
using HarmonyLib;
using Verse;

namespace BDP.Content.Patches
{
    /// <summary>原版不识别自定义Def的图标，仅为BDP制造定义提供现有芯片图标。</summary>
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.DefIcon))]
    public static class Patch_Widgets_DefIcon_ChipManufacturing
    {
        /// <summary>只改变本次绘图参数；外层信息卡链接仍指向真实功能或构型定义。</summary>
        public static void Prefix(ref Def def)
        {
            if (!(def is ChipManufacturingPresetDef) && !(def is ChipArmamentFormDef))
            {
                return;
            }
            ThingDef iconDef = DefDatabase<ThingDef>.GetNamedSilentFail("BDP_Chip_Custom");
            if (iconDef != null)
            {
                def = iconDef;
            }
        }
    }
}
