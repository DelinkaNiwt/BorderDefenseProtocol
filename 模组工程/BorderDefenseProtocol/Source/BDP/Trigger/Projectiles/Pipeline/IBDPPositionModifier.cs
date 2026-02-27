using UnityEngine;

namespace BDP.Trigger
{
    /// <summary>
    /// 位置修饰数据包——修改显示坐标（不影响逻辑位置）。
    /// 用于抛物线弧度、抖动等视觉效果。
    /// </summary>
    public struct PositionContext
    {
        // ── 输入（只读）──
        /// <summary>逻辑位置（引擎插值计算结果）。</summary>
        public readonly Vector3 LogicalPosition;

        /// <summary>飞行进度（0=刚发射，1=到达目标）。用于抛物线等需要进度的效果。</summary>
        public readonly float Progress;

        // ── 可修改 ──
        /// <summary>显示位置（模块可修改以添加视觉偏移）。</summary>
        public Vector3 DrawPosition;

        public PositionContext(Vector3 logicalPosition, float progress)
        {
            LogicalPosition = logicalPosition;
            Progress = progress;
            DrawPosition = logicalPosition;
        }
    }

    /// <summary>
    /// 位置修饰管线接口——修改显示坐标（抛物线/抖动）。
    /// 执行顺序：管线第4阶段（拦截检查之后）。
    /// </summary>
    public interface IBDPPositionModifier
    {
        /// <summary>修改显示位置。</summary>
        void ModifyPosition(Bullet_BDP host, ref PositionContext ctx);
    }
}
