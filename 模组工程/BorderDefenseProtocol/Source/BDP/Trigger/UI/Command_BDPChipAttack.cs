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
    /// v6.1变更：新增secondaryVerb字段+右键拦截，支持副攻击模式。
    ///   · secondaryVerb非null时，右键进入副攻击瞄准（BeginTargeting）
    ///   · secondaryVerb为null时，右键走默认行为（无变化）
    ///
    /// v7.0变更：引导弹verb拦截——左键/右键直接启动多步锚点瞄准，
    ///   绕过原版targeting流程（原版verb.targetParams不允许地面瞄准）。
    ///
    /// v8.0变更：secondaryVerb语义为"副攻击verb"（右键），不限于齐射。
    ///   · secondaryVerb可以是任意类型的verb（齐射、逐发、引导等）
    ///   · 描述文本根据verb类型动态生成
    ///
    /// v9.0变更：移除类型判断，描述文本从VerbProperties.label读取。
    ///   · 不再判断verb类型（改用FiringPattern配置）
    ///   · 直接从secondaryVerb.verbProps.label读取描述
    ///
    /// v10.0变更：单侧攻击不再合并，确保FireMode配置独立。
    ///   · 左手攻击：attackId = "left:" + chipDef.defName
    ///   · 右手攻击：attackId = "right:" + chipDef.defName
    ///   · 即使芯片相同，左右手也分开显示（因为FireMode可能不同）
    ///
    /// attackId生成规则（基于芯片defName + 侧别）：
    ///   左手芯片攻击 → "left:" + chipDef.defName（如"left:BDP_ChipArcMoon"）
    ///   右手芯片攻击 → "right:" + chipDef.defName（如"right:BDP_ChipArcMoon"）
    ///   双手触发     → "dual:" + Sort(A,B).Join("+")（如"dual:BDP_ChipArcMoon+BDP_ChipScorpion"）
    ///   组合技能     → "combo:" + comboAbilityDef.defName
    ///
    /// 合并效果：
    ///   同芯片两侧（弧月+弧月）→ 不同attackId → 分开显示为2个gizmo（左/右）
    ///   不同芯片（弧月+蝎子）  → 不同attackId → 分开显示
    ///   跨pawn同侧同芯片       → 同attackId → 合并（如多个pawn的左手弧月）
    /// </summary>
    public class Command_BDPChipAttack : Command_VerbTarget
    {
        /// <summary>基于芯片类型的攻击标识，用于GroupsWith合并判断。</summary>
        public string attackId;

        /// <summary>副攻击Verb实例（右键触发）。null=该芯片不支持副攻击，右键走默认行为。</summary>
        public Verb secondaryVerb;

        public override bool GroupsWith(Gizmo other)
        {
            if (other is Command_BDPChipAttack cmd)
                return attackId == cmd.attackId;
            return false;
        }

        /// <summary>
        /// 重写GizmoOnGUIInt：拦截左键/右键，引导弹verb直接启动多步锚点瞄准。
        ///
        /// PMS重构：统一使用Verb_BDPRangedBase.SupportsGuided判断，
        /// 不再依赖具体Verb子类类型检查。
        ///
        /// v11.0变更：在瞄准开始时创建 ShotSession（Task 19）。
        /// </summary>
        protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
        {
            var result = base.GizmoOnGUIInt(butRect, parms);

            // 拦截左键：支持引导的verb直接启动多步锚点瞄准
            if (result.State == GizmoState.Interacted
                && verb is Verb_BDPRangedBase ranged && ranged.SupportsGuided)
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                // 在瞄准开始时创建 ShotSession
                BeginTargetingSession(ranged);
                ranged.StartAnchorTargeting();
                return new GizmoResult(GizmoState.Clear);
            }
            // 拦截右键：secondaryVerb存在时进入副攻击瞄准
            if (result.State == GizmoState.OpenedFloatMenu && secondaryVerb != null)
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                // 引导副攻击verb：检查是否支持引导
                if (secondaryVerb is Verb_BDPRangedBase rangedSecondary && rangedSecondary.SupportsGuided)
                {
                    // 在瞄准开始时创建 ShotSession
                    BeginTargetingSession(rangedSecondary);
                    rangedSecondary.StartAnchorTargeting();
                }
                else
                {
                    // 普通副攻击verb走原版targeting
                    Find.Targeter.BeginTargeting(secondaryVerb);
                }
                return new GizmoResult(GizmoState.Clear);
            }

            return result;
        }

        /// <summary>
        /// 在瞄准开始时创建 ShotSession。
        /// 使用占位符 target（LocalTargetInfo.Invalid），在瞄准过程中会被更新。
        /// </summary>
        private void BeginTargetingSession(Verb_BDPRangedBase rangedVerb)
        {
            if (rangedVerb == null) return;

            // 初始化射击管线
            rangedVerb.InitShotPipeline();

            // 构建射击上下文（使用占位符 target）
            var context = rangedVerb.BuildContext();

            // 创建射击会话
            rangedVerb.activeSession = new ShotPipeline.ShotSession(context);
        }

        public override string Desc
        {
            get
            {
                string baseDesc = base.Desc;
                if (secondaryVerb != null)
                {
                    // v9.0：从VerbProperties.label读取副攻击描述
                    string secondaryLabel = secondaryVerb.verbProps.label ?? "副攻击";
                    return baseDesc + $"\n右键：{secondaryLabel}";
                }
                return baseDesc;
            }
        }
    }
}
