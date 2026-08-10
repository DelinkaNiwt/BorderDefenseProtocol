using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.CombatBody.Presentation
{
    /// <summary>
    /// 战斗体宿主变换表现注册表。
    /// Core 只广播生命周期，不认识具体内容表现。
    /// </summary>
    public static class CombatBodyTransformPresentationRegistry
    {
        /// <summary>
        /// 当前已注册的表现提供器。
        /// </summary>
        private static readonly List<ICombatBodyTransformPresentationProvider> providers =
            new List<ICombatBodyTransformPresentationProvider>();

        /// <summary>
        /// 注册一个表现提供器；同一具体类型只保留一个实例。
        /// </summary>
        public static void Register(ICombatBodyTransformPresentationProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            Type providerType = provider.GetType();
            for (int index = 0; index < providers.Count; index++)
            {
                ICombatBodyTransformPresentationProvider registered = providers[index];
                if (registered != null && registered.GetType() == providerType)
                {
                    return;
                }
            }

            providers.Add(provider);
        }

        /// <summary>
        /// 反注册一个表现提供器。
        /// </summary>
        public static void Unregister(ICombatBodyTransformPresentationProvider provider)
        {
            if (provider != null)
            {
                providers.Remove(provider);
            }
        }

        /// <summary>
        /// 通知全部提供器宿主变换即将开始。
        /// 单个提供器失败不能阻断战斗体主链。
        /// </summary>
        public static void NotifyBegin(Pawn pawn, CombatBodyTransformDirection direction)
        {
            for (int index = 0; index < providers.Count; index++)
            {
                ICombatBodyTransformPresentationProvider provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                try
                {
                    provider.Begin(pawn, direction);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "[BDP] CombatBodyTransformPresentationRegistry Begin failed: "
                        + provider.GetType().FullName + "\n" + ex);
                }
            }
        }

        /// <summary>
        /// 通知全部提供器宿主真实变换已经完成。
        /// 单个提供器失败不能阻断战斗体主链。
        /// </summary>
        public static void NotifyEnd(Pawn pawn, CombatBodyTransformDirection direction)
        {
            for (int index = 0; index < providers.Count; index++)
            {
                ICombatBodyTransformPresentationProvider provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                try
                {
                    provider.End(pawn, direction);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "[BDP] CombatBodyTransformPresentationRegistry End failed: "
                        + provider.GetType().FullName + "\n" + ex);
                }
            }
        }
    }
}
