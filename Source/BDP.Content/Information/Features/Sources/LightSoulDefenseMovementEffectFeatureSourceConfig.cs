using BDP.Content.Information.Features.Workers;

namespace BDP.Content.Information.Features.Sources
{
    /// <summary>读取光魂防护形态实际声明的移动速度效果。</summary>
    public sealed class LightSoulDefenseMovementEffectFeatureSourceConfig : ExtraEffectFeatureSourceConfig
    {
        /// <summary>绑定只读取光魂防护形态 Hediff 阶段移动速度倍率的 Worker。</summary>
        public LightSoulDefenseMovementEffectFeatureSourceConfig()
        {
            WorkerClass = typeof(LightSoulDefenseMovementEffectFeatureSourceWorker);
        }
    }
}
