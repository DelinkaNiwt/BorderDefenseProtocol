using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Core.Chips;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 芯片制造 DefName 到当前 Def 的集中查找入口。
    /// </summary>
    public static class ChipManufacturingDefLookup
    {
        /// <summary>查找主分类。</summary>
        public static ChipCategoryDef FindCategory(string defName)
        {
            return string.IsNullOrWhiteSpace(defName)
                ? null
                : DefDatabase<ChipCategoryDef>.GetNamedSilentFail(defName);
        }

        /// <summary>查找职业。</summary>
        public static ChipProfessionDef FindProfession(string defName)
        {
            return string.IsNullOrWhiteSpace(defName)
                ? null
                : DefDatabase<ChipProfessionDef>.GetNamedSilentFail(defName);
        }

        /// <summary>查找动作预设。</summary>
        public static ChipActionPresetDef FindAction(string defName)
        {
            return string.IsNullOrWhiteSpace(defName)
                ? null
                : DefDatabase<ChipActionPresetDef>.GetNamedSilentFail(defName);
        }

        /// <summary>查找枪壳预设。</summary>
        public static ChipGunShellDef FindGunShell(string defName)
        {
            return string.IsNullOrWhiteSpace(defName)
                ? null
                : DefDatabase<ChipGunShellDef>.GetNamedSilentFail(defName);
        }
    }
}
