using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 飞行阶段的正式结果。
    /// 它只描述 projectile 当前这一 tick 的正式飞行结论。
    /// </summary>
    internal sealed class FlightRecord : IExposable
    {
        /// <summary>
        /// 当前飞行记录所属攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前飞行记录所属正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前飞行记录对应的 emit 序号。
        /// </summary>
        public int EmitIndex { get; set; }

        /// <summary>
        /// 当前飞行记录的连续 tick 序号。
        /// </summary>
        public int FlightId { get; set; }

        /// <summary>
        /// 当前飞行记录沿用的瞄准目标。
        /// </summary>
        public LocalTargetInfo AimTarget { get; set; }

        /// <summary>
        /// 当前飞行记录追踪的正式目标。
        /// </summary>
        public LocalTargetInfo CurrentTarget { get; set; }

        /// <summary>
        /// 当前飞行记录最终朝向的世界坐标。
        /// </summary>
        public Vector3 CurrentDestination { get; set; }

        /// <summary>
        /// 当前飞行记录本 tick 是否声明了新的重定向坐标。
        /// </summary>
        public Vector3? RedirectDestination { get; set; }

        /// <summary>
        /// 当前飞行记录裁定后的速度倍率。
        /// </summary>
        public float SpeedFactor { get; set; }

        /// <summary>
        /// 当前飞行记录裁定后的伤害倍率。
        /// </summary>
        public float DamageFactor { get; set; }

        /// <summary>
        /// 当前飞行记录本 tick 是否接收到任意协议意图。
        /// </summary>
        public bool HasIntentThisTick { get; set; }

        /// <summary>
        /// 当前飞行记录是否要求进入下一段飞行。
        /// </summary>
        public bool ContinueFlight { get; set; }

        /// <summary>
        /// 当前飞行记录附带的标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 统一序列化当前飞行记录。
        /// </summary>
        public void ExposeData()
        {
            string attackInstanceId = AttackInstanceId;
            string resultId = ResultId;
            int emitIndex = EmitIndex;
            int flightId = FlightId;
            LocalTargetInfo aimTarget = AimTarget;
            LocalTargetInfo currentTarget = CurrentTarget;
            Vector3 currentDestination = CurrentDestination;
            bool hasRedirectDestination = RedirectDestination.HasValue;
            Vector3 redirectDestination = RedirectDestination ?? default;
            float speedFactor = SpeedFactor;
            float damageFactor = DamageFactor;
            bool hasIntentThisTick = HasIntentThisTick;
            bool continueFlight = ContinueFlight;
            List<string> tags = Tags;

            Scribe_Values.Look(ref attackInstanceId, "attackInstanceId");
            Scribe_Values.Look(ref resultId, "resultId");
            Scribe_Values.Look(ref emitIndex, "emitIndex", 0);
            Scribe_Values.Look(ref flightId, "flightId", 0);
            Scribe_TargetInfo.Look(ref aimTarget, "aimTarget");
            Scribe_TargetInfo.Look(ref currentTarget, "currentTarget");
            Scribe_Values.Look(ref currentDestination, "currentDestination");
            Scribe_Values.Look(ref hasRedirectDestination, "hasRedirectDestination", false);
            Scribe_Values.Look(ref redirectDestination, "redirectDestination");
            Scribe_Values.Look(ref speedFactor, "speedFactor", 1f);
            Scribe_Values.Look(ref damageFactor, "damageFactor", 1f);
            Scribe_Values.Look(ref hasIntentThisTick, "hasIntentThisTick", false);
            Scribe_Values.Look(ref continueFlight, "continueFlight", false);
            Scribe_Collections.Look(ref tags, "tags", LookMode.Value);

            AttackInstanceId = attackInstanceId;
            ResultId = resultId;
            EmitIndex = emitIndex;
            FlightId = flightId;
            AimTarget = aimTarget;
            CurrentTarget = currentTarget;
            CurrentDestination = currentDestination;
            RedirectDestination = hasRedirectDestination ? redirectDestination : (Vector3?)null;
            SpeedFactor = speedFactor;
            DamageFactor = damageFactor;
            HasIntentThisTick = hasIntentThisTick;
            ContinueFlight = continueFlight;
            Tags = tags ?? new List<string>();
        }
    }
}
