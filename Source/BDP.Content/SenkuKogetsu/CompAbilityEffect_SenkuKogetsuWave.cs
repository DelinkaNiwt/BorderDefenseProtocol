using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.SenkuKogetsu
{
    /// <summary>
    /// 旋空弧月 Ability 效果配置。
    /// 它集中保存旧 BDP 月牙剑气业务需要的形状、伤害、运动与视觉参数。
    /// </summary>
    public sealed class CompProperties_SenkuKogetsuWave : CompProperties_AbilityEffect
    {
        /// <summary>
        /// 当前 Ability 施放后生成的月牙剑气实体 Def。
        /// </summary>
        public ThingDef waveDef;

        /// <summary>
        /// 当前月牙剑气允许的最小射程，单位为格。
        /// </summary>
        public float minRange = 5f;

        /// <summary>
        /// 当前月牙剑气允许的最大射程，单位为格。
        /// </summary>
        public float maxRange = 20f;

        /// <summary>
        /// 近距离时月牙的半宽。
        /// </summary>
        public float halfWidthNear = 10f;

        /// <summary>
        /// 远距离时月牙的半宽。
        /// </summary>
        public float halfWidthFar = 3f;

        /// <summary>
        /// 近距离时月牙的凸度。
        /// </summary>
        public float bulgeNear = 1.5f;

        /// <summary>
        /// 远距离时月牙的凸度。
        /// </summary>
        public float bulgeFar = 8f;

        /// <summary>
        /// 月牙条带的厚度。
        /// </summary>
        public float crescentThickness = 1.5f;

        /// <summary>
        /// 当前月牙剑气单次命中的基础伤害值。
        /// </summary>
        public int damageAmount = 15;

        /// <summary>
        /// 当前月牙剑气使用的伤害类型。
        /// </summary>
        public DamageDef damageDef;

        /// <summary>
        /// 当前月牙剑气使用的护甲穿透。
        /// </summary>
        public float armorPenetration = 0.5f;

        /// <summary>
        /// 当前月牙剑气的制止力阈值。
        /// 体型小于等于该值的 Pawn 命中后会进入踉跄。
        /// </summary>
        public float stoppingPower = 3f;

        /// <summary>
        /// 当前踉跄持续 tick 数。
        /// </summary>
        public int staggerTicks = 95;

        /// <summary>
        /// 当前月牙剑气每 tick 推进距离。
        /// </summary>
        public float speedPerTick = 0.5f;

        /// <summary>
        /// 爆发阶段走完的距离比例。
        /// </summary>
        public float burstDist = 0.98f;

        /// <summary>
        /// 爆发阶段占总飞行时间的比例。
        /// </summary>
        public float burstPhase = 0.40f;

        /// <summary>
        /// 到达终点后滞留渐隐的 tick 数。
        /// </summary>
        public int lingerTicks = 45;

        /// <summary>
        /// 当前月牙剑气的发光强度倍率。
        /// </summary>
        public float glowIntensity = 1.5f;

        /// <summary>
        /// 当前月牙剑气是否被山体阻挡。
        /// </summary>
        public bool respectWalls = true;

        /// <summary>
        /// 当前月牙剑气是否允许友伤。
        /// </summary>
        public bool friendlyFire = false;

        /// <summary>
        /// 当前月牙剑气是否对建筑造成伤害。
        /// </summary>
        public bool damageBuildings = false;

        /// <summary>
        /// 当前月牙剑气对建筑的伤害倍率。
        /// </summary>
        public float buildingDamageFactor = 1f;

        /// <summary>
        /// 构造当前 Ability 效果配置，并绑定对应的 effect comp。
        /// </summary>
        public CompProperties_SenkuKogetsuWave()
        {
            compClass = typeof(CompAbilityEffect_SenkuKogetsuWave);
        }

        /// <summary>
        /// 根据当前设定射程插值月牙半宽与凸度。
        /// 旧行为要求近距离宽扁、远距离窄深。
        /// </summary>
        /// <param name="setRange">当前实际设定射程。</param>
        /// <param name="halfWidth">输出的半宽。</param>
        /// <param name="bulge">输出的凸度。</param>
        public void GetCrescentParams(float setRange, out float halfWidth, out float bulge)
        {
            float t = Mathf.Clamp01((setRange - minRange) / Mathf.Max(maxRange - minRange, 0.01f));
            halfWidth = Mathf.Lerp(halfWidthNear, halfWidthFar, t);
            bulge = Mathf.Lerp(bulgeNear, bulgeFar, t);
        }

        /// <summary>
        /// 计算月牙外边缘上一点的局部坐标。
        /// 局部坐标系以施法者为原点，+Z 为前方，+X 为右侧。
        /// </summary>
        /// <param name="s">从左尖到右尖的归一化参数，范围 [-1, 1]。</param>
        /// <param name="halfWidth">当前半宽。</param>
        /// <param name="bulge">当前凸度。</param>
        /// <param name="setRange">当前设定射程。</param>
        /// <returns>外边缘点的二维局部坐标。</returns>
        public Vector2 CrescentOuterPoint(float s, float halfWidth, float bulge, float setRange)
        {
            float taper = 1f - s * s;
            float x = halfWidth * s;
            float z = setRange - bulge * s * s
                + crescentThickness * 0.5f * Mathf.Pow(Mathf.Max(taper, 0f), 0.7f);
            return new Vector2(x, z);
        }
    }

    /// <summary>
    /// 旋空弧月 Ability 效果组件。
    /// 它负责：
    ///
    /// 1. 施法时生成月牙剑气实体。
    /// 2. 瞄准时绘制与正式波体一致的月牙预览。
    /// </summary>
    public sealed class CompAbilityEffect_SenkuKogetsuWave : CompAbilityEffect
    {
        /// <summary>
        /// 当前效果组件使用的强类型配置。
        /// </summary>
        private new CompProperties_SenkuKogetsuWave Props
        {
            get { return (CompProperties_SenkuKogetsuWave)props; }
        }

        /// <summary>
        /// 预览使用的格子缓存。
        /// </summary>
        private readonly List<IntVec3> tmpCells = new List<IntVec3>();

        /// <summary>
        /// 当前月牙预览多边形的采样段数。
        /// </summary>
        private const int IndicatorSegments = 32;

        /// <summary>
        /// 对当前目标应用旋空弧月效果。
        /// 施法时直接生成一枚自定义月牙剑气实体。
        /// </summary>
        /// <param name="target">当前技能目标。</param>
        /// <param name="dest">当前技能目的地。</param>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || Props.waveDef == null)
            {
                return;
            }

            Map map = pawn.Map;
            if (map == null)
            {
                return;
            }

            Vector3 origin = pawn.DrawPos;
            Vector3 dir = (target.Cell - pawn.Position).ToVector3();
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
            {
                return;
            }

            float rawRange = dir.magnitude;
            dir /= rawRange;

            float setRange = Mathf.Clamp(rawRange, Props.minRange, Props.maxRange);
            Vector3 destination = origin + dir * setRange;
            SenkuKogetsuDiagnostics.LogCast(pawn, target, rawRange, setRange, Props.waveDef);

            BdpSenkuKogetsuWave wave =
                (BdpSenkuKogetsuWave)GenSpawn.Spawn(Props.waveDef, pawn.Position, map);
            wave.Launch(pawn, origin, destination, setRange, Props);
        }

        /// <summary>
        /// 绘制当前技能的月牙覆盖预览。
        /// </summary>
        /// <param name="target">当前预览目标。</param>
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(ComputeIndicatorCells(target));
        }

        /// <summary>
        /// 计算当前预览应覆盖的地图格子。
        /// 预览、伤害与 Mesh 共用同一套月牙参数。
        /// </summary>
        /// <param name="target">当前预览目标。</param>
        /// <returns>当前预览覆盖格集合。</returns>
        private List<IntVec3> ComputeIndicatorCells(LocalTargetInfo target)
        {
            tmpCells.Clear();

            Pawn pawn = parent.pawn;
            if (pawn == null)
            {
                return tmpCells;
            }

            Map map = pawn.Map;
            IntVec3 originCell = pawn.Position;

            Vector3 dir = (target.Cell - originCell).ToVector3();
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
            {
                return tmpCells;
            }

            float rawRange = dir.magnitude;
            Vector3 forward = dir / rawRange;
            float setRange = Mathf.Clamp(rawRange, Props.minRange, Props.maxRange);

            float halfWidth;
            float bulge;
            Props.GetCrescentParams(setRange, out halfWidth, out bulge);

            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 originPoint = originCell.ToVector3Shifted();
            originPoint.y = 0f;

            Vector2[] polygon = new Vector2[IndicatorSegments + 2];
            polygon[0] = Vector2.zero;
            for (int index = 0; index <= IndicatorSegments; index++)
            {
                float s = -1f + 2f * index / IndicatorSegments;
                polygon[index + 1] = Props.CrescentOuterPoint(s, halfWidth, bulge, setRange);
            }

            float searchRadius = setRange + bulge + Props.crescentThickness + 1f;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(originCell, searchRadius, useCenter: false))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                Vector3 offset = cell.ToVector3Shifted() - originPoint;
                offset.y = 0f;
                float lx = Vector3.Dot(offset, right);
                float lz = Vector3.Dot(offset, forward);
                if (lz < 0f)
                {
                    continue;
                }

                if (!PointInPolygon(lx, lz, polygon))
                {
                    continue;
                }

                if (BdpSenkuKogetsuWave.IsBlockedByWall(originCell, cell, map))
                {
                    continue;
                }

                tmpCells.Add(cell);
            }

            return tmpCells;
        }

        /// <summary>
        /// 判断二维点是否位于指定多边形内部。
        /// </summary>
        /// <param name="px">待判定点的局部 X。</param>
        /// <param name="pz">待判定点的局部 Z。</param>
        /// <param name="polygon">当前多边形顶点序列。</param>
        /// <returns>在内部时返回 true。</returns>
        private static bool PointInPolygon(float px, float pz, Vector2[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                float iz = polygon[i].y;
                float jz = polygon[j].y;
                float ix = polygon[i].x;
                float jx = polygon[j].x;
                if ((iz > pz) != (jz > pz)
                    && px < (jx - ix) * (pz - iz) / (jz - iz) + ix)
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }
    }
}
