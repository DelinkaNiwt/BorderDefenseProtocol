using Verse;

namespace BDP.Trigger
{
    /// <summary>追踪模块转向模式。</summary>
    public enum TrackingTurnMode
    {
        /// <summary>角速度限幅——每tick限制最大转向角度。参数少，适合普通追踪弹。</summary>
        Simple,
        /// <summary>角加速度+阻尼——转向有惯性，弹道更平滑。适合超级追踪弹（追踪+追踪组合）。</summary>
        Smooth
    }

    /// <summary>
    /// 追踪模块配置——挂在投射物ThingDef的modExtensions上。
    /// 由TrackingModule在飞行中每tick读取参数执行追踪逻辑。
    /// </summary>
    public class BDPTrackingConfig : DefModExtension
    {
        // ── 转向参数 ──

        /// <summary>转向模式：Simple=角速度限幅，Smooth=角加速度+阻尼。</summary>
        public TrackingTurnMode turnMode = TrackingTurnMode.Simple;

        /// <summary>每tick最大转向角度（度）。两种模式共用。</summary>
        public float maxTurnRate = 8f;

        /// <summary>角加速度（度/tick²）。仅Smooth模式使用。</summary>
        public float angularAccel = 5f;

        /// <summary>角速度阻尼系数（0~1，每tick乘以此值衰减）。仅Smooth模式使用。</summary>
        public float damping = 0.95f;

        // ── 时间参数 ──

        /// <summary>发射后延迟N tick才开始追踪（直飞阶段）。</summary>
        public int trackingDelay = 20;

        /// <summary>最大飞行tick数，超时自毁。</summary>
        public int maxFlyingTicks = 600;

        // ── 脱锁参数 ──

        /// <summary>目标偏移角度超过此值时脱锁（度）。180=永不因角度脱锁。</summary>
        public float maxLockAngle = 120f;

        // ── 重搜索参数 ──

        /// <summary>目标丢失后搜索新目标的半径（格）。</summary>
        public float searchRadius = 15f;

        /// <summary>搜索新目标的间隔（tick）。</summary>
        public int searchInterval = 30;
    }
}
