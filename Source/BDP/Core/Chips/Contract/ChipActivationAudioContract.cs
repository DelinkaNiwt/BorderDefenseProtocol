using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片激活音效的运行时只读声明结果。
    /// </summary>
    internal sealed class ChipActivationAudioContract
    {
        /// <summary>
        /// 启用前摇开始音效。
        /// </summary>
        public SoundDef WarmupStartSound;

        /// <summary>
        /// 启用前摇持续音效。
        /// </summary>
        public SoundDef WarmupLoopSound;

        /// <summary>
        /// 正式激活完成音效。
        /// </summary>
        public SoundDef WarmupEndSound;
    }
}
