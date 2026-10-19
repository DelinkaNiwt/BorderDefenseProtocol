using BDP.Core.AttackExecution;
using BDP.Core.Projectiles.Visual;
using Verse;

namespace BDP.Content.RangedModules.HighAngleShooting
{
    /// <summary>
    /// 独立高角射击模块配置。
    /// </summary>
    public sealed class HighAngleShootingConfig : RangedModuleConfigNode, IExposable
    {
        /// <summary>
        /// 不可视目标使用的替代投射物 Def 名称。
        /// 该 Def 应继承普通投射物，并只开启原版 flyOverhead。
        /// </summary>
        public string OverheadProjectileDefName;

        /// <summary>
        /// 高角轨迹起点的绘制偏移。
        /// </summary>
        public float StartHeight;

        /// <summary>
        /// 高角轨迹终点的绘制偏移。
        /// </summary>
        public float EndHeight;

        /// <summary>
        /// 高角轨迹中段的最高绘制偏移。
        /// </summary>
        public float ArcHeight = 2.4f;

        /// <summary>
        /// 是否允许高角射击把普通薄屋顶下的目标作为目标。
        /// 厚屋顶/山顶屋顶不受此开关影响，始终禁止。
        /// </summary>
        public bool AllowTargetUnderThinRoof = false;

        /// <summary>
        /// 创建独立的配置快照。
        /// </summary>
        public override RangedModuleConfigNode Clone()
        {
            return new HighAngleShootingConfig
            {
                OverheadProjectileDefName = OverheadProjectileDefName,
                StartHeight = StartHeight,
                EndHeight = EndHeight,
                ArcHeight = ArcHeight,
                AllowTargetUnderThinRoof = AllowTargetUnderThinRoof
            };
        }

        /// <summary>
        /// 将模块配置转换为 Core（核心层）可消费的中性视觉轨迹参数。
        /// </summary>
        public ProjectileVisualTrajectoryProfile CreateVisualTrajectoryProfile()
        {
            return new ProjectileVisualTrajectoryProfile
            {
                StartOffset = StartHeight,
                EndOffset = EndHeight,
                ArcOffset = ArcHeight
            };
        }

        /// <summary>
        /// 持久化模块配置。
        /// </summary>
        public void ExposeData()
        {
            string overheadProjectileDefName = OverheadProjectileDefName;
            float startHeight = StartHeight;
            float endHeight = EndHeight;
            float arcHeight = ArcHeight;
            bool allowTargetUnderThinRoof = AllowTargetUnderThinRoof;

            Scribe_Values.Look(ref overheadProjectileDefName, "overheadProjectileDefName");
            Scribe_Values.Look(ref startHeight, "startHeight", 0f);
            Scribe_Values.Look(ref endHeight, "endHeight", 0f);
            Scribe_Values.Look(ref arcHeight, "arcHeight", 2.4f);
            Scribe_Values.Look(ref allowTargetUnderThinRoof, "allowTargetUnderThinRoof", false);

            OverheadProjectileDefName = overheadProjectileDefName;
            StartHeight = startHeight;
            EndHeight = endHeight;
            ArcHeight = arcHeight;
            AllowTargetUnderThinRoof = allowTargetUnderThinRoof;
        }
    }
}
