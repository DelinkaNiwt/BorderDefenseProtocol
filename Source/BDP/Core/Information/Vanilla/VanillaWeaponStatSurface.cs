using BDP.Core.Expressions;
using BDP.Core.AttackExecution;
using Verse;

namespace BDP.Core.Information.Vanilla
{
    /// <summary>
    /// 一条已通过能力判定的临时原版武器统计表面。
    /// 该对象只在当前调用期间存在，不是战斗真值，也不进入存档。
    /// </summary>
    public sealed class VanillaWeaponStatSurface
    {
        /// <summary>对应的正规化表达条目标识。</summary>
        public string EntryId { get; private set; }

        /// <summary>已裁定的近战或远程模式。</summary>
        public VerbExpressionModeConfig Mode { get; private set; }

        /// <summary>供原版统计 API 读取的临时 ThingDef。</summary>
        public ThingDef Definition { get; private set; }

        /// <summary>当前表面对应的单个近战 Tool；整把武器表面时为空。</summary>
        public MeleeToolSurface MeleeToolSurface { get; private set; }

        /// <summary>创建一条临时原版统计表面。</summary>
        internal VanillaWeaponStatSurface(
            string entryId,
            VerbExpressionModeConfig mode,
            ThingDef definition,
            MeleeToolSurface meleeToolSurface = null)
        {
            EntryId = entryId;
            Mode = mode;
            Definition = definition;
            MeleeToolSurface = meleeToolSurface;
        }
    }
}
