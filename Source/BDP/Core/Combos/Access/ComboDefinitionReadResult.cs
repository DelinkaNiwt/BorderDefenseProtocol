namespace BDP.Core.Combos
{
    /// <summary>
    /// 一次组合技定义读取的统一结果。
    /// 它把原 Def、正式契约和合法性校验收在一起。
    /// </summary>
    internal sealed class ComboDefinitionReadResult
    {
        /// <summary>
        /// 当前读取结果对应的 ComboDef。
        /// </summary>
        public ComboDef ComboDef;

        /// <summary>
        /// 当前目标的正式契约结果。
        /// </summary>
        public ComboDefinitionContract Contract;

        /// <summary>
        /// 当前目标的最低合法性校验结果。
        /// </summary>
        public ComboDefinitionValidationResult Validation;
    }
}
