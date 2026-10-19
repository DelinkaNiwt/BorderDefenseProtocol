using System.Collections.Generic;
using BDP.Content.FloatingFlight;
using Verse;

namespace BDP.Development.FloatingFlight
{
    /// <summary>
    /// Development 临时非玩家模式提供器。
    /// 它只为调试 AI 预留模式注入缝隙，不写入正式存档，也不承载 AI 决策。
    /// </summary>
    public sealed class FloatingFlightNonPlayerModeProvider : IFloatingFlightNonPlayerModeProvider
    {
        /// <summary>开发器内唯一的临时模式提供器实例。</summary>
        public static readonly FloatingFlightNonPlayerModeProvider Instance =
            new FloatingFlightNonPlayerModeProvider();

        /// <summary>按 Pawn ThingID 保存的临时模式覆盖。</summary>
        private readonly Dictionary<string, FloatingFlightMode> modesByThingId =
            new Dictionary<string, FloatingFlightMode>();

        /// <summary>读取指定非玩家 Pawn 的临时模式覆盖。</summary>
        public bool TryResolveMode(Pawn pawn, out FloatingFlightMode mode)
        {
            mode = FloatingFlightMode.Off;
            string thingId = pawn?.ThingID;
            return !string.IsNullOrWhiteSpace(thingId)
                && modesByThingId.TryGetValue(thingId, out mode);
        }

        /// <summary>写入一个临时 Pawn 个体模式。</summary>
        public void SetMode(Pawn pawn, FloatingFlightMode mode)
        {
            if (pawn == null || string.IsNullOrWhiteSpace(pawn.ThingID))
            {
                return;
            }

            modesByThingId[pawn.ThingID] = mode;
        }

        /// <summary>清除一个 Pawn 的临时模式覆盖，使其回到正式默认关闭。</summary>
        public void ClearMode(Pawn pawn)
        {
            if (pawn != null && !string.IsNullOrWhiteSpace(pawn.ThingID))
            {
                modesByThingId.Remove(pawn.ThingID);
            }
        }
    }
}
