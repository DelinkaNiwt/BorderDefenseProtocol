using System.Reflection;
using Verse;

namespace BDP.Content.Combos
{
    /// <summary>战斧只组合美特拉的伤害、护穿与半径；其余弹道由原版定义继承毒蛇。</summary>
    public static class TomahawkProjectileDefinitionResolver
    {
        /// <summary>原版基础伤害为私有字段；明确读取这一个既定字段，不建立通用跟随框架。</summary>
        private static readonly FieldInfo Damage = typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>原版基础护穿为私有字段，保留来源原值及默认值语义。</summary>
        private static readonly FieldInfo Armor = typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>在正式内容校验之前更新已加载的战斧定义。</summary>
        public static void Resolve()
        {
            Apply(DefDatabase<ThingDef>.GetNamed("BDP_Projectile_Variable_Explosive"),
                DefDatabase<ThingDef>.GetNamed("BDP_Projectile_Explosive"));
        }

        /// <summary>应用这项组合的三项来源数据；公开入口也供独立候选定义验收使用。</summary>
        public static void Apply(ThingDef tomahawk, ThingDef meteora)
        {
            ProjectileProperties target = tomahawk.projectile;
            ProjectileProperties source = meteora.projectile;
            Damage.SetValue(target, Damage.GetValue(source));
            Armor.SetValue(target, Armor.GetValue(source));
            target.explosionRadius = source.explosionRadius;
        }
    }
}
