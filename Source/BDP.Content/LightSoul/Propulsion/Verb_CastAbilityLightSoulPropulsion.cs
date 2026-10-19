using BDP.Content.GroundDash;
using BDP.Content.Shield;
using RimWorld;
using Verse;

namespace BDP.Content.LightSoul.Propulsion
{
    /// <summary>
    /// 光魂推进的目标入口。
    /// 复用中性地面冲刺的路径设施，并为射程外或建筑后的鼠标位置
    /// 提供与实际施法终点一致的路径预览。
    /// </summary>
    public class Verb_CastAbilityLightSoulPropulsion : Verb_CastAbilityGroundDash
    {
        /// <summary>
        /// 让原版 Targeter（目标器）在射程外或建筑遮挡后仍保留鼠标位置，
        /// 但只对至少存在一格推进路径的位置显示预览；确认与施法启动阶段
        /// 都会继续使用同一份实际路径解析结果。
        /// </summary>
        public override bool CanHitTarget(LocalTargetInfo target)
        {
            if (caster == null || caster.Map == null || !target.IsValid)
            {
                return false;
            }

            GroundDashPathResult path;
            return GroundDashUtility.TryResolvePath(
                caster.Map,
                caster.Position,
                target.Cell,
                EffectiveRange,
                out path)
                && path.HasMovement;
        }

        /// <summary>
        /// 允许确认与预览一致的有效终点。
        /// 基类地面冲刺校验要求鼠标目标本身完整可达，会错误拒绝射程外
        /// 或建筑后的预览终点；实际路径仍由效果组件再次解析并截断。
        /// </summary>
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!CanHitTarget(target) || !IsApplicableTo(target, showMessages) || ability == null)
            {
                return false;
            }

            for (int i = 0; i < ability.EffectComps.Count; i++)
            {
                if (!ability.EffectComps[i].Valid(target, showMessages))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 把射程外或建筑后的请求位置规范为实际推进终点。
        /// 原版 Verb 暖机时会再次调用 TryFindShootLineFromTo（射击线查找），
        /// 该方法不认识可截断冲刺；传入已解析终点即可保留原版暖机时序，
        /// 同时避免第二次校验把能力静默取消。
        /// </summary>
        public override bool TryStartCastOn(
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack = false,
            bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false,
            bool nonInterruptingSelfCast = false)
        {
            GroundDashPathResult path;
            if (caster != null
                && caster.Map != null
                && castTarg.IsValid
                && GroundDashUtility.TryResolvePath(
                    caster.Map,
                    caster.Position,
                    castTarg.Cell,
                    EffectiveRange,
                    out path)
                && path.HasMovement)
            {
                castTarg = new LocalTargetInfo(path.EndCell);
            }

            return base.TryStartCastOn(
                castTarg,
                destTarg,
                surpriseAttack,
                canHitNonTargetPawns,
                preventFriendlyFire,
                nonInterruptingSelfCast);
        }

        /// <summary>
        /// 能力进入飞行器后，原版 Verb 收尾会因 Pawn 已暂时离图而提前返回；
        /// 在此保留原版 AdjustedCooldownTicks（修正冷却 tick）计算，并设置同一
        /// 个 Stance_Cooldown（动作冷却姿态）。
        /// </summary>
        protected override bool TryCastShot()
        {
            // 预热期间可能破盾，必须在能力扣费和生成飞行器之前复查同一事实。
            if (!EnergyShieldApi.IsIntact(CasterPawn, EnergyShieldApi.LightSoulGroupKey))
                return false;
            bool castSucceeded = base.TryCastShot();
            if (castSucceeded && CasterPawn != null && !NonInterruptingSelfCast)
            {
                int cooldownTicks = verbProps.AdjustedCooldownTicks(this, CasterPawn);
                if (cooldownTicks > 0)
                {
                    CasterPawn.stances.SetStance(
                        new Stance_Cooldown(cooldownTicks, CurrentTarget, this));
                }
            }

            return castSucceeded;
        }

        /// <summary>绘制最大射程圈、实际推进区域和被路径裁剪后的终点高亮。</summary>
        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (caster == null || caster.Map == null)
            {
                return;
            }

            if (EffectiveRange > 0f)
            {
                verbProps.DrawRadiusRing(caster.Position, this);
            }

            if (!target.IsValid)
            {
                return;
            }

            GroundDashPathResult path;
            if (!GroundDashUtility.TryResolvePath(
                caster.Map,
                caster.Position,
                target.Cell,
                EffectiveRange,
                out path)
                || !path.HasMovement)
            {
                return;
            }

            ability.DrawEffectPreviews(target);
            GenDraw.DrawTargetHighlightWithLayer(
                path.EndCell.ToVector3Shifted(),
                AltitudeLayer.MetaOverlays);
        }
    }
}
