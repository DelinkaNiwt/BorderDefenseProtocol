namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体内部的稳定侧别命名。
    /// 固定约定：Main = 右，Sub = 左。
    /// </summary>
    public enum TriggerSide
    {
        // 主侧，固定约定对应右手。
        Main,
        // 副侧，固定约定对应左手。
        Sub,
        // 特殊侧，预留给非主副的特殊槽位体系。
        Special
    }
}
