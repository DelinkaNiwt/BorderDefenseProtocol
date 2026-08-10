using UnityEngine;
using Verse;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// 原版 DrawEquipmentAiming 边界采样到的装备姿态。
    /// 它保存绘制和枪口发射共同需要的宿主基准，不保存任何表达判断。
    /// </summary>
    internal sealed class EquipmentPoseSample
    {
        /// <summary>
        /// 当前样本绑定的已发布投影版本号。
        /// </summary>
        public int ProjectionVersion { get; set; }

        /// <summary>
        /// 原版传入的装备实际持握绘制点。
        /// 它包含小人动画和原版后坐力偏移，不能用 Pawn.DrawPos 替代。
        /// </summary>
        public Vector3 DrawLoc { get; set; }

        /// <summary>
        /// 原版传入的瞄准角度。
        /// 枪口偏移按这个角度旋转。
        /// </summary>
        public float AimAngle { get; set; }

        /// <summary>
        /// 当前 Pawn 绘制朝向。
        /// 它用于选择南北或东西姿态分支。
        /// </summary>
        public Rot4 Facing { get; set; }

        /// <summary>
        /// 当前样本采集时的游戏 tick。
        /// 仅用于诊断和未来过期策略，不参与当前裁决。
        /// </summary>
        public int SampleTick { get; set; }

        /// <summary>
        /// 当前样本是否绑定到有效投影。
        /// </summary>
        public bool IsValid
        {
            get { return ProjectionVersion > 0; }
        }

        /// <summary>
        /// 判断当前样本是否可用于指定投影版本。
        /// </summary>
        public bool IsValidForProjection(int projectionVersion)
        {
            return IsValid && projectionVersion > 0 && ProjectionVersion == projectionVersion;
        }

        /// <summary>
        /// 构建一份装备姿态样本。
        /// </summary>
        public static EquipmentPoseSample Create(
            int projectionVersion,
            Vector3 drawLoc,
            float aimAngle,
            Rot4 facing,
            int sampleTick)
        {
            return new EquipmentPoseSample
            {
                ProjectionVersion = projectionVersion,
                DrawLoc = drawLoc,
                AimAngle = aimAngle,
                Facing = facing,
                SampleTick = sampleTick
            };
        }
    }
}
