using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
	public class HediffComp_MechJump : HediffComp
	{
		public override void CompExposeData()
		{
			base.CompExposeData();
			Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
		}

		// Token: 0x06000226 RID: 550 RVA: 0x000128B8 File Offset: 0x00010AB8
		public override void CompPostMake()
		{
			base.CompPostMake();
		}

		// Token: 0x06000227 RID: 551 RVA: 0x00013170 File Offset: 0x00011370
		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			Pawn pawn = base.Pawn;
			if (!pawn.Faction.IsPlayer)
            {
				if (this.ticks >= 700 && this.JumpToTarget())
				{
					this.ticks = 0;
				}
				this.ticks++;
			}
		}
		// Token: 0x06000229 RID: 553 RVA: 0x000133D4 File Offset: 0x000115D4
		protected bool JumpToTarget()
		{
			if (!ModsConfig.RoyaltyActive)
			{
				return true;
			}
			if (base.Pawn.mindState.enemyTarget != null && base.Pawn.mindState.enemyTarget.Position != LocalTargetInfo.Invalid && base.Pawn.mindState.enemyTarget.Position.DistanceTo(base.Pawn.Position) >= 2f && base.Pawn.mindState.enemyTarget.Position.DistanceTo(base.Pawn.Position) <= 10f)
			{
				Map map = base.Pawn.Map;
				try
				{
					PawnFlyer pawnFlyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer, base.Pawn, base.Pawn.mindState.enemyTarget.Position, GDDefOf.JumpFlightEffect, GDDefOf.JumpPackLand, false);
					if (pawnFlyer != null)
					{
						GenSpawn.Spawn(pawnFlyer, base.Pawn.mindState.enemyTarget.Position, map, WipeMode.Vanish);
						return true;
					}
				}
				catch (Exception ex)
				{
					Log.Message(ex.ToString());
				}
				return false;
			}
			return false;
		}

		// Token: 0x04000115 RID: 277
		private int ticks = 1250;

	}
}