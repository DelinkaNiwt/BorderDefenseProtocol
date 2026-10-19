using HarmonyLib;

namespace BDP.Compatibility
{
    /// <summary>
    /// 一个外部模组兼容适配器的最小生命周期契约。
    /// </summary>
    public interface IExternalModCompatibilityAdapter
    {
        /// <summary>稳定的适配器标识。</summary>
        string Id { get; }

        /// <summary>当前适配器是否已启用。</summary>
        bool IsEnabled { get; }

        /// <summary>尝试发现目标模组并安装自己的 Harmony 补丁。</summary>
        bool TryInitialize(Harmony harmony, out string failureReason);

        /// <summary>关闭当前会话中的适配器。</summary>
        void Disable(string reason);
    }
}
