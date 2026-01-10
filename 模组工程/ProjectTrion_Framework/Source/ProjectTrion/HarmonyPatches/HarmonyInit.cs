using System;
using System.Reflection;
using Verse;

namespace ProjectTrion.HarmonyPatches
{
    /// <summary>
    /// Harmony补丁初始化。
    /// 使用动态反射加载Harmony，无需编译时依赖。
    ///
    /// Harmony patches initialization using dynamic reflection.
    /// No compile-time dependency on HarmonyLib required.
    /// </summary>
    public static class HarmonyInit
    {
        private const string HARMONY_ID = "ProjectTrion.Framework";

        /// <summary>
        /// 应用所有Harmony补丁。
        /// 通过反射动态加载并应用Harmony补丁。
        /// Apply all Harmony patches using reflection.
        /// </summary>
        public static void Init()
        {
            try
            {
                // 尝试通过反射加载Harmony
                var harmonyType = Type.GetType("HarmonyLib.Harmony, 0Harmony");
                if (harmonyType == null)
                {
                    Log.Warning("ProjectTrion Framework: 未能找到HarmonyLib，补丁系统不可用");
                    return;
                }

                // 创建Harmony实例
                var harmonyInstance = Activator.CreateInstance(harmonyType, HARMONY_ID);
                if (harmonyInstance == null)
                {
                    Log.Error("ProjectTrion Framework: 无法创建Harmony实例");
                    return;
                }

                // 获取PatchAll方法并调用
                var patchAllMethod = harmonyType.GetMethod("PatchAll", new Type[] { typeof(Assembly) });
                if (patchAllMethod != null)
                {
                    patchAllMethod.Invoke(harmonyInstance, new object[] { typeof(HarmonyInit).Assembly });
                }
                else
                {
                    // 尝试无参数的PatchAll
                    patchAllMethod = harmonyType.GetMethod("PatchAll", Type.EmptyTypes);
                    if (patchAllMethod != null)
                    {
                        patchAllMethod.Invoke(harmonyInstance, null);
                    }
                }

                Log.Message($"ProjectTrion Framework: Harmony补丁已应用（ID: {HARMONY_ID})");
            }
            catch (Exception ex)
            {
                Log.Error($"ProjectTrion Framework: Harmony初始化失败 - {ex.Message}");
            }
        }
    }
}
