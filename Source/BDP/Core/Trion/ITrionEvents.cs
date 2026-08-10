using System;

namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 资源边界事件。
    /// 这里只广播资源变化，不保存资源真值。
    /// </summary>
    public interface ITrionEvents
    {
        // 可用值从大于 0 变成小于等于 0 时触发。
        // 适合提醒外层“自由资源已见底”。
        event Action AvailableDepleted;

        // 总值从大于 0 变成小于等于 0 时触发。
        // 适合提醒外层“资源整体已经耗尽”。
        event Action TrionDepleted;
    }
}
