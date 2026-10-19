namespace BDP.Core.UI.Charts
{
    /// <summary>图表坐标轴的数值范围。</summary>
    public struct ChartRange
    {
        /// <summary>范围下限。</summary>
        public readonly float Minimum;

        /// <summary>范围上限。</summary>
        public readonly float Maximum;

        /// <summary>创建坐标轴范围。</summary>
        public ChartRange(float minimum, float maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>判断范围是否可用于坐标换算。</summary>
        public bool IsValid
        {
            get
            {
                return !float.IsNaN(Minimum)
                    && !float.IsInfinity(Minimum)
                    && !float.IsNaN(Maximum)
                    && !float.IsInfinity(Maximum)
                    && Maximum > Minimum;
            }
        }
    }
}
