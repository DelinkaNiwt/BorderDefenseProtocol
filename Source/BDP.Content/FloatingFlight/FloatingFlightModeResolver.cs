using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 浮空能力、玩家控制规则和非玩家扩展的集中模式解析入口。
    /// </summary>
    public static class FloatingFlightModeResolver
    {
        /// <summary>解析 Pawn 当前保存的个体偏好；无记录时返回关闭。</summary>
        public static FloatingFlightMode ResolvePreferredMode(Pawn pawn)
        {
            if (pawn == null || !FloatingFlightCapabilityResolver.HasCapability(pawn))
            {
                return FloatingFlightMode.Off;
            }

            FloatingFlightRecord record = FloatingFlightLedgerService.GetOrCreateRecord(pawn);
            return record == null ? FloatingFlightMode.Off : record.PreferredMode;
        }

        /// <summary>解析当前环境下真正应执行的浮空模式。</summary>
        public static FloatingFlightMode ResolveEffectiveMode(Pawn pawn)
        {
            if (pawn == null || !FloatingFlightCapabilityResolver.HasCapability(pawn))
            {
                return FloatingFlightMode.Off;
            }

            if (pawn.IsPlayerControlled)
            {
                if (!pawn.Drafted)
                {
                    return FloatingFlightMode.Off;
                }

                return ResolvePreferredMode(pawn);
            }

            return FloatingFlightNonPlayerModeProviderRegistry.TryResolveMode(
                pawn,
                out FloatingFlightMode mode)
                ? mode
                : FloatingFlightMode.Off;
        }

        /// <summary>判断当前 Pawn 是否可以新选择始终浮空。</summary>
        public static bool CanSelectAlways(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && pawn.Map != null
                && !pawn.Position.Roofed(pawn.Map);
        }

        /// <summary>向指定 Pawn 写入个体浮空偏好，并立即请求实际状态收口。</summary>
        public static bool TrySetPreferredMode(
            Pawn pawn,
            FloatingFlightMode mode,
            out string rejectionKey)
        {
            rejectionKey = null;
            if (pawn == null || !FloatingFlightCapabilityResolver.HasCapability(pawn))
            {
                rejectionKey = "BDP_Command_FloatingFlight_NoCapability";
                return false;
            }

            if (!System.Enum.IsDefined(typeof(FloatingFlightMode), mode))
            {
                rejectionKey = "BDP_Command_FloatingFlight_InvalidMode";
                return false;
            }

            if (mode == FloatingFlightMode.Always && !CanSelectAlways(pawn))
            {
                rejectionKey = "BDP_Command_FloatingFlight_AlwaysBlocked";
                return false;
            }

            FloatingFlightRecord record = FloatingFlightLedgerService.GetOrCreateRecord(pawn);
            if (record == null)
            {
                rejectionKey = "BDP_Command_FloatingFlight_Unavailable";
                return false;
            }

            record.PreferredMode = mode;
            record.GroundedForRoofSegment = false;
            FloatingFlightStateController.Reconcile(pawn, record);
            return true;
        }

        /// <summary>按关闭、仅移动、始终浮空的固定顺序循环偏好。</summary>
        public static bool CyclePreferredMode(Pawn pawn)
        {
            FloatingFlightMode current = ResolvePreferredMode(pawn);
            for (int offset = 1; offset <= 3; offset++)
            {
                FloatingFlightMode candidate = (FloatingFlightMode)(((int)current + offset) % 3);
                if (TrySetPreferredMode(pawn, candidate, out string rejectionKey))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>读取按钮左键循环时实际会尝试的下一个模式。</summary>
        public static FloatingFlightMode ResolveNextSelectableMode(Pawn pawn)
        {
            FloatingFlightMode current = ResolvePreferredMode(pawn);
            for (int offset = 1; offset <= 3; offset++)
            {
                FloatingFlightMode candidate = (FloatingFlightMode)(((int)current + offset) % 3);
                if (candidate != FloatingFlightMode.Always || CanSelectAlways(pawn))
                {
                    return candidate;
                }
            }

            return FloatingFlightMode.Off;
        }
    }
}
