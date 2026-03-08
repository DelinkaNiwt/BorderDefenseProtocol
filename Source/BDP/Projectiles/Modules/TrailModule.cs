using BDP.Projectiles.Pipeline;
using BDP.Projectiles.Config;
using UnityEngine;
using Verse;

namespace BDP.Projectiles.Modules
{
    /// <summary>
    /// 拖尾模块——从Bullet_BDP提取的光束拖尾逻辑。
    /// Priority=100（视觉效果，最后执行）。
    ///
    /// v5管线接口：IBDPVisualObserver（每tick只读观察，零副作用）。
    ///
    /// 每tick在宿主移动后创建一段BDPTrailSegment(prev→current)，
    /// 由BDPEffectMapComponent统一管理渲染和生命周期。
    /// </summary>
    public class TrailModule : IBDPProjectileModule, IBDPVisualObserver
    {
        /// <summary>拖尾配置引用（来自ThingDef.modExtensions）。</summary>
        private readonly BeamTrailConfig config;

        /// <summary>缓存的Material（首次Observe时延迟初始化，避免读档时跨线程加载）。</summary>
        private Material trailMat;

        /// <summary>是否已尝试初始化Material。</summary>
        private bool matResolved;

        /// <summary>上一tick的位置（用于创建线段）。</summary>
        private Vector3 prevPos;

        /// <summary>是否已初始化prevPos（首次Observe时延迟初始化）。</summary>
        private bool prevPosInitialized;

        /// <summary>实例ID（诊断用）。</summary>
        private readonly int instanceId;
        private static int nextInstanceId = 1;

        public int Priority => 100;

        /// <summary>无参构造——Scribe反序列化需要。</summary>
        public TrailModule()
        {
            config = null;
            instanceId = nextInstanceId++;
        }

        public TrailModule(BeamTrailConfig config)
        {
            this.config = config;
            instanceId = nextInstanceId++;
        }

        public void OnSpawn(Bullet_BDP host)
        {
            // Material延迟到OnVisualInit/Observe初始化，避免读档时在加载线程调用MaterialPool.MatFrom
        }

        /// <summary>
        /// IBDPVisualObserver.OnVisualInit实现——在首次base.TickInterval之前调用。
        /// 此时DrawPos ≈ 发射原点，朝飞行方向偏移muzzleOffset格，
        /// 模拟拖尾从枪口而非pawn中心开始。
        /// </summary>
        public void OnVisualInit(Bullet_BDP host)
        {
            EnsureMaterial(host);
            var cfg = config ?? host.def.GetModExtension<BeamTrailConfig>();
            float offset = cfg?.muzzleOffset ?? 0.6f;
            // FlightDirection已忽略Y轴，偏移后保持与DrawPos相同的高度层
            prevPos = host.DrawPos + host.FlightDirection * offset;
            prevPosInitialized = true;
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
        /// IBDPVisualObserver.Observe实现——每tick创建拖尾线段（零副作用）。
        /// prevPos已由OnVisualInit正确初始化，此方法只负责输出线段。
        /// </summary>
        public void Observe(Bullet_BDP host)
        {
            if (!prevPosInitialized || trailMat == null) return;
            var cfg = config ?? host.def.GetModExtension<BeamTrailConfig>();
            if (cfg == null || !cfg.enabled) return;

            Vector3 curPos = host.DrawPos;
            var comp = BDPEffectMapComponent.GetInstance(host.Map);
            comp?.CreateSegment(
                prevPos, curPos, trailMat, cfg.trailColor,
                cfg.trailWidth, cfg.segmentDuration,
                cfg.startOpacity, cfg.decayTime, cfg.decaySharpness);
            prevPos = curPos;
        }

        /// <summary>配置来自def，无需额外序列化。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref prevPosInitialized, "prevPosInitialized", false);
        }
    }
}
