using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// Content 侧紧急脱离崩解缓存状态。
    /// Core 不保存紧急脱离解析结果，避免宿主状态反向携带业务类型。
    /// </summary>
    public sealed class CompCombatBodyEmergencyEscapeState : ThingComp
    {
        /// <summary>
        /// 崩解入口缓存的紧急脱离解析结果。
        /// </summary>
        private CombatBodyEmergencyEscapeResolution preparedResolution;

        /// <summary>
        /// 读取当前缓存的解析结果。
        /// </summary>
        public CombatBodyEmergencyEscapeResolution PreparedResolution
        {
            get { return preparedResolution; }
        }

        /// <summary>
        /// 写入一次崩解入口解析结果。
        /// </summary>
        public void SetPreparedResolution(CombatBodyEmergencyEscapeResolution resolution)
        {
            preparedResolution = resolution;
        }

        /// <summary>
        /// 清除已消费或已失效的崩解缓存。
        /// </summary>
        public void Clear()
        {
            preparedResolution = null;
        }

        /// <summary>
        /// 持久化 Content 侧紧急脱离缓存。
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref preparedResolution, "bdpCombatBodyEmergencyEscapeResolution");
        }
    }

    /// <summary>
    /// 紧急脱离 Content 状态组件配置。
    /// </summary>
    public sealed class CompProperties_CombatBodyEmergencyEscapeState : CompProperties
    {
        /// <summary>
        /// 构造并绑定紧急脱离状态组件。
        /// </summary>
        public CompProperties_CombatBodyEmergencyEscapeState()
        {
            compClass = typeof(CompCombatBodyEmergencyEscapeState);
        }
    }
}
