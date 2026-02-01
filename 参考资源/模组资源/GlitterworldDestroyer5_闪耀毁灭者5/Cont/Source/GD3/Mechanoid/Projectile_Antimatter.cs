using System;
using Verse;
using RimWorld;

namespace GD3
{
	// Token: 0x02001103 RID: 4355
	public class Projectile_Antimatter : Projectile
	{
		// Token: 0x1700122D RID: 4653
		// (get) Token: 0x06006967 RID: 26983 RVA: 0x00012A6D File Offset: 0x00010C6D
		public override bool AnimalsFleeImpact
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06006968 RID: 26984 RVA: 0x0023E174 File Offset: 0x0023C374
		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			Map map = base.Map;
			base.Impact(hitThing);
			IntVec3 position = base.Position;
			Map map2 = map;
			float explosionRadius = this.def.projectile.explosionRadius;
			DamageDef bomb = GDDefOf.BombCharge;
			Thing launcher = this.launcher;
			int damageAmount = base.DamageAmount;
			float armorPenetration = base.ArmorPenetration;
			SoundDef explosionSound = null;
			ThingDef equipmentDef = this.equipmentDef;
			ThingDef def = this.def;
			ThingDef filth_Fuel = ThingDefOf.Filth_Fuel;
			GenExplosion.DoExplosion(position, map2, explosionRadius, bomb, launcher, damageAmount, armorPenetration, explosionSound, equipmentDef, def, this.intendedTarget.Thing, filth_Fuel, 0.2f, 1, null, null, 255, false, null, 0f, 1, 0.4f, false, null, null, null, true, 1f, 0f, true, null, 1f);
			CellRect cellRect = CellRect.CenteredOn(base.Position, 5);
			cellRect.ClipInsideMap(map);
			for (int i = 0; i < 7; i++)
			{
				IntVec3 randomCell = cellRect.RandomCell;
				this.DoFireExplosion(randomCell, map, 3.9f);
			}
		}

		// Token: 0x06006969 RID: 26985 RVA: 0x0023E23C File Offset: 0x0023C43C
		protected void DoFireExplosion(IntVec3 pos, Map map, float radius)
		{
			GenExplosion.DoExplosion(pos, map, radius, GDDefOf.BombCharge, this.launcher, this.DamageAmount, this.ArmorPenetration, null, this.equipmentDef, this.def, this.intendedTarget.Thing, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
		}

		// Token: 0x04003B41 RID: 15169
		private const int ExtraExplosionCount = 5;

		// Token: 0x04003B42 RID: 15170
		private const int ExtraExplosionRadius = 9;
	}
}
