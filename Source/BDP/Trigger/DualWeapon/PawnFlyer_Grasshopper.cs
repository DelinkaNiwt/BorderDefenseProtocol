using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 蚱蜢飞行器 — 直线贴地弹射
    ///
    /// 原版PawnFlyerWorker.AdjustedProgress()内部将SimpleCurve转为
    /// Unity AnimationCurve（三次贝塞尔插值），在斜率剧变处会产生
    /// 过冲(overshoot)，导致"飞过去又退回来"。
    ///
    /// 解决方案：绕过AnimationCurve，直接用SimpleCurve.Evaluate()
    /// 做线性插值，保证严格单调递增。
    /// </summary>
    public class PawnFlyer_Grasshopper : PawnFlyer
    {
        // 视觉效果控制
        private int lastTrailTick = -1;  // 上次生成拖尾的tick
        private bool launchEffectSpawned = false;  // 起跳特效是否已生成
        // 固定飞行高度
        private float FlatAltitude => AltitudeLayer.Skyfaller.AltitudeFor();

        // 起始和目标高度（用于Z轴偏移）
        public float startHeight = 0f;
        public float targetHeight = 0f;

        // 缓存：从PawnFlyerProperties反射获取的原始SimpleCurve
        private SimpleCurve cachedProgressCurve;
        private bool curveResolved;

        /// <summary>
        /// 获取原始SimpleCurve（绕过AnimationCurve转换）。
        /// PawnFlyerProperties.progressCurve是private字段，需要反射读取。
        /// </summary>
        private SimpleCurve GetProgressCurve()
        {
            if (!curveResolved)
            {
                curveResolved = true;
                FieldInfo fi = typeof(PawnFlyerProperties).GetField(
                    "progressCurve",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null)
                {
                    cachedProgressCurve = fi.GetValue(def.pawnFlyer) as SimpleCurve;
                    Log.Message($"[Grasshopper] ProgressCurve resolved: {(cachedProgressCurve != null ? $"OK, {cachedProgressCurve.PointsCount} points" : "NULL")}");
                }
                else
                {
                    // 列出所有字段名，帮助诊断
                    var fields = typeof(PawnFlyerProperties).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                    Log.Warning($"[Grasshopper] Field 'progressCurve' not found! All fields: {string.Join(", ", System.Array.ConvertAll(fields, f => f.Name))}");
                }
            }
            return cachedProgressCurve;
        }

        /// <summary>
        /// 用SimpleCurve线性插值计算调整进度，保证严格单调。
        /// </summary>
        private float EvaluateProgress(float t)
        {
            SimpleCurve curve = GetProgressCurve();
            if (curve == null || curve.PointsCount == 0)
                return t;
            return curve.Evaluate(t);
        }

        /// <summary>
        /// 重写DrawPos - 使用线性位置计算（直线冲刺）
        /// </summary>
        public override Vector3 DrawPos => GetLinearPosition();

        /// <summary>
        /// 计算线性飞行位置 - 使用SimpleCurve线性插值（无过冲）
        /// </summary>
        private Vector3 GetLinearPosition()
        {
            float num = Mathf.Max(1, ticksFlightTime);
            float t = (float)ticksFlying / num;

            // 使用SimpleCurve线性插值，避免AnimationCurve三次贝塞尔过冲
            float t2 = EvaluateProgress(t);

            Vector3 result = Vector3.Lerp(startVec, DestinationPos, t2);

            // 计算高度偏移
            float num2 = Mathf.Lerp(startHeight, targetHeight, t);

            // 设置固定高度
            result.y = FlatAltitude;
            result.z += num2;

            // 诊断日志
            if (ticksFlying % 5 == 0)
            {
                Log.Message($"[Grasshopper] Tick={ticksFlying}/{ticksFlightTime}(speed={def.pawnFlyer.flightSpeed}), t={t:F3}, t2={t2:F3}, DistToDest={Vector3.Distance(new Vector3(result.x,0,result.z), new Vector3(DestinationPos.x,0,DestinationPos.z)):F2}");
            }

            return result;
        }

        /// <summary>
        /// 重写DynamicDrawPhaseAt - 使用线性位置
        /// </summary>
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            if (FlyingPawn != null)
            {
                FlyingPawn.DynamicDrawPhaseAt(phase, DrawPos);
            }
        }

        /// <summary>
        /// 重写DrawAt - 使用线性位置绘制阴影
        /// </summary>
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DrawShadow(DrawPos, FlyingPawn?.BodySize ?? 1f);

            if (CarriedThing != null && FlyingPawn != null)
            {
                PawnRenderUtility.DrawCarriedThing(FlyingPawn, DrawPos, CarriedThing);
            }
        }

        /// <summary>
        /// 每帧Tick - 生成飞行拖尾特效
        /// </summary>
        protected override void Tick()
        {
            base.Tick();

            // 起跳时在脚下生成平台特效（只触发一次）
            if (!launchEffectSpawned && ticksFlying >= 1)
            {
                launchEffectSpawned = true;

                // 计算脚下位置（向下偏移0.3格）
                Vector3 footPos = new Vector3(
                    startVec.x,
                    AltitudeLayer.Shadows.AltitudeFor(),
                    startVec.z - 0.3f  // Z轴向下偏移，让踏板在脚下
                );

                // 生成主踏板（稍微放大）
                FleckMaker.Static(footPos, Map, Core.BDP_DefOf.BDP_GrasshopperPlatform, 0.7f);

                // 生成2个扩散波纹，错开时间产生连续脉冲效果（明显更大，增强跃动感）
                for (int i = 0; i < 2; i++)
                {
                    FleckCreationData data = FleckMaker.GetDataStatic(
                        footPos,
                        Map,
                        Core.BDP_DefOf.BDP_GrasshopperPulse,
                        1.2f
                    );
                    data.solidTimeOverride = 0.03f + i * 0.05f;  // 错开出现时间
                    Map.flecks.CreateFleck(data);
                }
            }

            // 每3tick生成一个拖尾粒子
            if (ticksFlying - lastTrailTick >= 3)
            {
                lastTrailTick = ticksFlying;
                FleckMaker.ThrowDustPuffThick(DrawPos, Map, 0.6f, new Color(0.2f, 1f, 0.6f, 0.5f));
            }
        }

        /// <summary>
        /// 绘制阴影（也使用SimpleCurve线性插值）
        /// </summary>
        private void DrawShadow(Vector3 drawLoc, float size)
        {
            if (def.pawnFlyer.ShadowMaterial == null)
                return;

            float num = Mathf.Max(1, ticksFlightTime);
            float num2 = (float)ticksFlying / num;

            float t2 = EvaluateProgress(num2);
            Vector3 pos = Vector3.Lerp(startVec, DestinationPos, t2);
            pos.y = AltitudeLayer.Shadows.AltitudeFor();

            float num3 = 1f - Mathf.Sin(num2 * Mathf.PI) * 0.3f;
            Matrix4x4 matrix = Matrix4x4.TRS(
                pos: pos,
                q: Quaternion.identity,
                s: new Vector3(size * num3, 1f, size * num3)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, def.pawnFlyer.ShadowMaterial, 0);
        }

        /// <summary>
        /// 保存/加载数据
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startHeight, "startHeight", 0f);
            Scribe_Values.Look(ref targetHeight, "targetHeight", 0f);
        }
    }
}
