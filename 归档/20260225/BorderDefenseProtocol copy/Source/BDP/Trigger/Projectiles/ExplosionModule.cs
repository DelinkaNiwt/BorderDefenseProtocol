using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 爆炸模块——替代Projectile_ExplosiveBDP，在Impact时触发范围爆炸。
    /// Priority=50（伤害效果，中等优先级）。
    ///
    /// 爆炸参数从BDPExplosionConfig读取：
    ///   radius = config.explosionRadius
    ///   damageDef = config.explosionDamageDef ?? projectile.damageDef
    ///   damageAmount = projectile.DamageAmount
    /// </summary>
    public class ExplosionModule : IBDPProjectileModule
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
        public void OnTick(Bullet_BDP host) { }
        public bool OnPreImpact(Bullet_BDP host) => false;

        /// <summary>
        /// Impact时触发爆炸，替代默认的单体伤害。
        /// 返回true=已处理（宿主跳过base.Impact）。
        /// </summary>
        public bool OnImpact(Bullet_BDP host, Thing hitThing)
        {
            var cfg = config ?? host.def.GetModExtension<BDPExplosionConfig>();
            if (cfg == null) return false;

            Map map = host.Map;
            if (map == null) return false;

            // 爆炸参数
            float radius = cfg.explosionRadius;
            DamageDef damageDef = cfg.explosionDamageDef
                ?? host.def.projectile?.damageDef;

            if (damageDef == null) return false;

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
            return true;
        }

        public void OnPostImpact(Bullet_BDP host, Thing hitThing) { }

        /// <summary>配置来自def，无需额外序列化。</summary>
        public void ExposeData() { }
    }
}
