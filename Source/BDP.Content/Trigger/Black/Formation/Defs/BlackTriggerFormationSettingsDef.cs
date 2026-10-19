using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>黑触发器形成系统的全局概率、危机和时序设置。</summary>
    public sealed class BlackTriggerFormationSettingsDef : Def
    {
        /// <summary>失血死亡倒计时危机阈值。</summary>
        public int crisisBleedoutTicks = 2500;

        /// <summary>致死病症达到该比例时视为个人危机。</summary>
        public float crisisLethalSeverityRatio = 0.9f;

        /// <summary>普通自然死亡在存在候选时进入形成流程的概率。</summary>
        public float naturalFormationChance = 0.15f;

        /// <summary>待决事件期限；默认 7500 Tick，即三游戏小时。</summary>
        public int pendingEventTicks = 7500;

        /// <summary>普通流程全局冷却；默认 60000 Tick，即一游戏日。</summary>
        public int normalCooldownTicks = 60000;

        /// <summary>尸体与期限扫描间隔。</summary>
        public int eventScanIntervalTicks = 60;
    }

    /// <summary>形成系统设置 DefOf。</summary>
    [DefOf]
    public static class BlackTriggerFormationSettingsDefOf
    {
        /// <summary>正式全局形成设置。</summary>
        public static BlackTriggerFormationSettingsDef BDP_BlackTriggerFormationSettings;

        /// <summary>保证第一次读取前完成 DefOf 绑定。</summary>
        static BlackTriggerFormationSettingsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BlackTriggerFormationSettingsDefOf));
        }
    }
}
