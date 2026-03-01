namespace BDP.Trigger
{
    /// <summary>
    /// 视觉观察者管线接口——每tick只读观察，零副作用。
    /// 用于拖尾、视觉特效、音效等纯表现层逻辑。
    /// 执行顺序：管线第5阶段（VisualObserve）。
    /// </summary>
    public interface IBDPVisualObserver
    {
        /// <summary>观察宿主状态（只读，零副作用）。</summary>
        void Observe(Bullet_BDP host);
    }
}
