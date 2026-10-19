using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 芯片分类注册表。
    /// 外部程序集可注册具体类别提供器，Core 只负责稳定调用。
    /// </summary>
    public static class TriggerChipActivationControlRegistry
    {
        /// <summary>
        /// 当前已注册的芯片分类提供器。
        /// </summary>
        private static readonly List<ITriggerChipActivationControlProvider> providers =
            new List<ITriggerChipActivationControlProvider>();

        /// <summary>
        /// 注册一个芯片分类提供器。
        /// 同一具体类型只注册一次，避免静态启动重复挂载。
        /// </summary>
        public static void Register(ITriggerChipActivationControlProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            Type providerType = provider.GetType();
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i] != null && providers[i].GetType() == providerType)
                {
                    return;
                }
            }

            providers.Add(provider);
            BdpDiagnostics.Once(
                "trigger.chip_activation_control." + providerType.FullName,
                "Trigger 芯片分类提供器已注册: " + providerType.FullName);
        }

        /// <summary>
        /// 判断指定芯片是否拥有独立激活控制范围。
        /// 未命中任何提供器时返回 false，保持普通芯片的默认语义。
        /// </summary>
        public static bool HasIndependentActivationControl(Thing chip)
        {
            if (chip == null)
            {
                return false;
            }

            for (int i = 0; i < providers.Count; i++)
            {
                ITriggerChipActivationControlProvider provider = providers[i];
                if (provider == null)
                {
                    continue;
                }

                try
                {
                    if (provider.HasIndependentActivationControl(chip))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "[BDP] TriggerChipActivationControlRegistry provider failed: "
                        + provider.GetType().FullName + "\n" + ex);
                }
            }

            return false;
        }
    }
}
