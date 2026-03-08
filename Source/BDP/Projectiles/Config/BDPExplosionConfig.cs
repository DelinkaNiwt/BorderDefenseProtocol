using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 爆炸模块配置——挂在投射物ThingDef的modExtensions上。
    /// 替代原版Projectile_Explosive基类，由ExplosionModule在Impact时触发爆炸。
    /// </summary>
    public class BDPExplosionConfig : DefModExtension
    {
        /// <summary>爆炸半径（格）。</summary>
        public float explosionRadius = 1.0f;

        /// <summary>
        /// 爆炸伤害类型（可选）。
        /// null时使用projectile.damageDef（ThingDef.projectile节点中的damageDef）。
        /// </summary>
        public DamageDef explosionDamageDef;
    }
}
