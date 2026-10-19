namespace BDP.Compatibility
{
    /// <summary>兼容适配器当前生命周期状态。</summary>
    public enum CompatibilityAdapterState
    {
        /// <summary>尚未尝试发现。</summary>
        Uninitialized,

        /// <summary>目标不存在，保持静默。</summary>
        NotPresent,

        /// <summary>目标存在且已启用。</summary>
        Enabled,

        /// <summary>目标存在但结构不支持。</summary>
        Unsupported,

        /// <summary>运行时异常后已关闭。</summary>
        Disabled
    }
}
