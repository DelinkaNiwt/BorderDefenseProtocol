using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 单发投射物在正式开火时冻结的原版精度事实。
    /// 只承载原版射击报告的公开结果，不解释任何具体弹道业务。
    /// </summary>
    public sealed class ProjectileAccuracySnapshot : IExposable
    {
        /// <summary>当前快照是否来自一次有效的原版射击报告。</summary>
        public bool IsAvailable { get; set; }

        /// <summary>忽略目标体型与姿态前的标准目标瞄准概率。</summary>
        public float StandardAimChance { get; set; } = 0.5f;

        /// <summary>包含目标体型、但忽略姿态后的瞄准概率。</summary>
        public float IgnoringPostureAimChance { get; set; } = 0.5f;

        /// <summary>原版射击报告给出的掩体通过概率。</summary>
        public float PassCoverChance { get; set; } = 1f;

        /// <summary>当前发射计划冻结的强制失准半径。</summary>
        public float ForcedMissRadius { get; set; }

        /// <summary>当前发射计划在原版报告外追加的精度倍率。</summary>
        public float AccuracyFactor { get; set; } = 1f;

        /// <summary>深度复制当前快照。</summary>
        public ProjectileAccuracySnapshot CloneTyped()
        {
            return new ProjectileAccuracySnapshot
            {
                IsAvailable = IsAvailable,
                StandardAimChance = StandardAimChance,
                IgnoringPostureAimChance = IgnoringPostureAimChance,
                PassCoverChance = PassCoverChance,
                ForcedMissRadius = ForcedMissRadius,
                AccuracyFactor = AccuracyFactor
            };
        }

        /// <summary>存档读写当前精度事实。</summary>
        public void ExposeData()
        {
            bool isAvailable = IsAvailable;
            float standardAimChance = StandardAimChance;
            float ignoringPostureAimChance = IgnoringPostureAimChance;
            float passCoverChance = PassCoverChance;
            float forcedMissRadius = ForcedMissRadius;
            float accuracyFactor = AccuracyFactor;

            Scribe_Values.Look(ref isAvailable, "isAvailable", false);
            Scribe_Values.Look(ref standardAimChance, "standardAimChance", 0.5f);
            Scribe_Values.Look(ref ignoringPostureAimChance, "ignoringPostureAimChance", 0.5f);
            Scribe_Values.Look(ref passCoverChance, "passCoverChance", 1f);
            Scribe_Values.Look(ref forcedMissRadius, "forcedMissRadius", 0f);
            Scribe_Values.Look(ref accuracyFactor, "accuracyFactor", 1f);

            IsAvailable = isAvailable;
            StandardAimChance = Mathf.Clamp01(standardAimChance);
            IgnoringPostureAimChance = Mathf.Clamp01(ignoringPostureAimChance);
            PassCoverChance = Mathf.Clamp01(passCoverChance);
            ForcedMissRadius = Mathf.Max(0f, forcedMissRadius);
            AccuracyFactor = accuracyFactor > 0f ? accuracyFactor : 1f;
        }
    }
}
