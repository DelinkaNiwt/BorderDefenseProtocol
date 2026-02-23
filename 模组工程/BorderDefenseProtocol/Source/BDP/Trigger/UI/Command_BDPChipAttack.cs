using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 自定义Command类（v5.0新增）——重写GroupsWith()，基于attackId控制Gizmo合并。
    ///
    /// 解决问题：RimWorld引擎的Command_VerbTarget.GroupsWith()按ownerThing.def合并，
    /// 导致同一触发体的所有芯片Verb只显示1个Gizmo。
    ///
    /// 方案：芯片Verb设hasStandardCommand=false脱离标准路径，
    /// CompGetEquippedGizmosExtra中用本类生成独立Gizmo，按attackId控制合并。
    ///
    /// attackId生成规则（基于芯片defName）：
    ///   独立芯片攻击 → chipDef.defName（如"BDP_ChipArcMoon"）
    ///   双手触发     → "dual:" + Sort(A,B).Join("+")（如"dual:BDP_ChipArcMoon+BDP_ChipScorpion"）
    ///   组合技能     → "combo:" + comboAbilityDef.defName
    ///
    /// 合并效果：
    ///   同芯片两侧（弧月+弧月）→ 同defName → 合并为1个gizmo
    ///   不同芯片（弧月+蝎子）  → 不同defName → 分开显示
    ///   跨pawn同种芯片          → 同defName → 合并
    /// </summary>
    public class Command_BDPChipAttack : Command_VerbTarget
    {
        /// <summary>基于芯片类型的攻击标识，用于GroupsWith合并判断。</summary>
        public string attackId;

        public override bool GroupsWith(Gizmo other)
        {
            // 只与同类型Command合并，不与标准Command_VerbTarget合并
            if (other is Command_BDPChipAttack cmd)
                return attackId == cmd.attackId;
            return false;
        }
    }
}
