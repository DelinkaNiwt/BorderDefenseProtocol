using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Core.CombatBody.External
{
    /// <summary>
    /// 战斗体前台个人外观提供器注册表。
    /// Core 只负责生命周期与异常隔离，不认识具体模板业务。
    /// </summary>
    public static class CombatBodyFrontAppearanceRegistry
    {
        /// <summary>
        /// 当前已经注册的前台外观提供器。
        /// </summary>
        private static readonly List<ICombatBodyFrontAppearanceProvider> providers =
            new List<ICombatBodyFrontAppearanceProvider>();

        /// <summary>
        /// 注册一个前台外观提供器；同一具体类型只保留一份。
        /// </summary>
        public static void Register(ICombatBodyFrontAppearanceProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            Type providerType = provider.GetType();
            for (int index = 0; index < providers.Count; index++)
            {
                ICombatBodyFrontAppearanceProvider registered = providers[index];
                if (registered != null && registered.GetType() == providerType)
                {
                    return;
                }
            }

            providers.Add(provider);
            BdpDiagnostics.Once(
                "combatbody.front_appearance_provider." + providerType.FullName,
                "战斗体前台个人外观提供器已注册: " + providerType.FullName);
        }

        /// <summary>
        /// 注销一个前台外观提供器。
        /// </summary>
        public static void Unregister(ICombatBodyFrontAppearanceProvider provider)
        {
            if (provider != null)
            {
                providers.Remove(provider);
            }
        }

        /// <summary>
        /// 尝试从已注册提供器解析指定 Pawn 的个人外观计划。
        /// </summary>
        public static bool TryResolve(
            Pawn pawn,
            out CombatBodyFrontAppearancePlan plan)
        {
            plan = null;
            if (pawn == null)
            {
                return false;
            }

            for (int index = 0; index < providers.Count; index++)
            {
                ICombatBodyFrontAppearanceProvider provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                try
                {
                    CombatBodyFrontAppearancePlan candidate;
                    if (provider.TryResolve(pawn, out candidate)
                        && candidate != null
                        && candidate.IsValid)
                    {
                        if (candidate.Entries == null)
                        {
                            candidate.Entries = new List<CombatBodyFrontAppearanceEntry>();
                        }

                        plan = candidate;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "[BDP] CombatBodyFrontAppearanceRegistry TryResolve failed: "
                        + provider.GetType().FullName + "\n" + ex);
                }
            }

            return false;
        }
    }
}
