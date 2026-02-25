using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 拖尾模块——从Bullet_BDP提取的光束拖尾逻辑。
    /// Priority=100（视觉效果，最后执行）。
    ///
    /// 管线接口：IBDPTickObserver（每tick只读观察，创建拖尾线段）。
    ///
    /// 每tick在宿主移动后创建一段BDPTrailSegment(prev→current)，
    /// 由BDPEffectMapComponent统一管理渲染和生命周期。
    /// </summary>
    public class TrailModule : IBDPProjectileModule, IBDPTickObserver
    {
        /// <summary>拖尾配置引用（来自ThingDef.modExtensions）。</summary>
        private readonly BeamTrailConfig config;

        /// <summary>缓存的Material（首次OnTick时延迟初始化，避免读档时跨线程加载）。</summary>
        private Material trailMat;

        /// <summary>是否已尝试初始化Material。</summary>
        private bool matResolved;

        /// <summary>上一tick的位置（用于创建线段）。</summary>
        private Vector3 prevPos;

        public int Priority => 100;

        /// <summary>无参构造——Scribe反序列化需要。</summary>
        public TrailModule() { config = null; }

        public TrailModule(BeamTrailConfig config)
        {
            this.config = config;
        }

        public void OnSpawn(Bullet_BDP host)
        {
            prevPos = host.DrawPos;
            // Material延迟到OnTick初始化，避免读档时在加载线程调用MaterialPool.MatFrom
        }

        /// <summary>延迟初始化Material（确保在主线程执行）。</summary>
        private void EnsureMaterial(Bullet_BDP host)
        {
            if (matResolved) return;
            matResolved = true;
            var cfg = config ?? host.def.GetModExtension<BeamTrailConfig>();
            if (cfg != null && cfg.enabled)
            {
                trailMat = MaterialPool.MatFrom(
                    cfg.trailTexPath,
                    ShaderDatabase.MoteGlow,
                    cfg.trailColor);
            }
        }

        /// <summary>
        /// IBDPTickObserver实现——每tick创建拖尾线段。
        /// </summary>
        public void OnTick(Bullet_BDP host)
        {
            EnsureMaterial(host);
            var cfg = config ?? host.def.GetModExtension<BeamTrailConfig>();
            if (cfg == null || !cfg.enabled || trailMat == null) return;

            Vector3 curPos = host.DrawPos;
            var comp = BDPEffectMapComponent.GetInstance(host.Map);
            comp?.CreateSegment(
                prevPos, curPos, trailMat, cfg.trailColor,
                cfg.trailWidth, cfg.segmentDuration,
                cfg.startOpacity, cfg.decayTime, cfg.decaySharpness);
            prevPos = curPos;
        }

        /// <summary>配置来自def，无需额外序列化。</summary>
        public void ExposeData() { }
    }
}
