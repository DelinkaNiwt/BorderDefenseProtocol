using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片功能属性的精确语义定义。
    /// </summary>
    public sealed class ChipFeaturePropertyDef : Def
    {
        /// <summary>属性的稳定值类型。</summary>
        public ChipInformationValueKind ValueKind;

        /// <summary>可选单位语言键。</summary>
        public string UnitKey;
    }
}
