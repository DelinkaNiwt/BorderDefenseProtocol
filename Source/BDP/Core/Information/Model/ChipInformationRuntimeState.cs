namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片信息解析的可选运行状态。
    /// </summary>
    public sealed class ChipInformationRuntimeState
    {
        /// <summary>创建运行时状态快照。</summary>
        public ChipInformationRuntimeState(
            bool hasRuntimeState,
            bool isActive,
            string activeModeKey,
            string activeStanceKey)
        {
            HasRuntimeState = hasRuntimeState;
            IsActive = isActive;
            ActiveModeKey = activeModeKey;
            ActiveStanceKey = activeStanceKey;
        }

        /// <summary>创建没有运行时绑定的空状态。</summary>
        public ChipInformationRuntimeState()
            : this(false, false, null, null)
        {
        }

        /// <summary>当前输入是否包含正式运行状态。</summary>
        public bool HasRuntimeState { get; set; }

        /// <summary>芯片当前是否处于激活状态。</summary>
        public bool IsActive { get; set; }

        /// <summary>当前活动形态键。</summary>
        public string ActiveModeKey { get; set; }

        /// <summary>当前活动姿态键。</summary>
        public string ActiveStanceKey { get; set; }
    }
}
