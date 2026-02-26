using RimWorld;
using Verse;

namespace BDP.Core
{
    /// <summary>
    /// DefOf引用类——编译时绑定XML中定义的Def。
    /// 字段名必须与XML中的defName完全一致。
    /// 注意（v1.6）：BDP_Hediff_TrionDepletion已移出此类。
    /// 该Hediff由战斗体模块通过自身DefOf直接引用，微内核不持有引用。
    /// </summary>
    [DefOf]
    public static class BDP_DefOf
    {
        // ── StatDef ──
        public static StatDef BDP_TrionCapacity;
        public static StatDef BDP_TrionOutputPower;
        public static StatDef BDP_TrionRecoveryRate;

        // ── GeneDef ──
        public static GeneDef BDP_Gene_TrionGland;

        // ── JobDef（芯片攻击） ──
        public static JobDef BDP_ChipRangedAttack;
        public static JobDef BDP_ChipMeleeAttack;

        static BDP_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BDP_DefOf));
        }
    }
}
