namespace BDP.Core.Trion
{
    /// <summary>
    /// Trion 当前值调试写入结果。
    /// 它只表达这次正式写请求的结果，不扩展成通用事务系统。
    /// </summary>
    public sealed class TrionCurrentWriteResult
    {
        public bool Succeeded { get; set; }

        public bool WasClamped { get; set; }

        public float PreviousCurrent { get; set; }

        public float Current { get; set; }

        public string Message { get; set; }
    }
}
