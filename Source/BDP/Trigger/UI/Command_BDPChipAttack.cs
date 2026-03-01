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
    /// v7.0变更：引导弹verb拦截——左键/右键直接启动多步锚点瞄准，
    ///   绕过原版targeting流程（原版verb.targetParams不允许地面瞄准）。
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
        /// 重写GizmoOnGUIInt：拦截左键/右键，引导弹verb直接启动多步锚点瞄准。
        ///
        /// PMS重构：统一使用Verb_BDPRangedBase.SupportsGuided判断，
        /// 不再依赖具体Verb子类类型检查。
        /// </summary>
        protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
        {
            var result = base.GizmoOnGUIInt(butRect, parms);

            // 拦截左键：支持引导的verb直接启动多步锚点瞄准
            if (result.State == GizmoState.Interacted
                && verb is Verb_BDPRangedBase ranged && ranged.SupportsGuided)
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                ranged.StartAnchorTargeting();
                return new GizmoResult(GizmoState.Clear);
            }
            // 双手触发verb含变化弹侧时，左键也启动锚点瞄准
            if (result.State == GizmoState.Interacted
                && verb is Verb_BDPDualRanged dual && dual.HasGuidedSide)
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                dual.StartAnchorTargeting();
                return new GizmoResult(GizmoState.Clear);
            }

            // 拦截右键：volleyVerb存在时进入齐射瞄准
            if (result.State == GizmoState.OpenedFloatMenu && volleyVerb != null)
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                // 引导齐射verb：检查是否支持引导
                if (volleyVerb is Verb_BDPRangedBase rangedVolley && rangedVolley.SupportsGuided)
                {
                    rangedVolley.StartAnchorTargeting();
                }
                // 双侧齐射verb含变化弹侧
                else if (volleyVerb is Verb_BDPDualVolley dualVolley && dualVolley.HasGuidedSide)
                {
                    dualVolley.StartAnchorTargeting();
                }
                else
                {
                    // 普通齐射verb走原版targeting
                    Find.Targeter.BeginTargeting(volleyVerb);
                }
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
