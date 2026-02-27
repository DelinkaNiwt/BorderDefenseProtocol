namespace BDP.Trigger
{
    /// <summary>
    /// 通知型管线接口——每tick只读观察，用于拖尾/视觉/音效。
    /// 在所有管线型阶段执行完毕后调用，不修改任何状态。
    /// </summary>
    public interface IBDPTickObserver
    {
        /// <summary>每tick通知（只读观察）。</summary>
        void OnTick(Bullet_BDP host);
    }
}
