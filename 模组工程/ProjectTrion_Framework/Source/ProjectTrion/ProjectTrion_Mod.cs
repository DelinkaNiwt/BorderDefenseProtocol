using ProjectTrion.GameParts;
using Verse;
using UnityEngine;

namespace ProjectTrion
{
    /// <summary>
    /// ProjectTrion Framework模组的主入口类。
    /// 负责模组初始化、Harmony补丁加载等。
    ///
    /// Main entry point for ProjectTrion Framework mod.
    /// Handles mod initialization, Harmony patch loading, etc.
    /// </summary>
    public class ProjectTrion_Mod : Mod
    {
        /// <summary>
        /// 模组构造函数。
        /// 在模组加载时自动调用。
        ///
        /// Mod constructor.
        /// Automatically called when mod is loaded.
        /// </summary>
        public ProjectTrion_Mod(ModContentPack content) : base(content)
        {
            // 初始化所有系统
            Initialize();
        }

        /// <summary>
        /// 初始化ProjectTrion Framework。
        /// 执行所有启动逻辑。
        ///
        /// Initialize ProjectTrion Framework.
        /// Execute all startup logic.
        /// </summary>
        private void Initialize()
        {
            // 步骤1：初始化Def生成器
            try
            {
                TrionDefGenerator.InitDefs();
                Log.Message("ProjectTrion Framework: Def生成器初始化成功");
            }
            catch (System.Exception ex)
            {
                Log.Error($"ProjectTrion Framework: Def生成器初始化失败 - {ex.Message}");
            }

            // 步骤2：输出版本信息
            Log.Message("=== ProjectTrion Framework v0.6 已加载 ===");
            Log.Message("Trion能量管理系统底层框架");
            Log.Message("应用层模组应在此框架基础上实现具体功能");
            Log.Message("Harmony补丁由应用层提供和加载");
            Log.Message("=====================================");
        }

        /// <summary>
        /// 返回模组的设置UI（可选）。
        /// Return settings UI for the mod (optional).
        /// 当前框架版本不提供设置界面。
        /// </summary>
        public override string SettingsCategory()
        {
            return "ProjectTrion Framework";
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            // 框架版本不需要设置界面
            // 所有配置都在XML中完成
        }
    }
}
