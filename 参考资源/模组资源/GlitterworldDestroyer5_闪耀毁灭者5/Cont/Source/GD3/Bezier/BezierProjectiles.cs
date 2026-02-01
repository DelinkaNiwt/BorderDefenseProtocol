using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GD3
{
    public class BezierProjectiles : Projectile
    {
        // 是否使用三阶贝塞尔曲线 如果不开启则使用二阶
        private bool useCubieCurve = false;

        // 用于计算控制点位置的方向依据
        public Vector3 launchDirNormalized;

        // 二阶贝塞尔的扰动控制项，随机偏移的状态变量
        private Vector3 randOffset;
        private bool randomInitialized = false;
        private float randomSideDirection = 1f; // 用于存储本次发射的随机方向

        // 上一帧的位置，用于轨迹尾迹粒子绘制
        private Vector3 lastPosition;

        // 控制贝塞尔轨迹的曲率范围与弯曲程度。
        private FloatRange randOffsetRange;
        private float controlPointFactor;

        // 三阶贝塞尔控制用字段
        private float P1Offset, P1Side, P2Offset, P2Side;

        // 三阶DNA螺旋效果的控制变量
        private bool enableRandomSide;
        private float randomSideChance;

        // 控制尾迹效果显示的时机、频率、粗细、粒子类型
        private int drawStartDelay;
        private float trailThickness;
        private FleckDef trailFleck;

        // 碰撞检测步长、Tick计数器
        private int tickCount = 0;
        private float probeStep;

        //modExt初始化标志。
        private bool extensionLoaded = false;

        // 子弹精确位置，重写成使用贝塞尔轨迹
        public override Vector3 ExactPosition
        {
            get
            {
                LoadExtension();
                Vector3 result = useCubieCurve
                    ? BPosCubic(base.DistanceCoveredFraction)
                    : BPosQuadratic(base.DistanceCoveredFraction);
                result.y = def.Altitude;
                return result;
            }
        }

        // 懒加载配置的方法，把 XML 中的扩展数据一次性读取
        private void LoadExtension()
        {
            if (!extensionLoaded)
            {
                ModExt_BezierProjectiles modExtension = def.GetModExtension<ModExt_BezierProjectiles>();
                randOffsetRange = modExtension?.randOffsetRange ?? new FloatRange(-1f, 1f);
                controlPointFactor = modExtension?.controlPointFactor ?? 0.5f;
                drawStartDelay = modExtension?.drawStartDelay ?? 2;
                trailFleck = DefDatabase<FleckDef>.GetNamed(modExtension?.trailFleckDef, false);
                trailThickness = modExtension?.trailThickness ?? 0.025f;
                probeStep = modExtension?.probeStep ?? 0.2f;
                useCubieCurve = modExtension?.useCubieCurve ?? false;

                // 三阶特有字段
                P1Offset = modExtension?.P1Offset ?? 0.3f;
                P1Side = modExtension?.P1Side ?? 3f;
                P2Offset = modExtension?.P2Offset ?? 0.6f;
                P2Side = modExtension?.P2Side ?? -3f;
                enableRandomSide = modExtension?.enableRandomSide ?? false;
                randomSideChance = modExtension?.randomSideChance ?? 0.5f;

                extensionLoaded = true;
            }
        }

        // 储存发射方向向量。必须在 Launch() 前调用
        public void InitLaunchDir(Vector3 dir)
        {
            launchDirNormalized = dir;
        }

        // 初始化控制点的扰动值
        private void InitRandOffset()
        {
            // 二阶曲线的随机偏移保持不变
            randOffset.x = Rand.Range(randOffsetRange.min, randOffsetRange.max);
            randOffset.z = Rand.Range(randOffsetRange.min, randOffsetRange.max);

            // 重置方向为默认值
            randomSideDirection = 1f;

            // 检查是否启用三阶曲线 和 是否启用随机反向功能
            if (useCubieCurve && enableRandomSide)
            {
                // 根据设定的概率决定是否翻转方向
                if (Rand.Chance(randomSideChance))
                {
                    randomSideDirection = -1f;
                }
            }

            randomInitialized = true;
        }

        // 轨迹核心1：调用二阶贝塞尔 GetPointQuadratic()
        protected Vector3 BPosQuadratic(float t)
        {
            if (!randomInitialized)
            {
                InitRandOffset();
            }
            Vector3 vector = origin;
            float num = Vector3.Distance(origin, destination);
            Vector3 vector2 = vector + launchDirNormalized * (num * controlPointFactor) + randOffset;
            Vector3 vector3 = destination;
            return MstBezierUtil.GetPointQuadratic(origin, vector2, destination, t);
        }

        // 轨迹核心2：调用三阶贝塞尔 GetPointCubic()
        protected Vector3 BPosCubic(float t)
        {
            if (!randomInitialized)
            {
                InitRandOffset();
            }

            Vector3 P0 = origin;
            Vector3 P3 = destination;

            Vector3 dir = (P3 - P0).normalized;
            float totalDist = Vector3.Distance(P0, P3);

            // 计算法线（右手法则：XZ平面上旋转90度）
            Vector3 side = new Vector3(-dir.z, 0, dir.x); // 向左为正，向右为负

            // 在计算P1和P2时，加上已经生成好的随机抖动偏移量randOffset
            Vector3 P1 = P0 + dir * (P1Offset * totalDist) + side * P1Side * randomSideDirection + randOffset;
            Vector3 P2 = P0 + dir * (P2Offset * totalDist) + side * P2Side * randomSideDirection + randOffset;

            return MstBezierUtil.GetPointCubic(P0, P1, P2, P3, t);
        }

        protected override void DrawAt(Vector3 drawPos, bool flip = false)
        {
            LoadExtension();
            Vector3 vector;
            Vector3 vector2;
            if (useCubieCurve)
            {
                vector = BPosCubic(Mathf.Max(base.DistanceCoveredFraction - 0.01f, 0f));
                vector2 = BPosCubic(base.DistanceCoveredFraction);
            }
            else
            {
                vector = BPosQuadratic(Mathf.Max(base.DistanceCoveredFraction - 0.01f, 0f));
                vector2 = BPosQuadratic(base.DistanceCoveredFraction);
            }
            Quaternion rotation = Quaternion.LookRotation(vector2 - vector);
            if (tickCount >= drawStartDelay)
            {
                vector2.y = AltitudeLayer.Projectile.AltitudeFor();
                Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), vector2, rotation, DrawMat, 0);
                Comps_PostDraw();
            }
        }

        // 子弹初始化
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            LoadExtension();
            lastPosition = origin;
            tickCount = 0;
            randomInitialized = false;
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }

        // 计算贝塞尔新位置
        // 检测碰撞
        // 尾迹粒子渲染
        protected override void Tick()
        {
            LoadExtension();
            tickCount++;
            Vector3 exactPosition = ExactPosition;
            ticksToImpact--;
            Vector3 vector = useCubieCurve
                ? BPosCubic(base.DistanceCoveredFraction)
                : BPosQuadratic(base.DistanceCoveredFraction);
            vector.y = AltitudeLayer.Projectile.AltitudeFor();
            if (!vector.InBounds(MapHeld))
            {
                Destroy();
                return;
            }
            if (CheckCollisionBetween(exactPosition, vector))
            {
                return;
            }
            base.Position = vector.ToIntVec3();
            if (!base.Position.InBounds(base.Map))
            {
                Destroy();
            }
            else if (ticksToImpact <= 0)
            {
                base.Position = destination.ToIntVec3();
                ImpactSomething();
            }
            else if (tickCount >= drawStartDelay)
            {
                float num = 1f;
                Vector3 vector2 = lastPosition;
                Vector3 vector3 = vector;
                float num2 = (vector3 - vector2).MagnitudeHorizontal();
                int num3 = Mathf.Max(1, Mathf.CeilToInt(num2 / num));
                Vector3 vector4 = (vector3 - vector2) / num3;
                Vector3 end = vector2;
                if (trailFleck != null)
                {
                    for (int i = 1; i <= num3; i++)
                    {
                        Vector3 vector5 = vector2 + vector4 * i;
                        FleckMaker.ConnectingLine(vector5, end, trailFleck, base.Map, trailThickness);
                        end = vector5;
                    }
                }
                lastPosition = vector3;
            }
        }

        // 完整的射线碰撞检测
        private bool CheckCollisionBetween(Vector3 lastPos, Vector3 newPos)
        {
            Vector3 normalized = (newPos - lastPos).normalized;
            float num = (newPos - lastPos).MagnitudeHorizontal();
            int num2 = Mathf.CeilToInt(num / probeStep);
            Vector3 vect = lastPos;
            for (int i = 0; i <= num2; i++)
            {
                IntVec3 intVec = vect.ToIntVec3();
                if (intVec.InBounds(base.Map))
                {
                    List<Thing> thingList = intVec.GetThingList(base.Map);
                    foreach (Thing item in thingList)
                    {
                        if (item.def.Fillage == FillCategory.Full)
                        {
                            base.Position = intVec;
                            Impact(item);
                            return true;
                        }
                        if (!(item is Pawn pawn) || !base.HitFlags.HasFlag(ProjectileHitFlags.NonTargetPawns))
                        {
                            continue;
                        }
                        float num3 = VerbUtility.InterceptChanceFactorFromDistance(origin, intVec);
                        if (!(num3 > 0f))
                        {
                            continue;
                        }
                        float num4 = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
                        if (pawn.GetPosture() != PawnPosture.Standing)
                        {
                            num4 *= 0.1f;
                        }
                        if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
                        {
                            if (preventFriendlyFire)
                            {
                                num4 = 0f;
                                ThrowDebugText("ff-miss", intVec);
                            }
                            else
                            {
                                num4 *= Find.Storyteller.difficulty.friendlyFireChanceFactor;
                            }
                        }
                        float num5 = num4 * num3;
                        if (num5 > 1E-05f)
                        {
                            if (Rand.Chance(num5))
                            {
                                ThrowDebugText("int-" + num5.ToStringPercent(), intVec);
                                base.Position = intVec;
                                Impact(pawn);
                                return true;
                            }
                            ThrowDebugText(num5.ToStringPercent(), intVec);
                        }
                    }
                }
                vect += normalized * probeStep;
            }
            return false;
        }

        private void ThrowDebugText(string text, IntVec3 c)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(c.ToVector3Shifted(), base.Map, text);
            }
        }
    }
}
