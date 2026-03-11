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

        // ── HediffDef（战斗体系统） ──
        public static HediffDef BDP_CombatBodyActive;
        public static HediffDef BDP_Exhaustion;
        public static HediffDef BDP_CombatBodyCollapsing;
        public static HediffDef BDP_CombatBodyPartPending;

        // ── HediffDef（护盾系统） ──
        public static HediffDef BDP_Shield;
        public static HediffDef BDP_ShieldFront;

        // 重构说明：
        // - BDP_CombatBodyPartDestroyed 已删除（使用原版 Hediff_MissingPart）
        // - BDP_CombatWound_* 系列已删除（使用原版 Hediff_Injury）

        // ── ThingDef（紧急脱离系统） ──
        public static ThingDef BDP_EmergencyBeacon;

        // ── ThingDef（芯片） ──
        public static ThingDef BDP_Chip_Shield;
        public static ThingDef BDP_Chip_ShieldFront;

        // ── ThingDef（PawnFlyer） ──
        public static ThingDef BDP_PawnFlyer_Grasshopper;

        // ── FleckDef（蚱蜢特效） ──
        public static FleckDef BDP_GrasshopperPlatform;
        public static FleckDef BDP_GrasshopperPulse;

        static BDP_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BDP_DefOf));
        }
    }
}
