using UnityEngine;

namespace BDP.Core.UI.Charts
{
    /// <summary>中性图表数据坐标点。</summary>
    public struct ChartPoint
    {
        /// <summary>横坐标。</summary>
        public readonly float X;

        /// <summary>纵坐标。</summary>
        public readonly float Y;

        /// <summary>创建数据坐标点。</summary>
        public ChartPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>转换为 Unity 二维向量。</summary>
        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }
    }
}
