using RimWorld;
using Verse;

namespace BDP.Content.BounceSlash
{
    /// <summary>
    /// 弹射砍击 Ability 效果配置 — 继承蚱蜢 Props 并追加伤害相关字段。
    /// </summary>
    public class CompProperties_BounceSlash : Grasshopper.CompProperties_GrasshopperJump
    {
        /// <summary>沿途碰撞伤害类型；由 XML 注入，运行期缺失时再回退为 Cut（切割）。</summary>
        public DamageDef damageDef;

        /// <summary>沿途碰撞伤害值。</summary>
        public int damageAmount = 15;

        /// <summary>护甲穿透。</summary>
        public float armorPenetration = 0f;

        /// <summary>构造并绑定对应的 effect comp 类型。</summary>
        public CompProperties_BounceSlash()
        {
            compClass = typeof(CompAbilityEffect_BounceSlash);
        }
    }

    /// <summary>
    /// 弹射砍击 Ability 效果组件 — 继承蚱蜢链式跳跃，附加沿途碰撞伤害。
    ///
    /// 核心差异：
    ///   - 使用 PawnFlyer_BounceSlash（沿途碰撞）替代蚱蜢飞行器
    ///   - 覆写 OnBeforeSegmentJump 每段起跳前注入伤害参数并清空伤害集
    ///   - 其余逻辑（路径处理、链式回调、Trion 扣费）完全复用基类
    /// </summary>
    public class CompAbilityEffect_BounceSlash : Grasshopper.CompAbilityEffect_GrasshopperJump
    {
        /// <summary>当前效果组件使用的强类型配置。</summary>
        private new CompProperties_BounceSlash Props
        {
            get { return (CompProperties_BounceSlash)props; }
        }

        /// <summary>
        /// 每段起跳前注入伤害参数并清空伤害集（基类钩子）。
        /// 确保同一目标在新段可以被再次伤害。
        /// </summary>
        protected override void OnBeforeSegmentJump(Pawn pawn, PawnFlyer flyer)
        {
            base.OnBeforeSegmentJump(pawn, flyer);

            PawnFlyer_BounceSlash bounceFlyer = flyer as PawnFlyer_BounceSlash;
            if (bounceFlyer != null)
            {
                bounceFlyer.damageDef = Props.damageDef;
                bounceFlyer.damageAmount = Props.damageAmount;
                bounceFlyer.armorPenetration = Props.armorPenetration;
                bounceFlyer.sourceLabel = parent?.def?.label;
                bounceFlyer.ResetHurtSet();
            }
        }
    }
}
