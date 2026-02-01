using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
	public class Skyfaller_LandingMech : Skyfaller
	{
		public Pawn Pawn => (Pawn)innerContainer[0];

		public bool selected;

		public bool drafted;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
			if (Pawn != null)
            {
				Pawn.Rotation = Rot4.East;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			GetDrawPositionAndRotation(ref drawLoc, out var _);
			Pawn?.DrawNowAt(drawLoc, flip);
			DrawDropSpotShadow();
		}

		protected override void SpawnThings()
		{
			Pawn pawn = Pawn;
			if (pawn != null)
			{
				GenSpawn.Spawn(pawn, base.Position, base.Map);
				if (pawn is Annihilator annihilator)
                {
					annihilator.animation = GDDefOf.Annihilator_Ambient;
					GDDefOf.Annihilator_DoJump.PlayOneShot(annihilator);
					GDDefOf.GD_ImpactDustCloud.Spawn(annihilator, annihilator, 1.2f).Cleanup();
					Find.CameraDriver.shaker.DoShake(0.3f, 40);
					if (annihilator.Position.ShouldSpawnMotesAt(Map) && Map.thingGrid.ThingAt<Building_CentralCharger>(pawn.Position) == null)
					{
						FleckCreationData dataStatic = FleckMaker.GetDataStatic(annihilator.DrawPos, Map, GDDefOf.GD_GroundCrack);
						dataStatic.scale = 10f;
						dataStatic.rotation = Rand.Range(0, 360);
						Map.flecks.CreateFleck(dataStatic);
					}
				}
				List<Thing> things = Map.listerThings.AllThings.Where(p => p.Position.IsInside(pawn) && p != pawn).ToList();
				if (things.Any())
                {
					foreach (Thing victim in things)
                    {
						float amount = (victim is Building && victim.def.useHitPoints) ? Math.Max(victim.MaxHitPoints * 0.75f, 2500) : 300;
						victim.TakeDamage(new DamageInfo(DamageDefOf.Crush, amount, 30000f, -1, pawn));
                    }
                }
				if (selected)
                {
					Find.Selector.Select(pawn, false);
                }
				if (drafted && pawn?.drafter != null && Map.thingGrid.ThingAt<Building_CentralCharger>(pawn.Position) == null)
				{
					pawn.drafter.Drafted = true;
				}
			}
		}

		protected override void DrawDropSpotShadow()
		{
			Material shadowMaterial = base.ShadowMaterial;
			if (shadowMaterial != null)
			{
				Vector3 drawPos = DrawPos;
				drawPos.y = AltitudeLayer.Shadows.AltitudeFor();
				drawPos.z = base.Position.ToVector3Shifted().z;
				Skyfaller.DrawDropSpotShadow(drawPos, base.Rotation, shadowMaterial, def.skyfaller.shadowSize, ticksToImpact);
			}
		}

        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Values.Look(ref selected, "selected");
			Scribe_Values.Look(ref drafted, "drafted");
        }
    }
}
