namespace BDP.Development.Trigger.Diagnostics
{
    /// <summary>
    /// Trigger 地图画点诊断的共享开关状态。
    /// 它只保存本次运行期的诊断偏好，不写入存档，也不参与正式战斗状态。
    /// </summary>
    public static class TriggerVisualMarkerSettings
    {
        /// <summary>
        /// 当前是否启用地图画点叠加层。
        /// </summary>
        public static bool OverlayEnabled { get; set; }
    }
}
