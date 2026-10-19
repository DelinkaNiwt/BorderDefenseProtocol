using Verse;

namespace BDP.Content.Trion.Units
{
    /// <summary>人物或建筑的归属能力；额度与覆盖名单由提供者自行维护。</summary>
    public interface ITrionUnitBandwidthSource
    {
        /// <summary>只读检查建立及持续归属资格（如派系、中枢）；不分配、不扣带宽，也不因已绑定而拒绝。</summary>
        AcceptanceReport CanBind(Pawn unit);
        /// <summary>只读查询该兵是否在当前带宽覆盖内。</summary>
        bool Covers(Pawn unit);
        /// <summary>归属改变时维护提供者自己的名单，读档重复通知应幂等。</summary>
        void NotifyBindingChanged(Pawn unit, bool bound);
    }
}
