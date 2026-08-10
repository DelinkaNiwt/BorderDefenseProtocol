using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义正式读取口。
    /// 主模组内部其它系统应统一从这里读芯片声明结果。
    /// </summary>
    internal interface IChipDefinitionReader
    {
        /// <summary>
        /// 读取指定 ThingDef 的芯片定义结果。
        /// </summary>
        ChipDefinitionReadResult Read(ThingDef thingDef);

        /// <summary>
        /// 读取指定 Thing 的芯片定义结果。
        /// </summary>
        ChipDefinitionReadResult Read(Thing thing);
    }
}
