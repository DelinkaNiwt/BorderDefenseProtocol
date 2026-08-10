using RimWorld;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技条目的 VerbProps 增量覆盖层。
    /// 它只承载作者在组合技条目中显式声明"我要改这个值"的字段。
    /// 未声明的字段保持 null——代码永远以 Main 侧完整数据为基底，
    /// 只在 overlay 非 null 的字段上做覆盖。
    ///
    /// 这从根本上消除了"条目空壳被误当完整 VerbProps"这一类设计错误：
    /// VerbPropsOverlay 根本不能当作 VerbProperties 使用，
    /// 它只是一个字段级的 delta diff。
    /// </summary>
    public sealed class VerbPropsOverlay
    {
        /// <summary>
        /// 显式覆盖的默认投射物。
        /// </summary>
        public ThingDef defaultProjectile;

        /// <summary>
        /// 显式覆盖的 Verb 类型。
        /// </summary>
        public System.Type verbClass;

        /// <summary>
        /// 显式覆盖的标签文本。
        /// </summary>
        public string label;

        /// <summary>
        /// 显式覆盖的是否有标准命令。
        /// </summary>
        public bool? hasStandardCommand;

        /// <summary>
        /// 显式覆盖的开火音效。
        /// </summary>
        public SoundDef soundCast;

        /// <summary>
        /// 显式覆盖的射程。
        /// null 表示不覆盖，走 VerbPropsResolve 或基底。
        /// </summary>
        public float? range;

        /// <summary>
        /// 显式覆盖的暖机时间。
        /// </summary>
        public float? warmupTime;

        /// <summary>
        /// 显式覆盖的冷却时间。
        /// </summary>
        public float? defaultCooldownTime;

        /// <summary>
        /// 显式覆盖的 burst 发射数。
        /// </summary>
        public int? burstShotCount;

        /// <summary>
        /// 显式覆盖的 burst 内发射间隔。
        /// </summary>
        public int? ticksBetweenBurstShots;

        /// <summary>
        /// 显式覆盖的最小射程。
        /// </summary>
        public float? minRange;

        /// <summary>
        /// 显式覆盖的强制偏移半径。
        /// </summary>
        public float? forcedMissRadius;

        /// <summary>
        /// 显式覆盖的触距精度。
        /// </summary>
        public float? accuracyTouch;

        /// <summary>
        /// 显式覆盖的近距离精度。
        /// </summary>
        public float? accuracyShort;

        /// <summary>
        /// 显式覆盖的中距离精度。
        /// </summary>
        public float? accuracyMedium;

        /// <summary>
        /// 显式覆盖的远距离精度。
        /// </summary>
        public float? accuracyLong;

        /// <summary>
        /// 显式覆盖的目标参数（canTargetLocations / canTargetBuildings / canTargetPawns）。
        /// null 表示不覆盖，走基底。
        /// </summary>
        public TargetingParameters targetParams;
    }
}
