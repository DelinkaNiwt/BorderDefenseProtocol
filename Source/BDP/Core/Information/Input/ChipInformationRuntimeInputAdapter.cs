namespace BDP.Core.Information
{
    /// <summary>
    /// 将运行时激活状态转换为信息模型使用的中性状态快照。
    /// </summary>
    public static class ChipInformationRuntimeInputAdapter
    {
        /// <summary>创建运行时状态事实。</summary>
        public static ChipInformationRuntimeState Create(
            bool isActive,
            string activeModeKey,
            string activeStanceKey)
        {
            return new ChipInformationRuntimeState(
                true,
                isActive,
                activeModeKey,
                activeStanceKey);
        }

        /// <summary>创建没有运行时绑定的静态状态。</summary>
        public static ChipInformationRuntimeState CreateStatic()
        {
            return new ChipInformationRuntimeState(false, false, null, null);
        }
    }
}
