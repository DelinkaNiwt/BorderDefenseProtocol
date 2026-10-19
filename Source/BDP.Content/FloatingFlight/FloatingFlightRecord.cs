using System;
using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 一个 Pawn 的浮空偏好和视觉过渡账本记录。
    /// </summary>
    public sealed class FloatingFlightRecord : IExposable
    {
        /// <summary>持久引用的 Pawn。</summary>
        public Pawn Pawn;

        /// <summary>Pawn 的稳定 ThingID 回退值。</summary>
        public string PawnThingId;

        /// <summary>该 Pawn 的三态浮空偏好，首次建立时关闭。</summary>
        public FloatingFlightMode PreferredMode = FloatingFlightMode.Off;

        /// <summary>当前线性过渡的视觉高度起点。</summary>
        public float TransitionStartFactor;

        /// <summary>当前线性过渡起点对应的游戏 tick。</summary>
        public int TransitionStartTick;

        /// <summary>是否正在等待移动到可安全降落的格子。</summary>
        public bool PendingSafeLanding;

        /// <summary>
        /// 是否正在从露天飞行交接到屋顶地面段。
        /// 这是运行时交接标记，不是第三种飞行状态，也不作为实际飞行真值。
        /// </summary>
        public bool GroundedForRoofSegment;

        /// <summary>供原版深度存档和反序列化使用的无参构造函数。</summary>
        public FloatingFlightRecord()
        {
        }

        /// <summary>为指定 Pawn 建立默认关闭浮空的记录。</summary>
        public FloatingFlightRecord(Pawn pawn)
        {
            BindPawn(pawn);
        }

        /// <summary>绑定 Pawn，同时保留稳定 ThingID。</summary>
        public void BindPawn(Pawn pawn)
        {
            Pawn = pawn;
            if (pawn != null && !string.IsNullOrWhiteSpace(pawn.ThingID))
            {
                PawnThingId = pawn.ThingID;
            }
        }

        /// <summary>判断记录是否属于指定 Pawn。</summary>
        public bool Matches(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (Pawn == pawn)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(PawnThingId)
                && !string.IsNullOrWhiteSpace(pawn.ThingID)
                && string.Equals(PawnThingId, pawn.ThingID, StringComparison.Ordinal);
        }

        /// <summary>规范化读档或外部写入后的记录值。</summary>
        public void Normalize()
        {
            if (!Enum.IsDefined(typeof(FloatingFlightMode), PreferredMode))
            {
                PreferredMode = FloatingFlightMode.Off;
            }

            if (float.IsNaN(TransitionStartFactor)
                || float.IsInfinity(TransitionStartFactor))
            {
                TransitionStartFactor = 0f;
            }

            TransitionStartFactor = Math.Max(0f, Math.Min(1f, TransitionStartFactor));
        }

        /// <summary>保存或读取 Pawn、偏好和视觉过渡字段。</summary>
        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "pawn");
            Scribe_Values.Look(ref PawnThingId, "pawnThingId");
            Scribe_Values.Look(ref PreferredMode, "preferredMode", FloatingFlightMode.Off);
            Scribe_Values.Look(ref TransitionStartFactor, "transitionStartFactor", 0f);
            Scribe_Values.Look(ref TransitionStartTick, "transitionStartTick", 0);
            Scribe_Values.Look(ref PendingSafeLanding, "pendingSafeLanding", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Pawn != null && string.IsNullOrWhiteSpace(PawnThingId))
                {
                    PawnThingId = Pawn.ThingID;
                }

                Normalize();
            }
        }
    }
}
