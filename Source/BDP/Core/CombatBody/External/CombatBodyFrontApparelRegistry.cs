using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Core.CombatBody.External
{
    /// <summary>
    /// 战斗体前台衣物准备注册表。
    /// Core 只负责广播公共准备时机，不认识具体内容规则。
    /// </summary>
    public static class CombatBodyFrontApparelRegistry
    {
        /// <summary>
        /// 当前已注册的前台衣物准备提供器。
        /// </summary>
        private static readonly List<ICombatBodyFrontApparelProvider> providers =
            new List<ICombatBodyFrontApparelProvider>();

        /// <summary>
        /// 注册一个前台衣物准备提供器；同一具体类型只保留一个实例。
        /// </summary>
        public static void Register(ICombatBodyFrontApparelProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            Type providerType = provider.GetType();
            for (int index = 0; index < providers.Count; index++)
            {
                ICombatBodyFrontApparelProvider registered = providers[index];
                if (registered != null && registered.GetType() == providerType)
                {
                    return;
                }
            }

            providers.Add(provider);
            BdpDiagnostics.Once(
                "combatbody.front_apparel_provider." + providerType.FullName,
                "战斗体前台衣物准备提供器已注册: " + providerType.FullName);
        }

        /// <summary>
        /// 注销一个前台衣物准备提供器。
        /// </summary>
        public static void Unregister(ICombatBodyFrontApparelProvider provider)
        {
            if (provider != null)
            {
                providers.Remove(provider);
            }
        }

        /// <summary>
        /// 查询所有提供器是否允许将原身衣物加入镜像前台。
        /// 没有提供器或单个提供器报错时沿用默认保留行为。
        /// </summary>
        public static bool CanMirror(Apparel apparel)
        {
            if (apparel == null)
            {
                return false;
            }

            for (int index = 0; index < providers.Count; index++)
            {
                ICombatBodyFrontApparelProvider provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                try
                {
                    if (!provider.CanMirror(apparel))
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "[BDP] CombatBodyFrontApparelRegistry CanMirror failed: "
                        + provider.GetType().FullName + "\n" + ex);
                }
            }

            return true;
        }

        /// <summary>
        /// 通知所有提供器准备一个前台衣物实例。
        /// 单个提供器失败不能阻断战斗体主链。
        /// </summary>
        public static void Prepare(Apparel apparel)
        {
            if (apparel == null)
            {
                return;
            }

            for (int index = 0; index < providers.Count; index++)
            {
                ICombatBodyFrontApparelProvider provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                try
                {
                    provider.Prepare(apparel);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "[BDP] CombatBodyFrontApparelRegistry Prepare failed: "
                        + provider.GetType().FullName + "\n" + ex);
                }
            }
        }
    }
}
