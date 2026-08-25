using UnityEngine;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// 一次绑定到正式表达结果的短暂视觉位移。
    /// 它只描述表现曲线，不解释伤害、护盾或其它内容业务。
    /// </summary>
    internal sealed class ExpressionVisualImpulse
    {
        /// <summary>
        /// 回弹阶段相对首次内缩距离的比例。
        /// </summary>
        private const float ReboundFactor = 0.2f;

        /// <summary>冲量开始的游戏 tick。</summary>
        internal int StartTick { get; set; }

        /// <summary>位移的世界方向。</summary>
        internal Vector3 Direction { get; set; }

        /// <summary>首次内缩的最大距离。</summary>
        internal float Distance { get; set; }

        /// <summary>完整内缩与回弹持续的 tick 数。</summary>
        internal int DurationTicks { get; set; }

        /// <summary>
        /// 解析指定 tick 的当前视觉位移。
        /// 前半段从内缩过渡到小幅反向回弹，后半段从回弹归零。
        /// </summary>
        internal Vector3 ResolveOffset(int currentTick)
        {
            if (DurationTicks <= 0 || Distance <= 0f || Direction == Vector3.zero)
            {
                return Vector3.zero;
            }

            int elapsedTicks = currentTick - StartTick;
            if (elapsedTicks < 0 || elapsedTicks >= DurationTicks)
            {
                return Vector3.zero;
            }

            float progress = elapsedTicks / (float)DurationTicks;
            float magnitude;
            if (progress < 0.5f)
            {
                magnitude = Mathf.Lerp(Distance, -Distance * ReboundFactor, progress / 0.5f);
            }
            else
            {
                magnitude = Mathf.Lerp(
                    -Distance * ReboundFactor,
                    0f,
                    (progress - 0.5f) / 0.5f);
            }

            return Direction.normalized * magnitude;
        }

        /// <summary>判断当前冲量是否已经结束。</summary>
        internal bool IsExpired(int currentTick)
        {
            return DurationTicks <= 0 || currentTick - StartTick >= DurationTicks;
        }
    }
}
