using Verse;
using RimWorld;

namespace GD3
{
	// Token: 0x020003FE RID: 1022
	public class Projectile_HellsphereCannon : Projectile
	{
		// Token: 0x06001D20 RID: 7456 RVA: 0x000B0D90 File Offset: 0x000AEF90
		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			Map map = base.Map;
			base.Impact(hitThing, blockedByShield);
			GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius, this.def.projectile.damageDef, this.launcher, this.DamageAmount, this.ArmorPenetration, null, this.equipmentDef, this.def, this.intendedTarget.Thing, null, 0f, 1, null, null, 255, false, null, 0f, 1, this.def.projectile.explosionChanceToStartFire, false, null, null, null, true, this.def.projectile.damageDef.expolosionPropagationSpeed, 0f, true, null, 1f);
		}

		// Token: 0x0400149D RID: 5277
		private const float ExtraExplosionRadius = 9.9f;
	}
}