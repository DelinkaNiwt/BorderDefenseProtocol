using System;
using RimWorld;
using Verse;

namespace GD3
{
	public class Projectile_AlphaBombardment : Projectile
	{
		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			Map map = base.Map;
			base.Impact(hitThing, blockedByShield);
			AlphaBombardment bombardment = (AlphaBombardment)GenSpawn.Spawn(GDDefOf.GD_AlphaBombardment, base.Position, map, WipeMode.Vanish);
			bombardment.instigator = this.launcher as Pawn ?? null;
		}
	}
}
