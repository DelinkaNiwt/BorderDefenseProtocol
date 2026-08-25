using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片激活阶段的可选音效配置。
    /// 该配置只声明音效，不负责播放时机；播放时机由 Trigger 通用运行时设施决定。
    /// </summary>
    public sealed class ChipActivationAudioConfig : IExposable
    {
        /// <summary>
        /// 启用前摇开始时播放的一次性音效。
        /// </summary>
        public SoundDef ActivationWarmupStartSound;

        /// <summary>
        /// 启用前摇期间持续维护的循环音效。
        /// </summary>
        public SoundDef ActivationWarmupLoopSound;

        /// <summary>
        /// 芯片正式激活完成时播放的一次性音效。
        /// </summary>
        public SoundDef ActivationWarmupEndSound;

        /// <summary>
        /// RimWorld XML 反序列化兼容口。
        /// 音效字段由 Def 加载器直接写入，此处不保存运行时播放状态。
        /// </summary>
        public void ExposeData()
        {
        }
    }
}
