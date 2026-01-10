using Verse;

namespace ProjectTrion.GameParts
{
    /// <summary>
    /// Trion系统的Def生成器。
    /// 在游戏启动时生成必要但不在XML中定义的Def。
    ///
    /// Trion system def generator.
    /// Generate defs that don't need to be in XML.
    /// </summary>
    public static class TrionDefGenerator
    {
        /// <summary>
        /// 初始化所有必要的Def。
        /// 在模组初始化时调用。
        /// Initialize all necessary defs.
        /// </summary>
        public static void InitDefs()
        {
            // 这里可以添加运行时生成的Def
            // 例如：根据配置动态生成组件Def等

            // 当前框架不强制生成任何Def
            // 所有Def定义都在XML中完成
        }
    }
}
