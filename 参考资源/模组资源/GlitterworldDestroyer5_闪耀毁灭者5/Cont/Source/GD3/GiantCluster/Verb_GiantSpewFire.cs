using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace GD3
{
	public class Verb_GiantSpewFire : Verb
	{
		protected override bool TryCastShot()
		{
			if (this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map)
			{
				return false;
			}
			if (base.EquipmentSource != null)
			{
				CompChangeableProjectile comp = base.EquipmentSource.GetComp<CompChangeableProjectile>();
				if (comp != null)
				{
					comp.Notify_ProjectileLaunched();
				}
				CompApparelReloadable comp2 = base.EquipmentSource.GetComp<CompApparelReloadable>();
				if (comp2 != null)
				{
					comp2.UsedOnce();
				}
			}
			IntVec3 position = this.caster.Position;
			float num = Mathf.Atan2((float)(-(float)(this.currentTarget.Cell.z - position.z)), (float)(this.currentTarget.Cell.x - position.x)) * 81.29578f;
			FloatRange value = new FloatRange(num - 13f, num + 13f);
			GenExplosion.DoExplosion(position, this.caster.MapHeld, this.verbProps.range, DamageDefOf.Flame, null, 100, -1f, null, null, null, null, ThingDefOf.Filth_FlammableBile, 1f, 1, null, null, 255, false, null, 0f, 1, 1f, false, null, null, new FloatRange?(value), false, 0.6f, 0f, false, null, 1f);
			base.AddEffecterToMaintain(EffecterDefOf.Fire_SpewShort.Spawn(this.caster.Position, this.currentTarget.Cell, this.caster.Map, 1f), this.caster.Position, this.currentTarget.Cell, 14, this.caster.Map);
			this.lastShotTick = Find.TickManager.TicksGame;
			return true;
		}

		public override bool Available()
		{
			if (!base.Available())
			{
				return false;
			}
			return true;
		}
	}
}
