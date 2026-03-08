namespace BDP.Trigger
{
    /// <summary>
    /// Phase转换观察者管线接口——当宿主Phase发生变化时通知模块。
    /// v5新增：将Phase转换事件化，模块可响应Phase变化执行副作用。
    /// </summary>
    public interface IBDPPhaseTransitionObserver
    {
        /// <summary>Phase发生变化时回调。</summary>
        void OnPhaseChanged(Bullet_BDP host, FlightPhase oldPhase, FlightPhase newPhase);
    }
}
