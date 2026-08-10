using BDP.Core.CombatBody.Presentation;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.CombatBody.Transform
{
    /// <summary>
    /// 战斗体低开销扫描 Mote。
    /// 每帧互补裁切形态切换前后的完整人物快照，并叠加一条白色核心光与青绿色柔光。
    /// </summary>
    public sealed class Mote_CombatBodyScan : MoteAttached
    {
        /// <summary>
        /// 扫描动画总时长，约为游戏内 0.17 秒。
        /// </summary>
        internal const int DurationTicks = 10;

        /// <summary>
        /// 原版完整人物绘制替代的异常兜底时长。
        /// 比动画多保留两 tick，正常情况下由 Mote 销毁立即释放。
        /// </summary>
        private const int SuppressionTimeoutTicks = DurationTicks + 2;

        /// <summary>
        /// 头部完整方形网格中用于估算肉眼可见头顶的半高系数。
        /// </summary>
        private const float HeadUpperBoundFactor = 0.38f;

        /// <summary>
        /// 完整人物快照覆盖层的海拔偏移。
        /// </summary>
        private const float SnapshotAltitudeOffset = 0.004f;

        /// <summary>
        /// 原版人物缓存相机的正交半尺寸，对应世界中边长为二的快照平面。
        /// </summary>
        private const float SnapshotHalfSize = 1f;

        /// <summary>
        /// 白色核心光厚度。
        /// </summary>
        private const float CoreThickness = 0.075f;

        /// <summary>
        /// 青绿色柔光厚度。
        /// </summary>
        private const float HaloThickness = 0.24f;

        /// <summary>
        /// 扫描光复用的现有柔边纹理材质。
        /// 类型初始化发生在 Mote 创建阶段，不进入逐帧绘制分配。
        /// </summary>
        private static readonly Material ScanMaterial = MaterialPool.MatFrom(
            "BDP/Effects/LeakParticle",
            ShaderDatabase.MoteGlow);

        /// <summary>
        /// 白色核心光材质属性块。
        /// </summary>
        private readonly MaterialPropertyBlock corePropertyBlock = new MaterialPropertyBlock();

        /// <summary>
        /// 青绿色柔光材质属性块。
        /// </summary>
        private readonly MaterialPropertyBlock haloPropertyBlock = new MaterialPropertyBlock();

        /// <summary>
        /// 本次动画跟随的 Pawn。
        /// </summary>
        private Pawn pawn;

        /// <summary>
        /// 本次扫描方向。
        /// </summary>
        private CombatBodyTransformDirection direction;

        /// <summary>
        /// 形态切换前冻结的完整人物最终画面。
        /// </summary>
        private CombatBodyPawnVisualSnapshot outgoingSnapshot;

        /// <summary>
        /// 形态切换完成后冻结的完整人物最终画面。
        /// </summary>
        private CombatBodyPawnVisualSnapshot incomingSnapshot;

        /// <summary>
        /// 已推进的游戏 tick 数。
        /// </summary>
        private int elapsedTicks;

        /// <summary>
        /// 本轮原版完整人物绘制替代令牌。
        /// </summary>
        private int suppressionToken;

        /// <summary>
        /// 在生成前注入本次扫描所需的 Pawn、方向与两张完整人物快照。
        /// </summary>
        internal void Configure(
            Pawn targetPawn,
            CombatBodyTransformDirection transformDirection,
            CombatBodyPawnVisualSnapshot outgoingVisualSnapshot,
            CombatBodyPawnVisualSnapshot incomingVisualSnapshot)
        {
            pawn = targetPawn;
            direction = transformDirection;
            outgoingSnapshot = outgoingVisualSnapshot;
            incomingSnapshot = incomingVisualSnapshot;
        }

        /// <summary>
        /// Mote 成功生成后才用两张完整快照接管原版人物绘制。
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            suppressionToken = CombatBodyPawnRenderSuppression.Begin(
                pawn,
                SuppressionTimeoutTicks);
        }

        /// <summary>
        /// 销毁 Mote 前恢复原版人物绘制，并归还两张完整人物快照。
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            CombatBodyPawnRenderSuppression.End(pawn, suppressionToken);
            suppressionToken = 0;
            CombatBodyPawnVisualCapture.Release(outgoingSnapshot);
            CombatBodyPawnVisualCapture.Release(incomingSnapshot);
            outgoingSnapshot = null;
            incomingSnapshot = null;
            base.Destroy(mode);
        }

        /// <summary>
        /// 按游戏 tick 推进动画，并在第 10 tick 后销毁。
        /// </summary>
        protected override void TimeInterval(float deltaTime)
        {
            base.TimeInterval(deltaTime);
            if (Destroyed)
            {
                return;
            }

            elapsedTicks++;
            if (elapsedTicks >= DurationTicks)
            {
                Destroy();
            }
        }

        /// <summary>
        /// 绘制两侧互补的完整人物快照裁切层和双层扫描光。
        /// </summary>
        protected override void DrawAt(Vector3 drawLoc, bool flip)
        {
            if (!CanDrawNow())
            {
                return;
            }

            Rot4 facing = pawn.Rotation;
            float bodyWidth = HumanlikeMeshPoolUtility.HumanlikeBodyWidthForPawn(pawn);
            float headWidth = HumanlikeMeshPoolUtility.HumanlikeHeadWidthForPawn(pawn);
            Vector3 headOffset = pawn.Drawer.renderer.BaseHeadOffsetAt(facing);
            float lowerBound = Mathf.Min(-bodyWidth * 0.5f, headOffset.z - headWidth * 0.5f);
            float upperBound = Mathf.Max(bodyWidth * 0.5f, headOffset.z + headWidth * HeadUpperBoundFactor);
            float progress = Mathf.Clamp01((float)elapsedTicks / (DurationTicks - 1));
            float localScanZ = direction == CombatBodyTransformDirection.Enter
                ? Mathf.Lerp(upperBound, lowerBound, progress)
                : Mathf.Lerp(lowerBound, upperBound, progress);

            Vector3 basePosition = exactPosition;
            bool outgoingKeepUpper = direction == CombatBodyTransformDirection.Exit;
            DrawSnapshot(outgoingSnapshot, outgoingKeepUpper, basePosition, localScanZ);
            DrawSnapshot(incomingSnapshot, !outgoingKeepUpper, basePosition, localScanZ);
            DrawScanLight(basePosition, bodyWidth, headWidth, localScanZ);
        }

        /// <summary>
        /// 判断当前帧是否仍适合绘制该短命特效。
        /// </summary>
        private bool CanDrawNow()
        {
            return pawn != null
                && !pawn.Destroyed
                && pawn.Spawned
                && pawn.Map == Map
                && pawn.Drawer?.renderer != null
                && Find.CameraDriver != null
                && (Find.UIRoot == null || !Find.UIRoot.HideMotes)
                && Find.CameraDriver.CurrentViewRect.ExpandedBy(4).Contains(pawn.Position);
        }

        /// <summary>
        /// 按指定保留方向绘制一张完整人物快照的裁切部分。
        /// </summary>
        private static void DrawSnapshot(
            CombatBodyPawnVisualSnapshot snapshot,
            bool keepUpper,
            Vector3 basePosition,
            float localScanZ)
        {
            if (snapshot?.Material == null)
            {
                return;
            }

            float normalizedCut = Mathf.InverseLerp(
                -SnapshotHalfSize,
                SnapshotHalfSize,
                localScanZ);
            if ((!keepUpper && normalizedCut <= 0f) || (keepUpper && normalizedCut >= 1f))
            {
                return;
            }

            Mesh mesh = CombatBodyScanMeshCache.GetMesh(keepUpper, false, normalizedCut);
            Vector3 center = basePosition;
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor() + SnapshotAltitudeOffset;
            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                Quaternion.identity,
                new Vector3(SnapshotHalfSize * 2f, 1f, SnapshotHalfSize * 2f));
            Graphics.DrawMesh(mesh, matrix, snapshot.Material, 0, null, 0, null);
        }

        /// <summary>
        /// 在完整人物快照覆盖层上方绘制青绿柔光和白色细核心。
        /// </summary>
        private void DrawScanLight(
            Vector3 basePosition,
            float bodyWidth,
            float headWidth,
            float localScanZ)
        {
            float width = Mathf.Max(bodyWidth, headWidth) * 1.12f;
            float altitude = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.02f;
            Vector3 haloPosition = new Vector3(basePosition.x, altitude, basePosition.z + localScanZ);
            Vector3 corePosition = haloPosition;
            corePosition.y += 0.002f;

            haloPropertyBlock.SetColor(
                ShaderPropertyIDs.Color,
                new Color(0.25f, 1f, 0.82f, 0.48f));
            corePropertyBlock.SetColor(
                ShaderPropertyIDs.Color,
                new Color(0.94f, 1f, 1f, 0.98f));

            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(haloPosition, Quaternion.identity, new Vector3(width, 1f, HaloThickness)),
                ScanMaterial,
                0,
                null,
                0,
                haloPropertyBlock);
            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(corePosition, Quaternion.identity, new Vector3(width * 0.96f, 1f, CoreThickness)),
                ScanMaterial,
                0,
                null,
                0,
                corePropertyBlock);
        }
    }
}
