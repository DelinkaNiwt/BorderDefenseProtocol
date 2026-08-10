using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Semantics;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 单个 emit 的正式发射记录。
    /// Fire 阶段只在这里表达每一发的差异，不直接碰 projectile 宿主。
    /// </summary>
    internal sealed class FireEmitRecord
    {
        public int EmitIndex { get; set; }

        public LocalTargetInfo Target { get; set; }

        public LocalTargetInfo SemanticTarget { get; set; }

        public Vector3 OriginOffsetWorld { get; set; }

        public Vector3 SpreadOffsetWorld { get; set; }

        public bool HasOriginSpreadRange { get; set; }

        public float OriginSpreadLateralMin { get; set; }

        public float OriginSpreadLateralMax { get; set; }

        public float OriginSpreadForwardMin { get; set; }

        public float OriginSpreadForwardMax { get; set; }

        public float SpeedFactor { get; set; }

        public float DamageFactor { get; set; }

        public ThingDef ProjectileOverride { get; set; }

        public string ResultId { get; set; }

        public string SourceResultId { get; set; }

        public FormalExpressionResult SourceResult { get; set; }

        public ISemanticContext SemanticContext { get; set; }

        public string OriginSide { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
    }
}
