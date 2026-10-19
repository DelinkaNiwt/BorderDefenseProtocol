using RimWorld;
using Verse;

namespace BDP.Content.Information.Features
{
    /// <summary>BDP Ability 效果组件可选择实现的只读近战信息来源契约。</summary>
    internal interface IAbilityMeleeAttackInformation
    {
        /// <summary>组件每次攻击造成的主要伤害。</summary>
        int DamageAmount { get; }

        /// <summary>组件攻击使用的伤害类型。</summary>
        DamageDef DamageDef { get; }

        /// <summary>组件攻击使用的护甲穿透。</summary>
        float ArmorPenetration { get; }
    }
}
