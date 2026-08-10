using System.Collections.Generic;
using BDP.Core.Requirements;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义层解释后的整枚芯片总契约。
    /// 它只承载正式声明结果，不承载运行时真值。
    /// </summary>
    internal sealed class ChipDefinitionContract
    {
        /// <summary>
        /// 当前契约对应的 ThingDef。
        /// </summary>
        public ThingDef ThingDef;

        /// <summary>
        /// 当前芯片的画像声明结果。
        /// </summary>
        public ChipProfileContract Profile;

        /// <summary>
        /// 当前芯片的装载声明结果。
        /// </summary>
        public ChipLoadoutContract Loadout;

        /// <summary>
        /// 当前芯片的表达声明引用。
        /// </summary>
        public ChipExpressionContractHandle Expression;

        /// <summary>
        /// 当前芯片的 Trion 声明结果。
        /// </summary>
        public ChipTrionContract Trion;

        /// <summary>
        /// 按作者声明顺序保存的激活条件集合。
        /// </summary>
        public IReadOnlyList<PawnRequirement> ActivationRequirements;

        /// <summary>
        /// 当前芯片的强类型静态扩展集合。
        /// 扩展随 Def 加载后保持不变，因此契约只保留其静态引用。
        /// </summary>
        public IReadOnlyList<ChipExtensionConfig> Extensions;

        /// <summary>
        /// 当前芯片已经正式声明的块集合。
        /// </summary>
        public IReadOnlyList<ChipDefinitionDeclaredBlock> DeclaredBlocks;
    }
}
