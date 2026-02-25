using Verse;

namespace BDP.Core
{
    /// <summary>
    /// Trion腺体基因——天赋配置器。
    /// 表达"此Pawn拥有Trion腺体"这一先天事实。
    /// 向Stat系统贡献基础属性值（通过GeneDef.statOffsets）。
    /// 不持有任何Trion数据，不参与运行时逻辑。
    ///
    /// 继承Gene而非Gene_Resource的原因：
    ///   Gene_Resource假设自身持有cur/max，
    ///   数据在CompTrion中 → 继承它会产生两份数据源，
    ///   违反Single Source of Truth。
    /// </summary>
    public class Gene_TrionGland : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            // Stat重新聚合，更新CompTrion.max
            pawn.GetComp<CompTrion>()?.RefreshMax();
        }

        public override void PostRemove()
        {
            base.PostRemove();
            // max可能缩小
            pawn.GetComp<CompTrion>()?.RefreshMax();
        }
    }
}
