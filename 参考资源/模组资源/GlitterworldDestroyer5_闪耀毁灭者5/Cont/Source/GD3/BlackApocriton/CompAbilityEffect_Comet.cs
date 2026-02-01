using System;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using UnityEngine;

namespace GD3
{
	public class CompAbilityEffect_Comet : CompAbilityEffect
	{
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			Map map = parent.pawn.MapHeld;
			float angle = Rand.Range(0, 360);
			List<IntVec3> vecs = GDUtility.GetStraightLineRange(target, map, 3, angle, 50, false, false);
			if (!vecs.Any())
            {
				return;
            }
			bool flag = false;
			IntVec3 refer = default(IntVec3);

			List<IntVec3> edgeCells = map.AllCells.Where(c => c.x == 0 || c.x == map.Size.x - 1 || c.z == 0 || c.z == map.Size.z - 1).ToList();
			for (int j = 0; j < 100; j++)
            {
				IntVec3 tmp = edgeCells.RandomElement();
				float result = (target.CenterVector3 - tmp.ToVector3Shifted()).Yto0().AngleFlat() - angle;
				flag = result < 30 && result > -30;
				if (flag)
                {
					refer = tmp;
					break;
                }
			}

			if (flag)
			{
				int count = (int)(vecs.Count * 0.04f);
				vecs = vecs.TakeRandom(count).ToList();
				vecs.SortBy(c => refer.DistanceTo(c));
				for (int i = 0; i < vecs.Count; i++)
				{
					Skyfaller comet = (Skyfaller)ThingMaker.MakeThing(GDDefOf.BlackStrike);
					GenSpawn.Spawn(comet, vecs[i], map);
					comet.ticksToImpact = 300 + 4 * i;
				}
				if (target.Cell.ShouldSpawnMotesAt(map))
				{
					FleckCreationData dataStatic = FleckMaker.GetDataStatic(target.CenterVector3, map, GDDefOf.GD_CometStrikeWarning);
					dataStatic.rotation = angle + 180;
					dataStatic.exactScale = new Vector3(1.0f, 1f, 3.2f) * 9f;
					map.flecks.CreateFleck(dataStatic);
				}
			}
			else Log.Warning("Black Apocriton comet cannot find valid edge cell.");
		}
		public override void Apply(GlobalTargetInfo target)
		{
			this.Apply(null, null);
		}

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
			BlackApocriton blackApocriton = parent.pawn as BlackApocriton;
			if (blackApocriton != null && !blackApocriton.CanUsePsychicAttack)
            {
				return false;
            }
			return true;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
		{
			return true;
		}

		public override bool CanApplyOn(GlobalTargetInfo target)
		{
			return this.CanApplyOn(null, null);
		}

		public override bool GizmoDisabled(out string reason)
		{
			reason = null;
			return false;
		}
	}
}