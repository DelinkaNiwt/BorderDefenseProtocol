using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

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
    /// v6.1变更：新增volleyVerb字段+右键拦截，支持齐射模式。
    ///   · volleyVerb非null时，右键进入齐射瞄准（BeginTargeting）
    ///   · volleyVerb为null时，右键走默认行为（无变化）
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

        /// <summary>齐射Verb实例（右键触发）。null=该芯片不支持齐射。</summary>
        public Verb volleyVerb;

        public override bool GroupsWith(Gizmo other)
        {
            // 只与同类型Command合并，不与标准Command_VerbTarget合并
            if (other is Command_BDPChipAttack cmd)
                return attackId == cmd.attackId;
            return false;
        }

        /// <summary>
        /// 重写GizmoOnGUIInt：拦截右键，volleyVerb存在时进入齐射瞄准。
        /// 原理：Command.GizmoOnGUIInt中Event.current.button==1返回OpenedFloatMenu。
        /// 拦截后调用BeginTargeting(volleyVerb)进入瞄准，返回Clear阻止浮动菜单。
        /// </summary>
        protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
        {
            var result = base.GizmoOnGUIInt(butRect, parms);

            // 拦截右键：volleyVerb存在时直接进入齐射瞄准
            if (result.State == GizmoState.OpenedFloatMenu && volleyVerb != null)
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                Find.Targeter.BeginTargeting(volleyVerb);
                return new GizmoResult(GizmoState.Clear);
            }

            return result;
        }

        public override string Desc
        {
            get
            {
                string baseDesc = base.Desc;
                if (volleyVerb != null)
                    return baseDesc + "\n右键：齐射";
                return baseDesc;
            }
        }
    }
}
