using Verse;

namespace BDP.Trigger
{
    /// <summary>追踪模块转向模式。</summary>
    public enum TrackingTurnMode
    {
        /// <summary>角速度限幅——每tick限制最大转向角度。参数少，适合普通追踪弹。</summary>
        Simple,
        /// <summary>角加速度+阻尼——转向有惯性，弹道更平滑。适合超级追踪弹（追踪+追踪组合）。</summary>
        Smooth,
        /// <summary>贝塞尔曲线——每tick动态重算二次贝塞尔，产出精确下一帧位置。弹道为几何平滑曲线。</summary>
        Bezier
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

        // ── 贝塞尔参数（仅Bezier模式） ──

        /// <summary>
        /// 控制点距离比例（0~1，基于当前位置到目标的距离）。
        /// 越大曲线前段越直、末段才急转；越小曲线从起点就开始弯。
        /// </summary>
        public float bezierControlRatio = 0.4f;

        // ── 目标预测参数（三种模式通用） ──

        /// <summary>是否启用目标位置预测（对移动中的Pawn做速度外推）。</summary>
        public bool enablePrediction = false;

        /// <summary>预测外推tick数。越大预判越远，但对变向目标误差越大。</summary>
        public int predictionTicks = 3;

        // ── 阶段转速参数（Simple/Smooth模式，Bezier通过曲线几何自然加速） ──

        /// <summary>末段转速倍率。当距目标 ≤ 初始距离×finalPhaseRatio 时生效。</summary>
        public float finalPhaseTurnMult = 1.5f;

        /// <summary>末段激活距离比例（基于初始距离）。</summary>
        public float finalPhaseRatio = 0.3f;

        // ── 激活参数 ──

        /// <summary>
        /// 追踪激活距离比例（基于射手到目标的初始距离）。
        /// 子弹距目标 ≤ 初始距离 × 此值时开始追踪。
        /// 例：0.67 = 飞过约1/3路程后激活。1.0 = 立即追踪。0 = 不使用距离激活（回退到trackingDelay）。
        /// </summary>
        public float trackingStartRatio = 0.67f;

        /// <summary>发射后延迟N tick才开始追踪（直飞阶段）。trackingStartRatio>0时作为最低保底延迟。</summary>
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

        /// <summary>是否允许丢锁后重新搜索并切换目标。</summary>
        public bool allowRetarget = true;

        /// <summary>丢锁后继续飞行多少tick后自毁。</summary>
        public int lostTrackingSelfDestructTicks = 60;
    }
}
