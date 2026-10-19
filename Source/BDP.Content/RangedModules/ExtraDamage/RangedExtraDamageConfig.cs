using BDP.Core.AttackExecution;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using Verse;

namespace BDP.Content.RangedModules.ExtraDamage
{
    /// <summary>
    /// 远程额外伤害的计算方式。
    /// </summary>
    public enum RangedExtraDamageAmountMode
    {
        /// <summary>
        /// 使用固定额外伤害量。
        /// </summary>
        Fixed,

        /// <summary>
        /// 使用目标当前存在身体部件的最大血量总和乘以倍率。
        /// </summary>
        BodyPartMaxHealthSumPercent,

        /// <summary>
        /// 使用实际命中身体部件的最大血量乘以倍率。
        /// </summary>
        HitPartMaxHealthPercent
    }

    /// <summary>
    /// 通用远程额外伤害模块的 Content 配置快照。
    /// </summary>
    public sealed class RangedExtraDamageConfig : RangedModuleConfigNode
    {
        /// <summary>
        /// 当前额外伤害的计算方式。
        /// </summary>
        public RangedExtraDamageAmountMode AmountMode = RangedExtraDamageAmountMode.Fixed;

        /// <summary>
        /// 固定额外伤害量。
        /// </summary>
        public float Amount;

        /// <summary>
        /// 身体部件最大血量总和的额外伤害倍率。
        /// </summary>
        public float Ratio;

        /// <summary>
        /// 额外伤害类型；为空时继承当前投射物伤害类型。
        /// </summary>
        public DamageDef DamageDef;

        /// <summary>
        /// 额外伤害护甲穿透；负值表示继承当前投射物护甲穿透。
        /// </summary>
        public float ArmorPenetration = -1f;

        /// <summary>
        /// 额外伤害的命中部位解析策略。
        /// </summary>
        public HitPartResolutionMode HitPartResolutionMode = HitPartResolutionMode.Independent;

        /// <summary>
        /// 是否只允许 Pawn（人形单位）目标。
        /// </summary>
        public bool OnlyPawns = true;

        /// <summary>
        /// 复制当前强类型配置。
        /// </summary>
        public RangedExtraDamageConfig CloneTyped()
        {
            return new RangedExtraDamageConfig
            {
                AmountMode = AmountMode,
                Amount = Amount,
                Ratio = Ratio,
                DamageDef = DamageDef,
                ArmorPenetration = ArmorPenetration,
                HitPartResolutionMode = HitPartResolutionMode,
                OnlyPawns = OnlyPawns
            };
        }

        /// <summary>
        /// 生成配置快照。
        /// </summary>
        public override RangedModuleConfigNode Clone()
        {
            return CloneTyped();
        }
    }
}
