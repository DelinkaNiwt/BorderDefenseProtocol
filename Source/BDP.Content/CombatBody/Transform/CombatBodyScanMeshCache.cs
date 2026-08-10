using UnityEngine;

namespace BDP.Content.CombatBody.Transform
{
    /// <summary>
    /// 战斗体扫描衣物使用的预建裁切平面缓存。
    /// 以固定档位换取绘制阶段零网格分配，并让几何边界与纹理坐标同步裁切。
    /// </summary>
    internal static class CombatBodyScanMeshCache
    {
        /// <summary>
        /// 从全保留到全消失之间的固定裁切档数。
        /// </summary>
        private const int CutStepCount = 24;

        /// <summary>
        /// 按“保留上段、水平翻转、裁切档位”索引的预建网格。
        /// </summary>
        private static readonly Mesh[,,] Meshes = new Mesh[2, 2, CutStepCount + 1];

        /// <summary>
        /// 首次访问类型时一次性预建全部裁切网格。
        /// </summary>
        static CombatBodyScanMeshCache()
        {
            for (int keepUpperIndex = 0; keepUpperIndex < 2; keepUpperIndex++)
            {
                for (int flippedIndex = 0; flippedIndex < 2; flippedIndex++)
                {
                    for (int step = 0; step <= CutStepCount; step++)
                    {
                        Meshes[keepUpperIndex, flippedIndex, step] = BuildMesh(
                            keepUpperIndex == 1,
                            flippedIndex == 1,
                            (float)step / CutStepCount);
                    }
                }
            }
        }

        /// <summary>
        /// 构造一个单位裁切平面。
        /// </summary>
        private static Mesh BuildMesh(bool keepUpper, bool flipped, float cut)
        {
            float bottom = keepUpper ? -0.5f + cut : -0.5f;
            float top = keepUpper ? 0.5f : -0.5f + cut;
            float bottomV = keepUpper ? cut : 0f;
            float topV = keepUpper ? 1f : cut;
            float leftU = flipped ? 1f : 0f;
            float rightU = flipped ? 0f : 1f;

            Mesh mesh = new Mesh
            {
                name = "BDP_CombatBodyScanClip_" + (keepUpper ? "Upper_" : "Lower_")
                    + (flipped ? "Flipped_" : "Normal_") + cut.ToString("0.000")
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, bottom),
                new Vector3(-0.5f, 0f, top),
                new Vector3(0.5f, 0f, top),
                new Vector3(0.5f, 0f, bottom)
            };
            mesh.uv = new[]
            {
                new Vector2(leftU, bottomV),
                new Vector2(leftU, topV),
                new Vector2(rightU, topV),
                new Vector2(rightU, bottomV)
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>
        /// 在 Mote 生成阶段提前触发静态缓存初始化，避免首帧绘制创建网格。
        /// </summary>
        internal static void WarmUp()
        {
        }

        /// <summary>
        /// 夹取并量化裁切值，返回已经预建的共享网格。
        /// </summary>
        internal static Mesh GetMesh(bool keepUpper, bool flipped, float normalizedCut)
        {
            float clampedCut = Mathf.Clamp01(normalizedCut);
            int step = Mathf.RoundToInt(clampedCut * CutStepCount);
            return Meshes[keepUpper ? 1 : 0, flipped ? 1 : 0, step];
        }
    }
}
