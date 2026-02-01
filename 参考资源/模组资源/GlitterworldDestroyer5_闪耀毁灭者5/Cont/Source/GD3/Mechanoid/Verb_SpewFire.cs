using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GD3
{
	public class Verb_SpewFire : Verb
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
			float num = Mathf.Atan2((float)(-(float)(this.currentTarget.Cell.z - position.z)), (float)(this.currentTarget.Cell.x - position.x)) * 57.29578f;
			FloatRange value = new FloatRange(num - 13f, num + 13f);

			/*List<IntVec3> list = new List<IntVec3>();
			Vector3 direction = new Vector3(this.currentTarget.Cell.x - position.x, 0, this.currentTarget.Cell.z - position.z).normalized;
			float num = 0;

			Vector3 zero = position.ToVector3();
			while (num < verbProps.range)
            {
				num++;
				list.Add((zero + num * direction).ToIntVec3());
            }
			num = 0;

			zero = position.ToVector3() + new Vector3(-(this.currentTarget.Cell.z - position.z), 0, this.currentTarget.Cell.x - position.x).normalized;
			while (num < verbProps.range)
			{
				num++;
				list.Add((zero + num * direction).ToIntVec3());
			}
			num = 0;

			zero = position.ToVector3() + new Vector3(this.currentTarget.Cell.z - position.z, 0, -(this.currentTarget.Cell.x - position.x)).normalized;
			while (num < verbProps.range)
			{
				num++;
				list.Add((zero + num * direction).ToIntVec3());
			}
			list.RemoveAll(c => !c.IsValid || !c.InBounds(caster.Map) || c == position);
			
			foreach (IntVec3 c in list)
            {
				GenExplosion.DoExplosion(c, this.caster.MapHeld, 0.9f, GDDefOf.BombCharge, null, 30, -1f, null, null, null, null, null, 0f, 1, null, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
			}*/
			GenExplosion.DoExplosion(position, this.caster.MapHeld, this.verbProps.range, DamageDefOf.Flame, null, 30, -1f, null, null, null, null, ThingDefOf.Filth_FlammableBile, 1f, 1, null, null, 255, false, null, 0f, 1, 1f, false, null, null, new FloatRange?(value), false, 0.6f, 0f, false, null, 1f);
			base.AddEffecterToMaintain(EffecterDefOf.Fire_SpewShort.Spawn(this.caster.Position, this.currentTarget.Cell, this.caster.Map, 1f), this.caster.Position, this.currentTarget.Cell, 14, this.caster.Map);
			this.lastShotTick = Find.TickManager.TicksGame;
			return true;
		}

		// Token: 0x06002CA7 RID: 11431 RVA: 0x0011BC8C File Offset: 0x00119E8C
		public override bool Available()
		{
			if (!base.Available())
			{
				return false;
			}
			if (this.CasterIsPawn)
			{
				Pawn casterPawn = this.CasterPawn;
				if (casterPawn.Faction != Faction.OfPlayer && casterPawn.mindState.MeleeThreatStillThreat && casterPawn.mindState.meleeThreat.Position.AdjacentTo8WayOrInside(casterPawn.Position))
				{
					return false;
				}
			}
			return true;
		}
	}
}
