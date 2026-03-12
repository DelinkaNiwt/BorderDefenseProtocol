using UnityEngine;
using Verse;

namespace BDP.Glow.Controllers
{
    /// <summary>
    /// 静态发光控制器——固定强度，无动画，无状态。
    ///
    /// 适用场景：芯片物品、始终发光的建筑等。
    /// 无需序列化（无运行时状态）。
    /// </summary>
    public class StaticGlowController : IGlowController
    {
        private float intensity = 1.0f;

        public void Initialize(Thing parent, GlowControllerParams parms)
        {
            if (parms is StaticGlowParams staticParms)
                intensity = staticParms.intensity;
            // parms为null时使用默认强度1.0
        }

        public void Tick()
        {
            // 静态控制器无需每tick更新
        }

        public float GetGlowIntensity() => intensity;

        public Color? GetGlowColor() => null; // 使用graphicData配置的颜色

        public void ExposeData()
        {
            // 无状态，无需序列化
        }
    }
}
