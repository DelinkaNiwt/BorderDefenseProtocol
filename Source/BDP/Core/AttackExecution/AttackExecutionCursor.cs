namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击计划执行游标。
    /// 它只记录当前推进到哪一组、哪一步，不承载表达真值。
    /// </summary>
    internal sealed class AttackExecutionCursor
    {
        /// <summary>
        /// 当前游标所属的攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前推进到的执行组索引。
        /// </summary>
        public int GroupIndex { get; set; }

        /// <summary>
        /// 当前推进到的组内施放动作索引。
        /// </summary>
        public int CastIndex { get; set; }
    }
}
