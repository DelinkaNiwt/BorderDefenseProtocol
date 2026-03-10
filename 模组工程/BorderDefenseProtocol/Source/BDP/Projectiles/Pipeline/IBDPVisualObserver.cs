namespace BDP.Projectiles.Pipeline
{
    /// <summary>
    /// 视觉观察者管线接口——每tick只读观察，零副作用。
    /// 用于拖尾、视觉特效、音效等纯表现层逻辑。
    ///
    /// 生命周期：
    ///   OnVisualInit — 第一个base.TickInterval之前调用一次（Stage 0）。
    ///                  此时DrawPos ≈ 发射原点，是视觉状态初始化的正确时机。
    ///   Observe      — 每tick在base.TickInterval之后调用（Stage 5），子弹已移动。
    ///                  职责：纯输出视觉效果，不做状态初始化。
    /// </summary>
    public interface IBDPVisualObserver
    {
        /// <summary>
        /// 视觉状态初始化——在第一个base.TickInterval之前调用一次。
        /// 实现者应在此记录起始位置等初始视觉状态。
        /// </summary>
        void OnVisualInit(Bullet_BDP host);

        /// <summary>观察宿主状态，输出视觉效果（只读，零副作用）。</summary>
        void Observe(Bullet_BDP host);
    }
}
