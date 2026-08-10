using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// TriggerAssembly 层内部使用的 JobDef 读取口。
    /// 它只负责把装配台入口需要的 JobDef 从 DefDatabase 取出来。
    /// </summary>
    internal static class AssemblyJobDefs
    {
        /// <summary>
        /// 使用触发器装配台 Job 的定义名。
        /// </summary>
        private const string UseTriggerAssemblerDefName = "BDP_UseTriggerAssembler";

        /// <summary>
        /// 搬运芯片到芯片仓 Job 的定义名。
        /// </summary>
        private const string HaulToChipStorageDefName = "BDP_HaulToChipStorage";

        /// <summary>
        /// 读取使用触发器装配台的 JobDef。
        /// </summary>
        internal static JobDef UseTriggerAssembler
        {
            get
            {
                return DefDatabase<JobDef>.GetNamedSilentFail(UseTriggerAssemblerDefName);
            }
        }

        /// <summary>
        /// 读取搬运芯片到芯片仓的 JobDef。
        /// </summary>
        internal static JobDef HaulToChipStorage
        {
            get
            {
                return DefDatabase<JobDef>.GetNamedSilentFail(HaulToChipStorageDefName);
            }
        }

    }
}
