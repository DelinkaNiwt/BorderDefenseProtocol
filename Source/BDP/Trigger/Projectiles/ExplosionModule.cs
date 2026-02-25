using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 爆炸模块——在Impact时触发范围爆炸。
    /// Priority=50（伤害效果，中等优先级）。
    ///
    /// 管线接口：IBDPImpactHandler（命中时执行爆炸效果）。
    ///
    /// 爆炸参数从BDPExplosionConfig读取：
    ///   radius = config.explosionRadius
    ///   damageDef = config.explosionDamageDef ?? projectile.damageDef
    ///   damageAmount = projectile.DamageAmount
    /// </summary>
    public class ExplosionModule : IBDPProjectileModule, IBDPImpactHandler
    {
        /// <summary>爆炸配置引用。</summary>
        private readonly BDPExplosionConfig config;

        public int Priority => 50;

        /// <summary>无参构造——Scribe反序列化需要。</summary>
        public ExplosionModule() { config = null; }

        public ExplosionModule(BDPExplosionConfig config)
        {
            this.config = config;
        }

        public void OnSpawn(Bullet_BDP host) { }

        /// <summary>
        /// IBDPImpactHandler实现——触发爆炸，替代默认的单体伤害。
        /// 设置ctx.Handled=true表示已处理Impact。
        /// </summary>
        public void HandleImpact(Bullet_BDP host, ref ImpactContext ctx)
        {
            var cfg = config ?? host.def.GetModExtension<BDPExplosionConfig>();
            if (cfg == null) return;

            Map map = host.Map;
            if (map == null) return;

            // 爆炸参数
            float radius = cfg.explosionRadius;
            DamageDef damageDef = cfg.explosionDamageDef
                ?? host.def.projectile?.damageDef;

            if (damageDef == null) return;

            // 使用Projectile公开属性获取伤害和穿甲值
            int damageAmount = host.DamageAmount;
            float armorPen = host.ArmorPenetration;

            GenExplosion.DoExplosion(
                center: host.Position,
                map: map,
                radius: radius,
                damType: damageDef,
                instigator: host.Launcher,
                damAmount: damageAmount,
                armorPenetration: armorPen,
                weapon: host.EquipmentDef,
                projectile: host.def);

            host.Destroy();
            ctx.Handled = true;
        }

        /// <summary>配置来自def，无需额外序列化。</summary>
        public void ExposeData() { }
    }
}
