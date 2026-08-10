namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片 Def 层使用的统一攻击执行配置。
    /// 它追求作者接口直观，不直接暴露内部实现字段。
    /// </summary>
    public sealed class ChipAttackExecutionConfig
    {
        /// <summary>
        /// 当前条目的作者侧节奏声明。
        /// 解释器会按武器模式翻译成内部正式模型。
        /// </summary>
        public ChipAttackExecutionRhythmConfig Rhythm = ChipAttackExecutionRhythmConfig.None;

        /// <summary>
        /// 一次攻击内部的动作数量。
        /// 远程表示发射数，近战表示命中数。
        /// </summary>
        public int HitCount = 0;

        /// <summary>
        /// 相邻两次内部动作之间的 tick 间隔。
        /// 远近战统一复用这一作者字段。
        /// </summary>
        public int HitIntervalTicks = 0;

        /// <summary>
        /// 远程发射点随机散布区间。
        /// 它使用射击方向局部坐标：Lateral 表示左右，Forward 表示前后。
        /// </summary>
        public ChipAttackOriginSpreadConfig OriginSpread;
    }

    /// <summary>
    /// 作者侧声明的远程发射点随机散布区间。
    /// 每发 projectile 真正发射时都会在该区间内独立随机取样。
    /// </summary>
    public sealed class ChipAttackOriginSpreadConfig
    {
        /// <summary>
        /// 横向最小偏移。负值偏左，正值偏右。
        /// </summary>
        public float LateralMin = 0f;

        /// <summary>
        /// 横向最大偏移。负值偏左，正值偏右。
        /// </summary>
        public float LateralMax = 0f;

        /// <summary>
        /// 前后最小偏移。负值靠后，正值靠前。
        /// </summary>
        public float ForwardMin = 0f;

        /// <summary>
        /// 前后最大偏移。负值靠后，正值靠前。
        /// </summary>
        public float ForwardMax = 0f;
    }
}
