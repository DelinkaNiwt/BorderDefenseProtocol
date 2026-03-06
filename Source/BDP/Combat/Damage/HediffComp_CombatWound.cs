using Verse;
using RimWorld;
using BDP.Core;
using UnityEngine;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体伤口Comp配置类。
    /// </summary>
    public class HediffCompProperties_CombatWound : HediffCompProperties
    {
        /// <summary>
        /// 每点severity每天消耗的Trion量。
        /// 默认值：5.0（即severity=1的伤口每天消耗5 Trion）
        /// </summary>
        public float trionDrainPerSeverityPerDay = 5f;

        /// <summary>
        /// 伤害类型标识（用于日志和调试）。
        /// </summary>
        public string damageTypeKey = "Unknown";

        public HediffCompProperties_CombatWound()
        {
            this.compClass = typeof(HediffComp_CombatWound);
        }
    }

    /// <summary>
    /// 战斗体伤口Comp类。
    /// 负责：
    /// 1. 记录受伤次数（用于显示"x2"）
    /// 2. 注册Trion流失到CompTrion聚合消耗系统（事件驱动，非轮询）
    /// 3. 序列化数据
    /// </summary>
    public class HediffComp_CombatWound : HediffComp
    {
        /// <summary>
        /// 受伤次数（同部位同类型伤口合并时累加）。
        /// </summary>
        public int hitCount = 1;

        /// <summary>
        /// 获取配置属性。
        /// </summary>
        public HediffCompProperties_CombatWound Props => (HediffCompProperties_CombatWound)props;

        /// <summary>
        /// 获取此伤口在CompTrion.drainRegistry中的唯一key。
        /// 格式：wound_{hediffID}
        /// </summary>
        private string DrainKey => $"wound_{parent.loadID}";

        /// <summary>
        /// 伤口添加时注册Trion流失。
        /// </summary>
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            UpdateDrainRegistration();
        }

        /// <summary>
        /// Severity变化时的回调（由Hediff_CombatWound.Severity setter调用）。
        /// 事件驱动机制，避免tick轮询。
        /// </summary>
        public void OnSeverityChanged()
        {
            UpdateDrainRegistration();
        }

        /// <summary>
        /// 更新CompTrion的drain注册。
        /// </summary>
        private void UpdateDrainRegistration()
        {
            var compTrion = parent.pawn?.GetComp<CompTrion>();
            if (compTrion == null) return;

            float drainPerDay = Props.trionDrainPerSeverityPerDay * parent.Severity;

            if (drainPerDay > 0f)
            {
                compTrion.RegisterDrain(DrainKey, drainPerDay);
            }
            else
            {
                compTrion.UnregisterDrain(DrainKey);
            }
        }

        /// <summary>
        /// 伤口移除时注销Trion流失。
        /// </summary>
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            var compTrion = parent.pawn?.GetComp<CompTrion>();
            if (compTrion != null)
            {
                compTrion.UnregisterDrain(DrainKey);
            }
        }

        /// <summary>
        /// 序列化数据。
        /// </summary>
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref hitCount, "hitCount", 1);

            // 读档后重新注册drain（因为CompTrion的drainRegistry已恢复）
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                UpdateDrainRegistration();
            }
        }

        /// <summary>
        /// 调试信息。
        /// </summary>
        public override string CompDebugString()
        {
            return $"hitCount={hitCount}, drainRate={Props.trionDrainPerSeverityPerDay * parent.Severity:F2}/day, drainKey={DrainKey}";
        }
    }
}
