using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 固定装载实体芯片工厂注册表。
    /// Core只编排通用工厂调用，具体芯片成品由外部程序集提供。
    /// </summary>
    public static class TriggerFixedLoadoutFactoryRegistry
    {
        /// <summary>
        /// 当前已注册的固定装载成品工厂，按注册顺序尝试。
        /// </summary>
        private static readonly List<ITriggerFixedLoadoutFactory> factories =
            new List<ITriggerFixedLoadoutFactory>();

        /// <summary>
        /// 注册一个固定装载成品工厂。
        /// 同一具体类型只注册一次，避免启动期重复接线。
        /// </summary>
        public static void Register(ITriggerFixedLoadoutFactory factory)
        {
            if (factory == null)
            {
                return;
            }

            Type factoryType = factory.GetType();
            for (int index = 0; index < factories.Count; index++)
            {
                ITriggerFixedLoadoutFactory existing = factories[index];
                if (existing != null && existing.GetType() == factoryType)
                {
                    return;
                }
            }

            factories.Add(factory);
            BdpDiagnostics.Once(
                "trigger.fixed_loadout_factory." + factoryType.FullName,
                "触发体固定装载成品工厂已注册: " + factoryType.FullName);
        }

        /// <summary>
        /// 按实体芯片Def和初始化键尝试创建完整实体芯片。
        /// 单个工厂异常时隔离异常并继续尝试后续工厂。
        /// </summary>
        public static bool TryCreate(
            ThingDef chipDef,
            string initializationKey,
            out Thing chip)
        {
            chip = null;
            if (chipDef == null || string.IsNullOrWhiteSpace(initializationKey))
            {
                return false;
            }

            for (int index = 0; index < factories.Count; index++)
            {
                ITriggerFixedLoadoutFactory factory = factories[index];
                if (factory == null)
                {
                    continue;
                }

                try
                {
                    Thing candidate;
                    if (factory.TryCreate(chipDef, initializationKey, out candidate)
                        && candidate != null)
                    {
                        chip = candidate;
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(
                        "[BDP] TriggerFixedLoadoutFactoryRegistry factory failed: "
                        + factory.GetType().FullName
                        + "\n"
                        + exception);
                }
            }

            return false;
        }
    }
}
