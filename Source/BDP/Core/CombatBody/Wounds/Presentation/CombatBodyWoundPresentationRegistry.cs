using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.CombatBody.Wounds.Presentation
{
    /// <summary>
    /// 战斗体伤口表现扩展注册表。
    /// Core 只按伤口生命周期通知提供器，不认识具体表现业务。
    /// </summary>
    public static class CombatBodyWoundPresentationRegistry
    {
        /// <summary>
        /// 已注册的伤口表现提供器列表。
        /// </summary>
        private static readonly List<ICombatBodyWoundPresentationProvider> providers =
            new List<ICombatBodyWoundPresentationProvider>();

        /// <summary>
        /// 注册一个伤口表现提供器。
        /// 同一具体类型只保留一个实例，避免重复执行表现业务。
        /// </summary>
        public static void Register(ICombatBodyWoundPresentationProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            Type providerType = provider.GetType();
            for (int index = 0; index < providers.Count; index++)
            {
                if (providers[index] != null && providers[index].GetType() == providerType)
                {
                    return;
                }
            }

            providers.Add(provider);
        }

        /// <summary>
        /// 反注册一个伤口表现提供器。
        /// </summary>
        public static void Unregister(ICombatBodyWoundPresentationProvider provider)
        {
            if (provider != null)
            {
                providers.Remove(provider);
            }
        }

        /// <summary>
        /// 保存全部提供器的表现状态。
        /// </summary>
        public static void ExposeData(Pawn pawn)
        {
            Invoke("ExposeData", delegate(ICombatBodyWoundPresentationProvider provider)
            {
                provider.ExposeData(pawn);
            });
        }

        /// <summary>
        /// 清除全部提供器的表现状态。
        /// </summary>
        public static void ClearAll(Pawn pawn)
        {
            Invoke("ClearAll", delegate(ICombatBodyWoundPresentationProvider provider)
            {
                provider.ClearAll(pawn);
            });
        }

        /// <summary>
        /// 通知全部提供器一个伤口进入有效生命周期。
        /// </summary>
        public static void NotifyWoundAdded(Pawn pawn, Hediff hediff)
        {
            Invoke("NotifyWoundAdded", delegate(ICombatBodyWoundPresentationProvider provider)
            {
                provider.NotifyWoundAdded(pawn, hediff);
            });
        }

        /// <summary>
        /// 通知全部提供器一个伤口的派生生命周期结束。
        /// </summary>
        public static void NotifyWoundDrainExpired(Pawn pawn, int hediffLoadId)
        {
            Invoke("NotifyWoundDrainExpired", delegate(ICombatBodyWoundPresentationProvider provider)
            {
                provider.NotifyWoundDrainExpired(pawn, hediffLoadId);
            });
        }

        /// <summary>
        /// 通知全部提供器一个伤口被移除。
        /// </summary>
        public static void NotifyWoundRemoved(Pawn pawn, Hediff hediff)
        {
            Invoke("NotifyWoundRemoved", delegate(ICombatBodyWoundPresentationProvider provider)
            {
                provider.NotifyWoundRemoved(pawn, hediff);
            });
        }

        /// <summary>
        /// 按当前活跃伤口标识通知全部提供器重建表现状态。
        /// </summary>
        public static void RebuildFromActiveDrains(Pawn pawn, IEnumerable<int> activeHediffLoadIds)
        {
            List<int> activeIds = activeHediffLoadIds != null
                ? new List<int>(activeHediffLoadIds)
                : null;
            Invoke("RebuildFromActiveDrains", delegate(ICombatBodyWoundPresentationProvider provider)
            {
                provider.RebuildFromActiveDrains(pawn, activeIds);
            });
        }

        /// <summary>
        /// 推进全部提供器的表现运行时。
        /// </summary>
        public static void Tick(Pawn pawn)
        {
            Invoke("Tick", delegate(ICombatBodyWoundPresentationProvider provider)
            {
                provider.Tick(pawn);
            });
        }

        /// <summary>
        /// 隔离单个提供器异常，避免视觉业务阻断伤口主链。
        /// </summary>
        private static void Invoke(string phase, Action<ICombatBodyWoundPresentationProvider> action)
        {
            for (int index = 0; index < providers.Count; index++)
            {
                ICombatBodyWoundPresentationProvider provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                try
                {
                    action(provider);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "[BDP] CombatBodyWoundPresentationRegistry " + phase + " failed: "
                        + provider.GetType().FullName + "\n" + ex);
                }
            }
        }
    }
}
