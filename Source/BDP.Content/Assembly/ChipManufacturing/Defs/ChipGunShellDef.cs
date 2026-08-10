using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Defs
{
    /// <summary>
    /// 芯片制造使用的枪壳预设。
    /// </summary>
    public sealed class ChipGunShellDef : Def
    {
        /// <summary>允许使用该枪壳的最终职业。</summary>
        public List<ChipProfessionDef> compatibleProfessions;

        /// <summary>枪壳对动作字段的可选覆盖。</summary>
        public ChipGunShellOverrides overrides;

        /// <summary>枪壳对投射物字段的可选覆盖。</summary>
        public ProjectileOverrides projectileOverrides;

        /// <summary>枪壳对单枚成品追加的具体材料。</summary>
        public List<ThingDefCountClass> additionalCost;

        /// <summary>枪壳对单枚成品追加的工作量。</summary>
        public float additionalWorkAmount;
    }
}
