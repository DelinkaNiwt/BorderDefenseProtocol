using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 一次芯片定义读取的统一结果。
    /// </summary>
    internal sealed class ChipDefinitionReadResult
    {
        /// <summary>
        /// 当前读取结果对应的 ThingDef。
        /// </summary>
        public ThingDef ThingDef;

        /// <summary>
        /// 当前目标的正式契约结果。
        /// </summary>
        public ChipDefinitionContract Contract;

        /// <summary>
        /// 当前目标的最低合法性校验结果。
        /// </summary>
        public ChipDefinitionValidationResult Validation;
    }
}
