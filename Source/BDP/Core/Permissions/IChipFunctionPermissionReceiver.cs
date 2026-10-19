namespace BDP.Core.Permissions
{
    /// <summary>
    /// 芯片功能业务接收地图许可的中性接口。
    /// Core 只负责传递事实，不规定许可对应的玩法效果。
    /// </summary>
    public interface IChipFunctionPermissionReceiver
    {
        /// <summary>
        /// 接收一次芯片表达运行时的许可上下文。
        /// </summary>
        /// <param name="context">当前 Pawn、地图、派系、结果和许可读取入口。</param>
        void Receive(MapPermissionContext context);
    }
}
