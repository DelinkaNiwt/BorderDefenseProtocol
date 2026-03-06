using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体伤口Hediff类。
    /// 不继承Hediff_Injury，天然屏蔽出血/疼痛系统。
    /// 支持同部位多次受伤时的Hediff合并显示（如"枪伤（木棍） x2 [泄露+0.24/s]"）。
    /// </summary>
    /// <remarks>
    /// 设计要点：
    /// - 不继承Hediff_Injury：避免触发原版出血/疼痛/感染系统
    /// - 通过HediffComp_CombatWound实现Trion流失（注册到CompTrion聚合消耗）
    /// - 战斗体解除时由WoundHandler统一清理
    /// - 使用原版Hediff基类的武器信息字段（sourceDef, sourceLabel等）
    /// - 重写Severity属性，在变化时立即触发CompTrion注册更新（事件驱动，非轮询）
    /// </remarks>
    public class Hediff_CombatWound : HediffWithComps
    {
        /// <summary>
        /// 重写Severity属性，在setter中触发CompTrion注册更新。
        /// 事件驱动机制，避免tick轮询。
        /// </summary>
        public override float Severity
        {
            get => base.Severity;
            set
            {
                float oldSeverity = base.Severity;
                base.Severity = value;

                // 如果severity变化，通知Comp更新注册
                if (Mathf.Abs(value - oldSeverity) > 0.001f)
                {
                    var comp = this.TryGetComp<HediffComp_CombatWound>();
                    comp?.OnSeverityChanged();
                }
            }
        }
        /// <summary>
        /// 重写标签基础部分，直接返回def.label（原版逻辑）。
        /// </summary>
        public override string LabelBase => def.label;

        /// <summary>
        /// 重写标签括号内容，只显示武器信息（完全照搬原版 Hediff_Injury 逻辑）。
        /// </summary>
        public override string LabelInBrackets
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                // 原版武器信息逻辑（完全照搬 Hediff_Injury）
                if (sourceHediffDef != null)
                {
                    sb.Append(sourceHediffDef.label);
                }
                else if (sourceDef != null)
                {
                    if (!sourceToolLabel.NullOrEmpty())
                    {
                        sb.Append("SourceToolLabel".Translate(sourceLabel, sourceToolLabel));
                    }
                    else if (sourceBodyPartGroup != null)
                    {
                        sb.Append("SourceToolLabel".Translate(sourceLabel, sourceBodyPartGroup.LabelShort));
                    }
                    else
                    {
                        sb.Append(sourceLabel);
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// 重写完整标签，在括号后添加受伤次数。
        /// 格式：伤害类型（武器） x n
        /// Trion流失信息只在鼠标悬停的详情中显示。
        /// </summary>
        public override string Label
        {
            get
            {
                // 基础标签（包含括号内的武器信息）
                string baseLabel = base.Label;

                StringBuilder sb = new StringBuilder(baseLabel);

                var comp = this.TryGetComp<HediffComp_CombatWound>();
                if (comp != null)
                {
                    // 受伤次数（如果大于1）
                    if (comp.hitCount > 1)
                    {
                        sb.Append($" x{comp.hitCount}");
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// 重写标签颜色。
        /// 被破坏部位的伤口显示灰色，否则显示白色。
        /// </summary>
        public override Color LabelColor
        {
            get
            {
                // 检查是否战斗体激活
                var runtime = BDP.Combat.CombatBodyRuntime.Of(pawn);
                if (runtime == null || !runtime.IsActive)
                    return Color.white;

                // 检查部位是否被破坏
                if (Part != null && runtime.ShadowHP != null)
                {
                    float hp = runtime.ShadowHP.GetHP(Part);
                    if (hp <= 0f)
                        return Color.gray;  // 被破坏部位的伤口显示灰色
                }

                return Color.white;
            }
        }

        /// <summary>
        /// 重写提示信息，显示总伤害和受伤次数。
        /// </summary>
        public override string TipStringExtra
        {
            get
            {
                string baseStr = base.TipStringExtra;
                var comp = this.TryGetComp<HediffComp_CombatWound>();

                if (comp != null)
                {
                    string extra = $"\n受伤次数: {comp.hitCount}";
                    extra += $"\n总伤害: {this.Severity:F1}";
                    float drainPerDay = comp.Props.trionDrainPerSeverityPerDay * this.Severity;
                    float drainPerSecond = drainPerDay / GenDate.TicksPerDay * 60f;
                    extra += $"\nTrion流失: {drainPerSecond:F2}/秒 ({drainPerDay:F2}/天)";
                    return baseStr + extra;
                }

                return baseStr;
            }
        }
    }
}
