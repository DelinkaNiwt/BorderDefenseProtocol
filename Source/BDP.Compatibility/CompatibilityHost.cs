using System;
using System.Collections.Generic;
using HarmonyLib;

namespace BDP.Compatibility
{
    /// <summary>
    /// 外部兼容适配器宿主。
    /// 每个适配器独立初始化和熔断，单个外部模组不影响 BDP 正式内容；初始化保持 Idempotent（幂等）。
    /// </summary>
    public static class CompatibilityHost
    {
        /// <summary>防止宿主被多个程序集入口重复初始化。</summary>
        private static bool initialized;

        /// <summary>逐个初始化已注册适配器。</summary>
        public static void Initialize(IEnumerable<IExternalModCompatibilityAdapter> adapters)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (adapters == null)
            {
                return;
            }

            foreach (IExternalModCompatibilityAdapter adapter in adapters)
            {
                if (adapter == null)
                {
                    continue;
                }

                try
                {
                    // 每个适配器使用独立 Harmony ID（补丁标识），禁用一个外部模组时不撤销其它适配器。
                    Harmony harmony = new Harmony("niwt.bdp.compatibility." + adapter.Id);
                    string failureReason;
                    if (!adapter.TryInitialize(harmony, out failureReason))
                    {
                        if (!string.IsNullOrWhiteSpace(failureReason))
                        {
                            CompatibilityDiagnostics.Once(adapter.Id, failureReason);
                        }

                        continue;
                    }

                    CompatibilityDiagnostics.Once(adapter.Id, "已启用。");
                }
                catch (Exception exception)
                {
                    try
                    {
                        adapter.Disable("初始化异常：" + exception.GetType().Name);
                    }
                    catch
                    {
                        // 适配器自己的禁用逻辑也不得反向破坏宿主。
                    }

                    CompatibilityDiagnostics.Once(adapter.Id, "初始化异常，已禁用：" + exception.GetType().Name);
                }
            }
        }
    }
}
