using BDP.Core.Expressions.Runtime;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技定义对内接口获取面。
    /// 主模组内其它系统应统一从这里拿正式读取口和匹配入口。
    /// </summary>
    internal static class ComboSurfaceAccess
    {
        /// <summary>
        /// 单例组合技运行时索引。
        /// </summary>
        private static readonly ComboRuntimeIndex runtimeIndex = new ComboRuntimeIndex();

        /// <summary>
        /// 单例正式读取表面。
        /// </summary>
        private static readonly ComboDefinitionService definitionReader = new ComboDefinitionService(runtimeIndex);

        /// <summary>
        /// 读取组合技定义正式读取口。
        /// </summary>
        public static IComboDefinitionReader ResolveDefinitionReader()
        {
            return definitionReader;
        }

        /// <summary>
        /// 读取组合技正式服务。
        /// </summary>
        internal static ComboDefinitionService ResolveService()
        {
            return definitionReader;
        }

        /// <summary>
        /// 读取组合技共享运行时索引。
        /// </summary>
        internal static ComboRuntimeIndex ResolveRuntimeIndex()
        {
            return runtimeIndex;
        }

        /// <summary>
        /// 读取组合技字段级契约解释器。
        /// 表达系统通过这里复用最小求值协议。
        /// </summary>
        internal static ComboDefinitionContractResolver ResolveContractResolver()
        {
            return definitionReader.ResolveContractResolver();
        }

        /// <summary>
        /// 直接读取指定 ComboDef 的正式组合技结果。
        /// </summary>
        public static ComboDefinitionReadResult Read(ComboDef comboDef)
        {
            return definitionReader.Read(comboDef);
        }

        /// <summary>
        /// 直接按 DefName 读取正式组合技结果。
        /// </summary>
        public static ComboDefinitionReadResult Read(string defName)
        {
            return definitionReader.Read(defName);
        }

        /// <summary>
        /// 按当前两枚芯片 Thing 匹配唯一命中的组合技。
        /// 芯片身份只由运行时索引读取中性有序来源键，不解释具体业务变体。
        /// </summary>
        public static ComboDefinitionReadResult FindMatch(Thing chipA, Thing chipB)
        {
            if (chipA == null || chipB == null)
            {
                return null;
            }

            return definitionReader.FindMatch(chipA, chipB);
        }
    }
}
