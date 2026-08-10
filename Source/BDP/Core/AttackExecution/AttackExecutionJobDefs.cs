using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// AttackExecution 层内部使用的 JobDef 读取口。
    /// 它只负责把正式执行层需要的 JobDef 从 DefDatabase 取出来。
    /// </summary>
    internal static class AttackExecutionJobDefs
    {
        /// <summary>
        /// 远程持续攻击推进 Job 的定义名。
        /// </summary>
        private const string RangedAttackExecutionDefName = "BDP_RangedAttackExecution";

        /// <summary>
        /// 近战持续攻击推进 Job 的定义名。
        /// </summary>
        private const string MeleeAttackExecutionDefName = "BDP_MeleeAttackExecution";

        /// <summary>
        /// 读取远程持续攻击推进 JobDef。
        /// </summary>
        internal static JobDef RangedAttackExecution
        {
            get
            {
                return DefDatabase<JobDef>.GetNamedSilentFail(RangedAttackExecutionDefName);
            }
        }

        /// <summary>
        /// 读取近战持续攻击推进 JobDef。
        /// </summary>
        internal static JobDef MeleeAttackExecution
        {
            get
            {
                return DefDatabase<JobDef>.GetNamedSilentFail(MeleeAttackExecutionDefName);
            }
        }
    }
}
