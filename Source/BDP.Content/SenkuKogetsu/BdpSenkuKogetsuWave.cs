using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.SenkuKogetsu
{
    /// <summary>
    /// 正式旋空弧月波体实体。
    /// 它按旧 BDP 逻辑推进、扫掠伤害并绘制月牙 Mesh。
    /// </summary>
    public sealed class BdpSenkuKogetsuWave : ThingWithComps
    {
        /// <summary>
        /// 当前月牙剑气的施放原点。
        /// </summary>
        private Vector3 origin;

        /// <summary>
        /// 当前月牙剑气的理论终点。
        /// </summary>
        private Vector3 destination;

        /// <summary>
        /// 当前月牙剑气的施法者。
        /// </summary>
        private Pawn launcher;

        /// <summary>
        /// 当前飞行阶段总 tick 数。
        /// </summary>
        private int travelTicks;

        /// <summary>
        /// 当前终点滞留阶段总 tick 数。
        /// </summary>
        private int lingerTicks;

        /// <summary>
        /// 当前实体已存活 tick 数。
        /// </summary>
        private int ticksAlive;

        /// <summary>
        /// 当前月牙剑气设定射程。
        /// </summary>
        private float setRange;

        /// <summary>
        /// 当前月牙剑气半宽。
        /// </summary>
        private float halfWidth;

        /// <summary>
        /// 当前月牙剑气凸度。
        /// </summary>
        private float bulge;

        /// <summary>
        /// 当前月牙剑气条带厚度。
        /// </summary>
        private float crescentThickness;

        /// <summary>
        /// 当前月牙剑气基础伤害值。
        /// </summary>
        private int damageAmount;

        /// <summary>
        /// 当前月牙剑气伤害类型。
        /// </summary>
        private DamageDef damageDef;

        /// <summary>
        /// 当前月牙剑气护甲穿透。
        /// </summary>
        private float armorPenetration;

        /// <summary>
        /// 当前月牙剑气是否被山体阻挡。
        /// </summary>
        private bool respectWalls;

        /// <summary>
        /// 当前月牙剑气是否允许友伤。
        /// </summary>
        private bool friendlyFire;

        /// <summary>
        /// 当前月牙剑气是否伤害建筑。
        /// </summary>
        private bool damageBuildings;

        /// <summary>
        /// 当前月牙剑气对建筑的伤害倍率。
        /// </summary>
        private float buildingDamageFactor;

        /// <summary>
        /// 当前月牙剑气的制止力阈值。
        /// </summary>
        private float stoppingPower;

        /// <summary>
        /// 当前踉跄持续 tick 数。
        /// </summary>
        private int staggerTicks;

        /// <summary>
        /// 当前波体已经伤过的目标集合。
        /// 用于保持单次波动去重伤害语义。
        /// </summary>
        private readonly HashSet<int> damagedThings = new HashSet<int>();

        /// <summary>
        /// 当前上一 tick 的飞行进度。
        /// </summary>
        private float prevProgress;

        /// <summary>
        /// 当前缓存的月牙 Mesh。
        /// </summary>
        private Mesh crescentMesh;

        /// <summary>
        /// 当前缓存的材质实例。
        /// </summary>
        private Material cachedMat;

        /// <summary>
        /// 当前材质属性块缓存。
        /// </summary>
        private MaterialPropertyBlock propBlock;

        /// <summary>
        /// 当前缓存 Mesh 对应的进度值。
        /// </summary>
        private float cachedMeshProgress = -1f;

        /// <summary>
        /// 当前发光强度倍率。
        /// </summary>
        private float glowIntensity = 1.5f;

        /// <summary>
        /// 当前月牙剑气前进方向。
        /// </summary>
        private Vector3 forward;

        /// <summary>
        /// 当前月牙剑气右侧方向。
        /// </summary>
        private Vector3 right;

        /// <summary>
        /// 当前月牙 Mesh 的采样段数。
        /// </summary>
        private const int MeshSegments = 32;

        /// <summary>
        /// 当前爆发阶段完成的距离比例。
        /// </summary>
        private float burstDist = 0.98f;

        /// <summary>
        /// 当前爆发阶段占总飞行时间的比例。
        /// </summary>
        private float burstPhase = 0.40f;

        /// <summary>
        /// 预计算的逐段山体阻挡距离。
        /// 索引为顶点对序号（0..MeshSegments），存储该方向射线碰到的第一块山体距原点的水平距离。
        /// float.MaxValue 表示该方向无山体阻挡。
        /// 在 Launch 时一次性计算，方向固定不随飞行进度变化。
        /// </summary>
        private float[] segmentBlockDistance;

        /// <summary>
        /// 经过间隙合并和渐变处理后的逐顶点对 alpha 值。
        /// 在 RebuildCrescentMesh 中每次重建 Mesh 时按当前进度重新计算。
        /// </summary>
        private float[] segmentAlpha;

        /// <summary>
        /// 上次抛洒山体剐蹭火花的 tick 序号，用于节流。
        /// </summary>
        private int lastSparkTick = -999;

        /// <summary>
        /// 渐变过渡世界距离（格）。月牙前沿距山体此距离内开始渐隐。
        /// </summary>
        private const float GradientDistBefore = 2.5f;

        /// <summary>
        /// 渐变过渡世界距离（格）。月牙前沿越过山体此距离后完全消失。
        /// </summary>
        private const float GradientDistAfter = 0.5f;

        /// <summary>
        /// 最小间隙段数。小于此值的未被挡间隙直接并入阻挡区。
        /// </summary>
        private const int MinGapSegments = 3;

        /// <summary>
        /// 剐蹭火花节流间隔（tick）。
        /// </summary>
        private const int SparkThrottleTicks = 2;

        /// <summary>
        /// 当前实体是否仍处于飞行阶段。
        /// </summary>
        private bool IsTraveling
        {
            get { return ticksAlive < travelTicks; }
        }

        /// <summary>
        /// 当前飞行进度，范围 [0, 1]。
        /// 正式运动规则要求前段快速爆发、后段平滑减速。
        /// </summary>
        private float TravelProgress
        {
            get
            {
                if (travelTicks <= 0)
                {
                    return 1f;
                }

                float t = Mathf.Clamp01((float)Mathf.Min(ticksAlive, travelTicks) / travelTicks);
                if (t < burstPhase)
                {
                    return burstDist * (t / burstPhase);
                }

                float u = (t - burstPhase) / (1f - burstPhase);
                float smooth = u * u * (3f - 2f * u);
                return burstDist + (1f - burstDist) * smooth;
            }
        }

        /// <summary>
        /// 当前滞留渐隐进度，范围 [0, 1]。
        /// </summary>
        private float FadeProgress
        {
            get
            {
                if (IsTraveling)
                {
                    return 0f;
                }

                if (lingerTicks <= 0)
                {
                    return 1f;
                }

                return Mathf.Clamp01((float)(ticksAlive - travelTicks) / lingerTicks);
            }
        }

        /// <summary>
        /// 当前实体的绘制中心点。
        /// RimWorld 的渲染剔除会据此决定是否显示当前波体。
        /// </summary>
        public override Vector3 DrawPos
        {
            get
            {
                float progress = TravelProgress;
                Vector3 point = origin;
                point.y = 0f;
                return point + forward * (setRange * progress) + Vector3.up * def.Altitude;
            }
        }

        /// <summary>
        /// 初始化当前月牙剑气实体。
        /// </summary>
        /// <param name="launcher">当前施法者。</param>
        /// <param name="origin">当前施法原点。</param>
        /// <param name="destination">当前理论终点。</param>
        /// <param name="setRange">当前设定射程。</param>
        /// <param name="props">当前使用的效果配置。</param>
        public void Launch(
            Pawn launcher,
            Vector3 origin,
            Vector3 destination,
            float setRange,
            CompProperties_SenkuKogetsuWave props)
        {
            this.launcher = launcher;
            this.origin = origin;
            this.destination = destination;
            this.setRange = setRange;

            props.GetCrescentParams(setRange, out halfWidth, out bulge);
            crescentThickness = props.crescentThickness;
            SenkuKogetsuDiagnostics.LogCrescentParams(
                setRange,
                halfWidth,
                bulge,
                crescentThickness);

            damageAmount = props.damageAmount;
            damageDef = props.damageDef ?? DamageDefOf.Cut;
            armorPenetration = props.armorPenetration;
            respectWalls = props.respectWalls;
            friendlyFire = props.friendlyFire;
            damageBuildings = props.damageBuildings;
            buildingDamageFactor = props.buildingDamageFactor;
            stoppingPower = props.stoppingPower;
            staggerTicks = props.staggerTicks;
            burstDist = Mathf.Clamp01(props.burstDist);
            burstPhase = Mathf.Clamp(props.burstPhase, 0.01f, 0.99f);
            glowIntensity = Mathf.Max(props.glowIntensity, 0.1f);

            Vector3 diff = destination - origin;
            diff.y = 0f;
            forward = diff.normalized;
            right = new Vector3(forward.z, 0f, -forward.x);

            // 预计算逐段实体阻挡距离。
            // 每段射线方向固定，记录碰到的第一块山体距原点的水平距离。
            segmentBlockDistance = PrecomputeSegmentBlockDistance();
            segmentAlpha = new float[MeshSegments + 1];

            float totalDist = diff.magnitude;
            travelTicks = Mathf.Max(1, Mathf.RoundToInt(totalDist / Mathf.Max(props.speedPerTick, 0.01f)));
            lingerTicks = Mathf.Max(props.lingerTicks, 1);
            ticksAlive = 0;
            prevProgress = 0f;
        }

        /// <summary>
        /// 推进当前月牙剑气实体。
        /// 飞行阶段持续扫掠伤害；结束后进入滞留渐隐，再自动消失。
        /// </summary>
        protected override void Tick()
        {
            base.Tick();
            ticksAlive++;

            if (IsTraveling)
            {
                float curProgress = TravelProgress;
                DamageCrescentSweep(prevProgress, curProgress);
                prevProgress = curProgress;

                // 山体剐蹭火花：在阻挡渐变边界抛洒能量粒子
                TryThrowMountainSparks();
                return;
            }

            if (ticksAlive >= travelTicks + lingerTicks && !Destroyed)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        /// <summary>
        /// 对指定进度区间扫过的格子施加月牙伤害。
        /// 这段算法直接沿用旧 BDP 的月牙方程求交逻辑。
        /// </summary>
        /// <param name="prevP">上一 tick 的飞行进度。</param>
        /// <param name="curP">当前 tick 的飞行进度。</param>
        private void DamageCrescentSweep(float prevP, float curP)
        {
            if (Map == null || launcher == null || curP < 0.01f)
            {
                return;
            }

            Vector3 flatOrigin = origin;
            flatOrigin.y = 0f;
            IntVec3 originCell = flatOrigin.ToIntVec3();

            float margin = bulge + crescentThickness + 2f;
            float outerRadius = setRange * curP + margin;
            float innerRadius = Mathf.Max(setRange * prevP - margin, 0f);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(originCell, outerRadius, useCenter: false))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                Vector3 offset = cell.ToVector3Shifted() - flatOrigin;
                offset.y = 0f;
                float distSq = offset.x * offset.x + offset.z * offset.z;
                if (distSq < innerRadius * innerRadius)
                {
                    continue;
                }

                float lx = Vector3.Dot(offset, right);
                float lz = Vector3.Dot(offset, forward);
                if (lz < 0f)
                {
                    continue;
                }

                float hw2 = halfWidth * halfWidth;
                if (hw2 < 0.001f)
                {
                    continue;
                }

                float k = bulge * lx * lx / (hw2 * setRange);
                float lzNorm = lz / setRange;
                float disc = lzNorm * lzNorm + 4f * k;
                if (disc < 0f)
                {
                    continue;
                }

                float sqrtDisc = Mathf.Sqrt(disc);
                float progress = (lzNorm + sqrtDisc) * 0.5f;

                float progressMargin = crescentThickness / Mathf.Max(setRange, 1f) * 0.5f + 0.05f;
                if (progress < prevP - progressMargin || progress > curP + progressMargin)
                {
                    continue;
                }

                float clampedProgress = Mathf.Max(progress, 0.01f);
                float s = lx / (halfWidth * clampedProgress);
                if (s * s > 1.05f)
                {
                    continue;
                }

                if (respectWalls && IsBlockedByWall(originCell, cell, Map))
                {
                    SenkuKogetsuDiagnostics.LogMountainBlock(originCell, cell);
                    continue;
                }

                DamageThingsInCell(cell);
            }
        }

        /// <summary>
        /// 对当前格中的 Pawn 或建筑施加伤害。
        /// 正式命中规则要求对同一目标去重，并保留建筑倍率与踉跄逻辑。
        /// </summary>
        /// <param name="cell">当前被扫到的地图格。</param>
        private void DamageThingsInCell(IntVec3 cell)
        {
            List<Thing> things = cell.GetThingList(Map);
            for (int i = things.Count - 1; i >= 0; i--)
            {
                Thing thing = things[i];

                Pawn target = thing as Pawn;
                if (target != null)
                {
                    if (target == launcher)
                    {
                        SenkuKogetsuDiagnostics.LogTargetResolution(target, "skip_self");
                        continue;
                    }

                    if (!friendlyFire && !target.HostileTo(launcher))
                    {
                        SenkuKogetsuDiagnostics.LogTargetResolution(target, "skip_friendly");
                        continue;
                    }

                    if (!damagedThings.Add(target.thingIDNumber))
                    {
                        SenkuKogetsuDiagnostics.LogTargetResolution(target, "skip_duplicate");
                        continue;
                    }

                    BattleLogEntry_RangedImpact log =
                        new BattleLogEntry_RangedImpact(launcher, target, target, null, def, null);
                    Find.BattleLog.Add(log);

                    float angle = (target.DrawPos - origin).AngleFlat();
                    DamageInfo damageInfo = new DamageInfo(
                        damageDef,
                        damageAmount,
                        armorPenetration,
                        angle,
                        launcher,
                        weapon: def);
                    target.TakeDamage(damageInfo).AssociateWithLog(log);
                    SenkuKogetsuDiagnostics.LogTargetResolution(target, "pawn_hit", damageAmount);

                    if (stoppingPower > 0f && target.BodySize <= stoppingPower)
                    {
                        target.stances?.stagger?.StaggerFor(staggerTicks);
                    }

                    continue;
                }

                Building building = thing as Building;
                if (damageBuildings && building != null)
                {
                    if (building.def.building == null)
                    {
                        SenkuKogetsuDiagnostics.LogTargetResolution(building, "skip_non_building");
                        continue;
                    }

                    if (!damagedThings.Add(building.thingIDNumber))
                    {
                        SenkuKogetsuDiagnostics.LogTargetResolution(building, "skip_duplicate");
                        continue;
                    }

                    int buildingDamage = Mathf.RoundToInt(damageAmount * buildingDamageFactor);
                    DamageInfo damageInfo = new DamageInfo(
                        damageDef,
                        buildingDamage,
                        armorPenetration,
                        -1f,
                        launcher);
                    building.TakeDamage(damageInfo);
                    SenkuKogetsuDiagnostics.LogBuildingDamage(building, buildingDamage, buildingDamageFactor);
                    SenkuKogetsuDiagnostics.LogTargetResolution(building, "building_hit", buildingDamage);
                }
            }
        }

        /// <summary>
        /// 绘制当前月牙剑气 Mesh。
        /// Mesh 与伤害共用同一套月牙参数，并随滞留阶段逐渐淡出。
        /// </summary>
        /// <param name="drawLoc">RimWorld 给出的绘制位置。</param>
        /// <param name="flip">当前是否翻转。</param>
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (def.graphic == null)
            {
                return;
            }

            if (cachedMat == null)
            {
                cachedMat = def.graphic.MatSingle;
            }

            if (cachedMat == null)
            {
                return;
            }

            if (propBlock == null)
            {
                propBlock = new MaterialPropertyBlock();
            }

            float progress = Mathf.Max(TravelProgress, 0.02f);
            if (crescentMesh == null || Mathf.Abs(progress - cachedMeshProgress) > 0.005f)
            {
                // 每帧按当前飞行进度重新计算逐段 alpha
                ComputeSegmentAlpha(progress);

                float curHalfWidth = halfWidth * progress;
                float curBulge = bulge * progress;
                float curRange = setRange * progress;
                RebuildCrescentMesh(curHalfWidth, curBulge, curRange, crescentThickness);
                cachedMeshProgress = progress;
            }

            float alpha = 1f - FadeProgress;
            float intensity = glowIntensity;
            propBlock.SetColor("_Color", new Color(intensity, intensity, intensity * 0.95f, alpha));

            float angleDeg = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            Vector3 meshOrigin = origin;
            meshOrigin.y = def.Altitude;

            Matrix4x4 matrix = Matrix4x4.TRS(
                meshOrigin,
                Quaternion.AngleAxis(angleDeg, Vector3.up),
                Vector3.one);
            Graphics.DrawMesh(crescentMesh, matrix, cachedMat, 0, null, 0, propBlock);
        }

        /// <summary>
        /// 重建当前月牙条带 Mesh。
        /// 局部坐标系以 +Z 为前方、+X 为右。
        /// </summary>
        /// <param name="curHalfWidth">当前进度下的半宽。</param>
        /// <param name="curBulge">当前进度下的凸度。</param>
        /// <param name="curRange">当前进度下的射程。</param>
        /// <param name="thickness">当前条带厚度。</param>
        private void RebuildCrescentMesh(
            float curHalfWidth,
            float curBulge,
            float curRange,
            float thickness)
        {
            if (crescentMesh == null)
            {
                crescentMesh = new Mesh();
            }
            else
            {
                crescentMesh.Clear();
            }

            int segmentCount = MeshSegments;
            int vertexCount = (segmentCount + 1) * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Color[] colors = new Color[vertexCount];
            int[] triangles = new int[segmentCount * 6];

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                float s = -1f + 2f * t;

                float middleX = curHalfWidth * s;
                float middleZ = curRange - curBulge * s * s;

                float taper = 1f - s * s;
                float halfThickness = thickness * 0.5f * Mathf.Pow(Mathf.Max(taper, 0f), 0.7f);

                // 阻挡 alpha：0=完全被挡，1=完全可见，渐变段取过渡值
                float blockAlpha = segmentAlpha != null && i < segmentAlpha.Length
                    ? segmentAlpha[i]
                    : 1f;

                vertices[i * 2] = new Vector3(middleX, 0f, middleZ - halfThickness);
                vertices[i * 2 + 1] = new Vector3(middleX, 0f, middleZ + halfThickness);
                uvs[i * 2] = new Vector2(t, 0f);
                uvs[i * 2 + 1] = new Vector2(t, 1f);

                colors[i * 2] = new Color(1f, 1f, 1f, 0f * blockAlpha);
                colors[i * 2 + 1] = new Color(1f, 1f, 1f, 1f * blockAlpha);
            }

            for (int i = 0; i < segmentCount; i++)
            {
                int baseIndex = i * 2;
                int triangleIndex = i * 6;
                triangles[triangleIndex] = baseIndex;
                triangles[triangleIndex + 1] = baseIndex + 1;
                triangles[triangleIndex + 2] = baseIndex + 2;
                triangles[triangleIndex + 3] = baseIndex + 1;
                triangles[triangleIndex + 4] = baseIndex + 3;
                triangles[triangleIndex + 5] = baseIndex + 2;
            }

            crescentMesh.vertices = vertices;
            crescentMesh.uv = uvs;
            crescentMesh.colors = colors;
            crescentMesh.triangles = triangles;
            crescentMesh.RecalculateNormals();
        }

        /// <summary>
        /// 检查从施法原点到目标格之间是否存在实体阻挡。
        /// 任何填满格子的不可通行物（山体、墙体、遗迹）均视为阻挡。
        /// 本方法是整个旋空弧月系统唯一的阻挡判定入口，瞄准预览、伤害判定、
        /// 视觉缺口预计算均走此方法。
        /// </summary>
        /// <param name="origin">施法原点格。</param>
        /// <param name="target">待判定目标格。</param>
        /// <param name="map">当前地图。</param>
        /// <returns>被实体阻挡时返回 true。</returns>
        public static bool IsBlockedByWall(IntVec3 origin, IntVec3 target, Map map)
        {
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(origin, target))
            {
                if (cell == origin || cell == target)
                {
                    continue;
                }

                if (cell.GetEdifice(map) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 预计算所有 33 个顶点对方向的首次山体阻挡距离。
        /// 沿每条射线从原点出发，记录碰到的第一块天然山体距原点的水平距离。
        /// 无阻挡的方向存储 float.MaxValue。
        /// </summary>
        /// <returns>逐顶点对的阻挡距离数组。</returns>
        private float[] PrecomputeSegmentBlockDistance()
        {
            float[] blockDist = new float[MeshSegments + 1];
            for (int i = 0; i <= MeshSegments; i++)
            {
                blockDist[i] = float.MaxValue;
            }

            if (Map == null || !respectWalls)
            {
                return blockDist;
            }

            Vector3 flatOrigin = origin;
            flatOrigin.y = 0f;
            IntVec3 originCell = origin.ToIntVec3();

            for (int i = 0; i <= MeshSegments; i++)
            {
                float t = (float)i / MeshSegments;
                float s = -1f + 2f * t;
                float midX = halfWidth * s;
                float midZ = setRange - bulge * s * s;
                Vector3 worldPos = flatOrigin + forward * midZ + right * midX;
                IntVec3 targetCell = worldPos.ToIntVec3();

                if (targetCell == originCell || !targetCell.InBounds(Map))
                {
                    continue;
                }

                // 沿射线逐格查找首次实体阻挡
                foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(originCell, targetCell))
                {
                    if (cell == originCell)
                    {
                        continue;
                    }

                    if (IsBlockedByWall(originCell, cell, Map))
                    {
                        Vector3 cellPos = cell.ToVector3Shifted();
                        cellPos.y = 0f;
                        blockDist[i] = (cellPos - flatOrigin).magnitude;
                        break;
                    }
                }
            }

            return blockDist;
        }

        /// <summary>
        /// 基于阻挡距离与当前飞行进度计算逐顶点对 alpha 值。
        /// 处理流程：小间隙合并 → 距离比较 → 渐变过渡。
        /// 月牙前沿未到山体：alpha=1；接近山体时渐变；越过山体后：alpha=0。
        /// </summary>
        /// <param name="curProgress">当前飞行进度 [0, 1]。</param>
        private void ComputeSegmentAlpha(float curProgress)
        {
            if (segmentBlockDistance == null || segmentAlpha == null)
            {
                return;
            }

            int count = MeshSegments + 1;
            float curRange = setRange * curProgress;
            float curHalfWidth = halfWidth * curProgress;
            float curBulge = bulge * curProgress;

            // 第一步：合并过小的未被挡间隙
            // 先转成布尔判断（有阻挡 = blockDist < float.MaxValue），再做间隙合并
            bool[] blocked = new bool[count];
            for (int i = 0; i < count; i++)
            {
                blocked[i] = segmentBlockDistance[i] < float.MaxValue * 0.5f;
            }

            int gapStart = -1;
            for (int i = 0; i < count; i++)
            {
                if (!blocked[i])
                {
                    if (gapStart < 0) gapStart = i;
                }
                else
                {
                    if (gapStart >= 0)
                    {
                        int gapLen = i - gapStart;
                        if (gapLen < MinGapSegments)
                        {
                            // 取两侧阻挡距离的最小值赋给间隙段
                            float minBlockDist = float.MaxValue;
                            if (gapStart > 0
                                && segmentBlockDistance[gapStart - 1] < float.MaxValue * 0.5f)
                            {
                                minBlockDist = Mathf.Min(minBlockDist,
                                    segmentBlockDistance[gapStart - 1]);
                            }

                            if (i < count
                                && segmentBlockDistance[i] < float.MaxValue * 0.5f)
                            {
                                minBlockDist = Mathf.Min(minBlockDist,
                                    segmentBlockDistance[i]);
                            }

                            if (minBlockDist < float.MaxValue * 0.5f)
                            {
                                for (int j = gapStart; j < i; j++)
                                {
                                    segmentBlockDistance[j] = minBlockDist;
                                }
                            }
                        }

                        gapStart = -1;
                    }
                }
            }

            // 处理末尾间隙
            if (gapStart >= 0)
            {
                int gapLen = count - gapStart;
                if (gapLen < MinGapSegments && gapStart > 0
                    && segmentBlockDistance[gapStart - 1] < float.MaxValue * 0.5f)
                {
                    float prevDist = segmentBlockDistance[gapStart - 1];
                    for (int j = gapStart; j < count; j++)
                    {
                        segmentBlockDistance[j] = prevDist;
                    }
                }
            }

            // 第二步：按当前进度与世界距离计算 alpha
            for (int i = 0; i < count; i++)
            {
                float blockDist = segmentBlockDistance[i];
                if (blockDist >= float.MaxValue * 0.5f)
                {
                    segmentAlpha[i] = 1f;
                    continue;
                }

                // 当前段的世界位置
                float t = (float)i / MeshSegments;
                float s = -1f + 2f * t;
                float midX = curHalfWidth * s;
                float midZ = curRange - curBulge * s * s;
                float curDist = Mathf.Sqrt(midX * midX + midZ * midZ);

                float distToBlock = curDist - blockDist;

                if (distToBlock < -GradientDistBefore)
                {
                    // 未到山体：完整可见
                    segmentAlpha[i] = 1f;
                }
                else if (distToBlock > GradientDistAfter)
                {
                    // 已过山体：完全消失
                    segmentAlpha[i] = 0f;
                }
                else
                {
                    // 渐变带内
                    float fadeT = (distToBlock + GradientDistBefore)
                        / (GradientDistBefore + GradientDistAfter);
                    segmentAlpha[i] = 1f - Mathf.Clamp01(fadeT);
                }
            }
        }

        /// <summary>
        /// 在山体当前剐蹭面抛洒火花。
        /// 只在飞行阶段调用，找出月牙前沿正在接触山体的段（当前处于渐变带），
        /// 在其世界位置抛洒粉尘云 + 碎屑粒子。
        /// </summary>
        private void TryThrowMountainSparks()
        {
            if (segmentBlockDistance == null || segmentAlpha == null || Map == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick - lastSparkTick < SparkThrottleTicks)
            {
                return;
            }

            float curProgress = TravelProgress;

            // 检查是否有任何段正处于渐变带（正在被阻挡）
            bool anyActiveContact = false;
            for (int i = 0; i <= MeshSegments; i++)
            {
                if (segmentAlpha[i] > 0.01f && segmentAlpha[i] < 0.99f)
                {
                    anyActiveContact = true;
                    break;
                }
            }

            if (!anyActiveContact)
            {
                return;
            }

            lastSparkTick = currentTick;

            HashSet<IntVec3> sparkedCells = new HashSet<IntVec3>();

            for (int i = 0; i <= MeshSegments; i++)
            {
                float alpha = segmentAlpha[i];
                // 只抛在渐变带（alpha 0.05~0.9）——这正是月牙当前在跨越山体的段
                if (alpha < 0.05f || alpha > 0.9f)
                {
                    continue;
                }

                float blockDist = segmentBlockDistance[i];
                if (blockDist >= float.MaxValue * 0.5f)
                {
                    continue;
                }

                float t = (float)i / MeshSegments;
                float s = -1f + 2f * t;

                // 计算山体面的实际世界位置（沿该段方向射线，距原点 blockDist）
                float midXFull = halfWidth * s;
                float midZFull = setRange - bulge * s * s;
                float fullDist = Mathf.Sqrt(midXFull * midXFull + midZFull * midZFull);
                if (fullDist < 0.001f)
                {
                    continue;
                }

                Vector3 rayDir = (forward * midZFull + right * midXFull) / fullDist;
                Vector3 mountainPos = origin + rayDir * blockDist;
                mountainPos.y = def.Altitude;
                IntVec3 sparkCell = mountainPos.ToIntVec3();

                if (!sparkCell.InBounds(Map) || !sparkedCells.Add(sparkCell))
                {
                    continue;
                }

                // 粉尘撞击云（模拟山体被切割粉碎）
                FleckMaker.ThrowDustPuff(sparkCell, Map, Rand.Range(1.2f, 2.0f));

                // 高速碎屑粒子飞溅
                for (int j = 0; j < 5; j++)
                {
                    Vector3 offset = new Vector3(
                        Rand.Range(-1.2f, 1.2f),
                        0f,
                        Rand.Range(-1.2f, 1.2f));
                    FleckMaker.Static(
                        mountainPos + offset,
                        Map,
                        FleckDefOf.ShotHit_Dirt,
                        Rand.Range(0.5f, 1.2f));
                }
            }
        }
    }
}
