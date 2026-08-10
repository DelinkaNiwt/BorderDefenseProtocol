using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.CombatBody.Wounds.Visuals
{
    /// <summary>
    /// 管理单个战斗体伤口的 Trion 粒子喷射。
    /// 它只负责视觉发射，不修改 Hediff 或 Trion 事实。
    /// </summary>
    internal sealed class CombatBodyWoundSprayEmitter
    {
        /// <summary>
        /// 绑定的伤口 Hediff loadID。
        /// </summary>
        internal int HediffLoadId { get; }

        /// <summary>
        /// 切口倾斜角，用于读档后保持同一伤口喷射方向细节。
        /// </summary>
        internal float CutTilt
        {
            get { return cutTilt; }
        }

        /// <summary>
        /// 绑定的身体部位，用于查找原版伤口锚点。
        /// </summary>
        private readonly BodyPartRecord part;

        /// <summary>
        /// 每处伤口独立的切口倾斜角。
        /// </summary>
        private float cutTilt;

        /// <summary>
        /// 发射频率计数器。
        /// </summary>
        private int tickCounter;

        /// <summary>
        /// 上次缓存锚点时的 Pawn 朝向。
        /// </summary>
        private int cachedRotAsInt = -1;

        /// <summary>
        /// 当前朝向下的锚点偏移缓存。
        /// </summary>
        private Vector3 cachedAnchorOffset;

        /// <summary>
        /// 当前朝向下是否存在可用锚点。
        /// </summary>
        private bool cachedAnchorValid;

        /// <summary>
        /// 下次 Tick 是否需要执行受伤瞬间爆发。
        /// </summary>
        private bool pendingBurst;

        /// <summary>
        /// 锚点查找复用列表。
        /// </summary>
        private readonly List<BodyTypeDef.WoundAnchor> tmpAnchors =
            new List<BodyTypeDef.WoundAnchor>();

        /// <summary>
        /// 常规发射间隔，单位为 tick。
        /// </summary>
        private const int EmitInterval = 3;

        /// <summary>
        /// 每层常规发射粒子数。
        /// </summary>
        private const int EmitBurst = 3;

        /// <summary>
        /// 受伤瞬间爆发倍率。
        /// </summary>
        private const int BurstMultiplier = 4;

        /// <summary>
        /// 基础喷射锥角半角。
        /// </summary>
        private const float BaseConeHalfAngle = 6f;

        /// <summary>
        /// 粒子发射切口线段长度。
        /// </summary>
        private const float CutLength = 0.1f;

        /// <summary>
        /// 视野裁剪边距，单位为格。
        /// </summary>
        private const int CullMargin = 8;

        /// <summary>
        /// 创建单伤口喷射器。
        /// </summary>
        internal CombatBodyWoundSprayEmitter(Hediff hediff, float? savedCutTilt = null)
        {
            HediffLoadId = hediff != null ? hediff.loadID : 0;
            part = hediff?.Part;
            cutTilt = savedCutTilt ?? Rand.Range(-45f, 45f);
        }

        /// <summary>
        /// 标记下一次 Tick 执行爆发发射。
        /// </summary>
        internal void NotifyBurst()
        {
            pendingBurst = true;
        }

        /// <summary>
        /// 推进当前伤口的粒子发射。
        /// </summary>
        internal void Tick(Pawn pawn)
        {
            if (pendingBurst)
            {
                pendingBurst = false;
                EmitParticles(pawn, EmitBurst * BurstMultiplier);
            }

            tickCounter++;
            if (tickCounter < EmitInterval)
            {
                return;
            }

            tickCounter = 0;
            EmitParticles(pawn, EmitBurst);
        }

        /// <summary>
        /// 按当前伤口锚点发射三层 Trion 粒子。
        /// </summary>
        private void EmitParticles(Pawn pawn, int burstCount)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null || part == null)
            {
                return;
            }

            if (Find.CameraDriver != null && !Find.CameraDriver.CurrentViewRect.ExpandedBy(CullMargin).Contains(pawn.Position))
            {
                return;
            }

            int rotInt = pawn.Rotation.AsInt;
            if (rotInt != cachedRotAsInt)
            {
                cachedAnchorValid = TryGetAnchorOffset(pawn, out cachedAnchorOffset);
                cachedRotAsInt = rotInt;
            }

            if (!cachedAnchorValid)
            {
                return;
            }

            Vector3 anchorOffset = cachedAnchorOffset;
            float sprayAngle = anchorOffset.sqrMagnitude < 0.001f
                ? Rand.Range(0f, 360f)
                : Mathf.Atan2(anchorOffset.x, anchorOffset.z) * Mathf.Rad2Deg;

            float cutDirAngle = sprayAngle + 90f + cutTilt;
            float cutRad = cutDirAngle * Mathf.Deg2Rad;
            Vector3 cutDir = new Vector3(Mathf.Sin(cutRad), 0f, Mathf.Cos(cutRad));

            Vector3 center = pawn.DrawPos + anchorOffset;
            float halfLen = CutLength * 0.5f;
            Map map = pawn.Map;

            EmitLayer(
                map,
                center,
                cutDir,
                halfLen,
                sprayAngle,
                BaseConeHalfAngle,
                burstCount,
                WoundSprayFleckDefs.LeakCore,
                6f,
                12f,
                0.005f,
                0.009f);

            EmitLayer(
                map,
                center,
                cutDir,
                halfLen,
                sprayAngle,
                BaseConeHalfAngle * 1.7f,
                burstCount,
                WoundSprayFleckDefs.LeakMid,
                3f,
                7.5f,
                0.009f,
                0.017f);

            EmitLayer(
                map,
                center,
                cutDir,
                halfLen,
                sprayAngle,
                BaseConeHalfAngle * 2.7f,
                burstCount,
                WoundSprayFleckDefs.LeakOuter,
                0.75f,
                3f,
                0.013f,
                0.024f);
        }

        /// <summary>
        /// 发射单层 Fleck 粒子。
        /// </summary>
        private static void EmitLayer(
            Map map,
            Vector3 center,
            Vector3 cutDir,
            float halfLen,
            float sprayAngle,
            float coneHalf,
            int count,
            FleckDef fleckDef,
            float speedMin,
            float speedMax,
            float scaleMin,
            float scaleMax)
        {
            if (map == null || fleckDef == null || count <= 0)
            {
                return;
            }

            for (int index = 0; index < count; index++)
            {
                Vector3 origin = center + cutDir * Rand.Range(-halfLen, halfLen);
                float speed = Rand.Range(speedMin, speedMax);
                float angle = sprayAngle + Rand.Range(-coneHalf, coneHalf);

                FleckCreationData data = FleckMaker.GetDataStatic(
                    origin,
                    map,
                    fleckDef,
                    Rand.Range(scaleMin, scaleMax));
                data.velocityAngle = angle;
                data.velocitySpeed = speed;
                data.airTimeLeft = speed * 0.0125f;

                map.flecks.CreateFleck(data);
            }
        }

        /// <summary>
        /// 按原版 PawnWoundDrawer 的规则查找伤口锚点偏移。
        /// </summary>
        private bool TryGetAnchorOffset(Pawn pawn, out Vector3 anchorOffset)
        {
            anchorOffset = Vector3.zero;
            if (pawn?.story?.bodyType?.woundAnchors == null)
            {
                return false;
            }

            tmpAnchors.Clear();
            foreach (BodyTypeDef.WoundAnchor anchor in PawnDrawUtility.FindAnchors(pawn, part))
            {
                tmpAnchors.Add(anchor);
            }

            for (int index = tmpAnchors.Count - 1; index >= 0; index--)
            {
                BodyTypeDef.WoundAnchor anchor = tmpAnchors[index];
                if (!IsSupportedOverlayLayer(anchor.layer) || !PawnDrawUtility.AnchorUsable(pawn, anchor, pawn.Rotation))
                {
                    tmpAnchors.RemoveAt(index);
                }
            }

            if (tmpAnchors.Count == 0)
            {
                return false;
            }

            BodyTypeDef.WoundAnchor selectedAnchor = tmpAnchors.RandomElement();
            PawnDrawUtility.CalcAnchorData(pawn, selectedAnchor, pawn.Rotation, out anchorOffset, out _);

            if (selectedAnchor.layer == PawnOverlayDrawer.OverlayLayer.Head)
            {
                Vector2 headOffset = pawn.story.bodyType.headOffset;
                anchorOffset += new Vector3(headOffset.x, 0f, headOffset.y);
            }

            return true;
        }

        /// <summary>
        /// 只接受原版伤口覆盖层使用的 Body 和 Head 锚点。
        /// </summary>
        private static bool IsSupportedOverlayLayer(PawnOverlayDrawer.OverlayLayer layer)
        {
            return layer == PawnOverlayDrawer.OverlayLayer.Body ||
                layer == PawnOverlayDrawer.OverlayLayer.Head;
        }
    }
}
