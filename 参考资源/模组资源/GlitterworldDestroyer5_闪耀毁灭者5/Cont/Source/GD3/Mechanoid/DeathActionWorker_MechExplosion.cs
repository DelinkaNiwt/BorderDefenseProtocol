using System;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace GD3
{
	public class DeathActionWorker_MechExplosion : DeathActionWorker
	{
		public override RulePackDef DeathRules
		{
			get
			{
				return RulePackDefOf.Transition_DiedExplosive;
			}
		}

		public override bool DangerousInMelee
		{
			get
			{
				return true;
			}
		}

		public override void PawnDied(Corpse corpse, Lord prevLord)
		{
			float radius;
			radius = 5.4f;
			GenExplosion.DoExplosion(corpse.Position, corpse.Map, radius, GDDefOf.BombFrostBite, corpse.InnerPawn, -1, -1f, null, null, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f);
		}
	}
}