using RimWorld;
using Verse;

namespace BDP.Content.OperatorConsole
{
    /// <summary>
    /// 操作员控制台工作定义引用。
    /// </summary>
    [DefOf]
    public static class OperatorConsoleJobDefOf
    {
        /// <summary>控制台持续值守工作。</summary>
        public static JobDef BDP_OperateOperatorConsole;

        /// <summary>确保 Def 引用完成初始化。</summary>
        static OperatorConsoleJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(OperatorConsoleJobDefOf));
        }
    }
}
