using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 非玩家浮空模式提供器注册表。
    /// 首版不注册正式 AI，仅保留 Development 和未来 AI 的接入口。
    /// </summary>
    public static class FloatingFlightNonPlayerModeProviderRegistry
    {
        /// <summary>当前已注册的非玩家模式提供器。</summary>
        private static readonly List<IFloatingFlightNonPlayerModeProvider> Providers =
            new List<IFloatingFlightNonPlayerModeProvider>();

        /// <summary>注册一个非空且未重复的模式提供器。</summary>
        public static void Register(IFloatingFlightNonPlayerModeProvider provider)
        {
            if (provider != null && !Providers.Contains(provider))
            {
                Providers.Add(provider);
            }
        }

        /// <summary>注销一个已经注册的模式提供器。</summary>
        public static void Unregister(IFloatingFlightNonPlayerModeProvider provider)
        {
            if (provider != null)
            {
                Providers.Remove(provider);
            }
        }

        /// <summary>按注册顺序尝试解析一个合法非玩家模式。</summary>
        public static bool TryResolveMode(Pawn pawn, out FloatingFlightMode mode)
        {
            mode = FloatingFlightMode.Off;
            for (int index = 0; index < Providers.Count; index++)
            {
                IFloatingFlightNonPlayerModeProvider provider = Providers[index];
                if (provider == null || !provider.TryResolveMode(pawn, out FloatingFlightMode candidate))
                {
                    continue;
                }

                if (!Enum.IsDefined(typeof(FloatingFlightMode), candidate))
                {
                    continue;
                }

                mode = candidate;
                return true;
            }

            return false;
        }
    }
}
