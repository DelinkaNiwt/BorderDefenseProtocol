using UnityEngine;

namespace BDP.Projectiles
{
    /// <summary>
    /// 角度/方向向量转换工具类。
    /// 提供XZ平面上角度与方向向量的互转,用于投射物追踪计算。
    /// </summary>
    public static class BDPAngleHelper
    {
        /// <summary>
        /// 将角度(度)转换为XZ平面单位方向向量。
        /// 0度对应北方向(+Z轴),顺时针旋转。
        /// </summary>
        /// <param name="angleDeg">角度(度)</param>
        /// <returns>XZ平面单位方向向量</returns>
        public static Vector3 AngleToDirection(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        /// <summary>
        /// 将XZ平面方向向量转换为角度(度)。
        /// 返回值范围[-180, 180],0度对应北方向(+Z轴)。
        /// </summary>
        /// <param name="dirXZ">XZ平面方向向量</param>
        /// <returns>角度(度)</returns>
        public static float DirectionToAngle(Vector3 dirXZ)
        {
            return Mathf.Atan2(dirXZ.x, dirXZ.z) * Mathf.Rad2Deg;
        }
    }
}
