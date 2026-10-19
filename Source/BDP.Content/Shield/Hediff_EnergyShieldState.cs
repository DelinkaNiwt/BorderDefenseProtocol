using System.Collections.Generic;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>原版持久健康状态；不由表达同步移除，也不参与战斗体原身快照。</summary>
    public sealed class Hediff_EnergyShieldState : Hediff
    {
        /// <summary>人物各护盾组记录；普通双盾共用一条，其他盾型各自独立。</summary>
        private List<EnergyShieldDurabilityState> groups = new List<EnergyShieldDurabilityState>();
        /// <summary>此记录不是玩家疾病，不在健康列表中单独显示。</summary>
        public override bool Visible { get { return false; } }
        /// <summary>持久状态不因零严重度自动移除。</summary>
        public override bool ShouldRemove { get { return false; } }
        /// <summary>只读组列表，供统一接口和开发诊断读取。</summary>
        public IReadOnlyList<EnergyShieldDurabilityState> Groups { get { return groups; } }

        /// <summary>按定义组键定位记录；只有正式盾源启用或外部破盾才创建。</summary>
        public EnergyShieldDurabilityState FindGroup(string key, bool create = false)
        {
            for (int i = 0; i < groups.Count; i++)
                if (groups[i].GroupKey == key) return groups[i];
            if (!create) return null;
            var state = new EnergyShieldDurabilityState { GroupKey = key };
            groups.Add(state);
            return state;
        }

        /// <summary>各组统一结算自然恢复与破盾冷却；只有破盾到期转换通知表达重新求值。</summary>
        public override void Tick()
        {
            base.Tick();
            bool changed = false;
            int tick = Find.TickManager.TicksGame;
            for (int i = 0; i < groups.Count; i++)
            {
                var shield = EnergyShieldApi.FindActiveShield(pawn, groups[i].GroupKey);
                // Severity（来源数量）可直接变化，不必等下一次格挡才修正固定恢复的上限。
                // 没有防护宿主时沿用最近配置，让关闭芯片后的恢复继续。
                if (shield != null) EnergyShieldApi.UpdateSourceConfiguration(groups[i], shield);
                changed |= groups[i].RecoverIfDue(tick);
            }
            if (changed) ExpressionSurfaceAccess.NotifySourceConditionsChanged(pawn);
        }

        /// <summary>原版深度存档保证关闭芯片、切换姿态和读档后继续沿用状态。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref groups, "shieldGroups", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && groups == null)
                groups = new List<EnergyShieldDurabilityState>();
        }
    }
}
