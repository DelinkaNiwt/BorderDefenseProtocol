using BDP.Core.Expressions.Runtime;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义对内接口获取面。
    /// 主模组内其它系统应统一从这里拿正式读取口。
    /// </summary>
    internal static class ChipSurfaceAccess
    {
        /// <summary>
        /// 单例芯片定义读取缓存。
        /// </summary>
        private static readonly ChipDefinitionCache definitionCache = new ChipDefinitionCache();

        /// <summary>
        /// 单例正式读取表面。
        /// </summary>
        private static readonly ChipDefinitionService definitionReader = new ChipDefinitionService(definitionCache);

        /// <summary>
        /// 读取芯片定义正式读取口。
        /// </summary>
        public static IChipDefinitionReader ResolveDefinitionReader()
        {
            return definitionReader;
        }

        /// <summary>
        /// 读取芯片定义正式服务。
        /// </summary>
        internal static ChipDefinitionService ResolveService()
        {
            return definitionReader;
        }

        /// <summary>
        /// 读取芯片定义共享缓存。
        /// </summary>
        internal static ChipDefinitionCache ResolveDefinitionCache()
        {
            return definitionCache;
        }

        /// <summary>
        /// 直接读取指定 ThingDef 的芯片结果。
        /// </summary>
        public static ChipDefinitionReadResult Read(ThingDef thingDef)
        {
            return definitionReader.Read(thingDef);
        }

        /// <summary>
        /// 直接读取指定 Thing 的芯片结果。
        /// </summary>
        public static ChipDefinitionReadResult Read(Thing thing)
        {
            return definitionReader.Read(thing);
        }
    }
}
