using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.CombatBody.External
{
    /// <summary>
    /// 战斗体崩解扩展注册表。
    /// 外部程序集可注册具体业务，Core 只按生命周期通知提供器。
    /// </summary>
    public static class CombatBodyCollapseExtensionRegistry
    {
        /// <summary>
        /// 当前已注册的崩解扩展提供器。
        /// </summary>
        private static readonly List<ICombatBodyCollapseExtensionProvider> providers =
            new List<ICombatBodyCollapseExtensionProvider>();

        /// <summary>
        /// 注册一个崩解扩展提供器。
        /// 同一具体类型只保留一个实例，避免重复执行业务。
        /// </summary>
        public static void Register(ICombatBodyCollapseExtensionProvider provider)
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
                "combatbody.collapse_extension." + providerType.FullName,
                "战斗体崩解扩展已注册: " + providerType.FullName);
        }

        /// <summary>
        /// 通知所有扩展准备被动崩解阶段。
        /// </summary>
        public static void Prepare(Pawn pawn)
        {
            Invoke(pawn, "Prepare", delegate(ICombatBodyCollapseExtensionProvider provider, Pawn target)
            {
                provider.Prepare(target);
            });
        }

        /// <summary>
        /// 通知所有扩展执行被动崩解附加阶段。
        /// </summary>
        public static void Execute(Pawn pawn)
        {
            Invoke(pawn, "Execute", delegate(ICombatBodyCollapseExtensionProvider provider, Pawn target)
            {
                provider.Execute(target);
            });
        }

        /// <summary>
        /// 通知所有扩展清理运行状态。
        /// </summary>
        public static void Clear(Pawn pawn)
        {
            Invoke(pawn, "Clear", delegate(ICombatBodyCollapseExtensionProvider provider, Pawn target)
            {
                provider.Clear(target);
            });
        }

        /// <summary>
        /// 隔离单个提供器异常，避免外部业务阻断战斗体主链。
        /// </summary>
        private static void Invoke(
            Pawn pawn,
            string phase,
            Action<ICombatBodyCollapseExtensionProvider, Pawn> action)
        {
            for (int i = 0; i < providers.Count; i++)
            {
                ICombatBodyCollapseExtensionProvider provider = providers[i];
                if (provider == null)
                {
                    continue;
                }

                try
                {
                    action(provider, pawn);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "[BDP] CombatBodyCollapseExtensionRegistry " + phase + " failed: "
                        + provider.GetType().FullName + "\n" + ex);
                }
            }
        }
    }
}
