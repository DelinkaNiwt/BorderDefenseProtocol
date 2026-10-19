using System;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.GroundDash
{
    /// <summary>
    /// 中性地面冲刺飞行器。
    ///
    /// 只负责 PawnFlyer 的贴地线性绘制、单调进度曲线和落地回调；
    /// 具体能力的伤害、推挤和特效由业务子类通过钩子实现。
    /// </summary>
    public class PawnFlyer_GroundDash : PawnFlyer
    {
        /// <summary>飞行器落地后执行的一次性回调。</summary>
        public Action onLanded;

        /// <summary>冲刺起点的高度偏移。</summary>
        public float startHeight;

        /// <summary>冲刺终点的高度偏移。</summary>
        public float targetHeight;

        /// <summary>缓存的飞行进度曲线。</summary>
        private SimpleCurve cachedProgressCurve;

        /// <summary>是否已经尝试读取飞行进度曲线。</summary>
        private bool curveResolved;

        /// <summary>上一帧用于逐段扩展通知的地图格。</summary>
        private IntVec3 lastDashCell = IntVec3.Invalid;

        /// <summary>原版 PawnFlyer 私有目标格字段。</summary>
        private static readonly FieldInfo DestinationCellField = typeof(PawnFlyer).GetField(
            "destCell",
            BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>飞行器绘制所在的固定贴地高度。</summary>
        protected float FlatAltitude => AltitudeLayer.Skyfaller.AltitudeFor();

        /// <summary>
        /// 读取原版 PawnFlyerProperties 的私有 SimpleCurve。
        /// 原版字段没有公开访问器，因此只在中性设施内集中处理一次反射。
        /// </summary>
        private SimpleCurve GetProgressCurve()
        {
            if (!curveResolved)
            {
                curveResolved = true;
                FieldInfo field = typeof(PawnFlyerProperties).GetField(
                    "progressCurve",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    cachedProgressCurve = field.GetValue(def.pawnFlyer) as SimpleCurve;
                }
                else
                {
                    Log.Warning("[BDP] GroundDash could not read PawnFlyerProperties.progressCurve.");
                }
            }
            return cachedProgressCurve;
        }

        /// <summary>
        /// 使用原版 SimpleCurve 评估进度，绕过 AnimationCurve 的过冲插值。
        /// </summary>
        protected float EvaluateProgress(float time)
        {
            SimpleCurve curve = GetProgressCurve();
            if (curve == null || curve.PointsCount == 0)
            {
                return time;
            }
            return curve.Evaluate(time);
        }

        /// <summary>按冲刺起点与终点计算单调贴地位置。</summary>
        protected Vector3 GetLinearPosition()
        {
            float flightTime = Mathf.Max(1, ticksFlightTime);
            float time = (float)ticksFlying / flightTime;
            float progress = EvaluateProgress(time);
            Vector3 result = Vector3.Lerp(startVec, DestinationPos, progress);
            result.y = FlatAltitude;
            result.z += Mathf.Lerp(startHeight, targetHeight, time);
            return result;
        }

        /// <summary>飞行器当前绘制位置。</summary>
        public override Vector3 DrawPos => GetLinearPosition();

        /// <summary>
        /// 为沿途伤害或其它业务碰撞提供逐段扩展点。
        /// 基类不解释路径上的 Pawn、建筑或其它 Thing。
        /// </summary>
        protected virtual void OnDashSegment(IntVec3 from, IntVec3 to) { }

        /// <summary>
        /// 将本次冲刺提前结束在指定格。
        /// 原版 PawnFlyer 的目标格是私有字段，中性设施集中封装这一处必要反射。
        /// </summary>
        protected void LandAtCell(IntVec3 cell)
        {
            if (DestinationCellField == null || !cell.IsValid)
            {
                return;
            }

            DestinationCellField.SetValue(this, cell);
            ticksFlying = ticksFlightTime;
        }

        /// <summary>使用贴地冲刺位置绘制 Pawn 的动态阶段。</summary>
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            if (FlyingPawn != null)
            {
                FlyingPawn.DynamicDrawPhaseAt(phase, DrawPos);
            }
        }

        /// <summary>使用贴地冲刺位置绘制阴影和携带物。</summary>
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DrawShadow(DrawPos, FlyingPawn != null ? FlyingPawn.BodySize : 1f);

            if (CarriedThing != null && FlyingPawn != null)
            {
                PawnRenderUtility.DrawCarriedThing(FlyingPawn, DrawPos, CarriedThing);
            }
        }

        /// <summary>推进飞行器、通知业务碰撞扩展并执行原版飞行 Tick。</summary>
        protected override void Tick()
        {
            IntVec3 currentCell = DrawPos.ToIntVec3();
            if (lastDashCell.IsValid && Map != null)
            {
                OnDashSegment(lastDashCell, currentCell);
            }
            lastDashCell = currentCell;
            base.Tick();
        }

        /// <summary>按单调进度绘制收缩阴影。</summary>
        private void DrawShadow(Vector3 drawLoc, float size)
        {
            if (def.pawnFlyer.ShadowMaterial == null)
            {
                return;
            }

            float flightTime = Mathf.Max(1, ticksFlightTime);
            float time = (float)ticksFlying / flightTime;
            float progress = EvaluateProgress(time);
            Vector3 position = Vector3.Lerp(startVec, DestinationPos, progress);
            position.y = AltitudeLayer.Shadows.AltitudeFor();

            float scale = 1f - Mathf.Sin(time * Mathf.PI) * 0.3f;
            Matrix4x4 matrix = Matrix4x4.TRS(
                position,
                Quaternion.identity,
                new Vector3(size * scale, 1f, size * scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, def.pawnFlyer.ShadowMaterial, 0);
        }

        /// <summary>单位落地后执行一次通用回调。</summary>
        protected override void RespawnPawn()
        {
            base.RespawnPawn();

            Action callback = onLanded;
            onLanded = null;
            if (callback != null)
            {
                callback.Invoke();
            }
        }

        /// <summary>保存和加载高度偏移。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startHeight, "startHeight", 0f);
            Scribe_Values.Look(ref targetHeight, "targetHeight", 0f);
        }
    }
}
