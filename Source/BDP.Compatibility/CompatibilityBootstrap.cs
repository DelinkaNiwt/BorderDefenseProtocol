using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using BDP.Compatibility.NiceInventoryTab;
using BDP.Compatibility.FlashingBlades;

namespace BDP.Compatibility
{
    /// <summary>
    /// 外部模组兼容程序集的启动入口。
    /// 具体适配器会在宿主就绪后逐个发现和安装。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CompatibilityBootstrap
    {
        /// <summary>防止多个启动入口重复初始化兼容宿主。</summary>
        private static bool initialized;

        /// <summary>程序集加载后立即尝试安装可选外部兼容适配器。</summary>
        static CompatibilityBootstrap()
        {
            Initialize();
        }

        /// <summary>由程序集加载器调用的幂等启动入口。</summary>
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            // 兼容适配器只从已加载程序集运行时发现外部模组，不产生编译时依赖。
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            CompatibilityHost.Initialize(new IExternalModCompatibilityAdapter[]
            {
                new NiceInventoryTabAdapter(loadedAssemblies),
                new FlashingBladesAdapter(loadedAssemblies)
            });
        }
    }
}
