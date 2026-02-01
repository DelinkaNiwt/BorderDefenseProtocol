using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using Verse.AI;
using UnityEngine;

namespace GD3
{
	public class CompProperties_ArchoDrone : CompProperties
	{
		public float explosionRadius = 1.9f;

		public DamageDef explosionDamageType;

		public int explosionDamageAmount = 50;

		public float detectRadius = 7.9f;

		public int detectTick = 80;

		public CompProperties_ArchoDrone()
		{
			compClass = typeof(CompArchoDrone);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			if (explosionDamageType == null)
			{
				explosionDamageType = DamageDefOf.Bomb;
			}
		}
	}

	public class CompArchoDrone : ThingComp
	{
		private bool wickStarted;

		private int wickTicks;

		public bool alert = false;

		public bool detected = false;

		public int detectTick = 0;

		public int randomSkipInterval = 1800;

		[Unsaved(false)]
		private Sustainer wickSoundSustainer;

		[Unsaved(false)]
		private OverlayHandle? overlayBurningWick;

		private CompProperties_ArchoDrone Props => (CompProperties_ArchoDrone)props;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref wickStarted, "wickStarted", defaultValue: false);
			Scribe_Values.Look(ref alert, "alert", defaultValue: false);
			Scribe_Values.Look(ref detected, "detected", defaultValue: false);
			Scribe_Values.Look(ref wickTicks, "wickTicks", 0);
			Scribe_Values.Look(ref detectTick, "detectTick", 0);
			Scribe_Values.Look(ref randomSkipInterval, "randomSkipInterval", 1800);
		}

		public override void CompTickInterval(int delta)
		{
			if (!alert)
            {
				return;
            }

			if (!wickStarted && parent.IsHashIntervalTick(30, delta) && parent is Pawn pawn && pawn.Spawned && !pawn.Downed && PawnUtility.EnemiesAreNearby(pawn, 9, passDoors: true, 1.5f))
			{
				StartWick();
			}
		}

		public override void CompTick()
		{
			if (!alert && parent is Pawn pawn && pawn.Spawned)
            {
				int interval = detected ? 5 : 25;
				if (parent.IsHashIntervalTick(interval))
                {
					if (!detected && EnemiesAreNearby(pawn, 9, passDoors: true, Props.detectRadius))
                    {
						detected = true;
						pawn.Rotation = pawn.GetRot(pawn.Map.listerThings.AllThings.FindAll(t => t.HostileTo(pawn)).MinBy(t => t.Position.DistanceTo(pawn.Position)).DrawPos);
						SoundDefOf.TurretAcquireTarget.PlayOneShot(parent);
						if (pawn.Fogged())
                        {
							pawn.Map.fogGrid.FloodUnfogAdjacent(pawn, false);
						}
					}
					else if (detected && !EnemiesAreNearby(pawn, 9, passDoors: true, Props.detectRadius))
                    {
						detected = false;
						detectTick = 0;
						pawn.Rotation = Rot4.South;
						SoundInfo info = parent;
						info.pitchFactor = 0.4f;
						SoundDefOf.TurretAcquireTarget.PlayOneShot(info);
					}
                }
				if (detected)
                {
					detectTick++;
					if (detectTick > Props.detectTick)
                    {
						SoundDefOf.MechanoidsWakeUp.PlayOneShot(parent);
						alert = true;
						pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                }

				if (!detected && pawn.Spawned && !pawn.Fogged() && pawn.IsHashIntervalTick(randomSkipInterval))
                {
					randomSkipInterval = Rand.Range(1200, 2400);
					IEnumerable<Room> rooms = pawn.Map.regionGrid.AllRooms.Where(r => !r.Fogged && !r.IsDoorway);
					if (rooms.Any())
                    {
						if (rooms.RandomElement().Cells.TryRandomElement(c => c.StandableBy(pawn.Map, pawn), out IntVec3 cell))
                        {
							SkipUtility.SkipTo(pawn, cell, pawn.Map);
                        }
                    }
				}
            }

			if (wickStarted)
			{
				wickSoundSustainer.Maintain();
				wickTicks--;
				if (wickTicks <= 0)
				{
					Detonate();
				}
			}
		}

        public override void PostDraw()
        {
			if (!alert && detected)
            {
				Vector3 drawPos = parent.DrawPos;
				drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
				drawPos += new Vector3(0, 0, 0.85f);
				Matrix4x4 matrix = default(Matrix4x4);
				matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(1.2f, 1.0f, 1.2f));
				Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("UI/Symbols/WarningIcon", ShaderDatabase.MoteGlow, new Color(1, 1, 1, 0.5f)), 0);
			}
		}

        private void StartWick()
		{
			if (!wickStarted)
			{
				wickStarted = true;
				overlayBurningWick = parent.Map.overlayDrawer.Enable(parent, OverlayTypes.BurningWick);
				wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.PerTick));
				wickTicks = 120;
			}
		}

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
			Pawn pawn = parent as Pawn;
			if (!alert)
			{
				SoundDefOf.MechanoidsWakeUp.PlayOneShot(parent);
				alert = true;
				pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		}

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
		{
			if (dinfo.HasValue)
			{
				Detonate(prevMap);
			}
		}

		private void Detonate(Map map = null)
		{
			IntVec3 position = parent.Position;
			if (map == null)
			{
				map = parent.Map;
			}
			if (!parent.Destroyed)
			{
				parent.Destroy();
			}
			GenExplosion.DoExplosion(position, map, Props.explosionRadius, Props.explosionDamageType, parent, Props.explosionDamageAmount);
			if (base.ParentHolder is Corpse corpse)
			{
				corpse.Destroy();
			}
		}

		private bool EnemiesAreNearby(Pawn pawn, int regionsToScan = 9, bool passDoors = false, float maxDistance = -1f, int maxCount = 1, bool invisible = false)
		{
			TraverseParms tp = passDoors ? TraverseParms.For(TraverseMode.PassDoors) : TraverseParms.For(pawn);
			int count = 0;
			RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region to) => to.Allows(tp, isDestination: false), delegate (Region r)
			{
				List<Thing> list = r.ListerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
				for (int i = 0; i < list.Count; i++)
				{
					Thing thing = list[i];
					if ((maxDistance <= 0f || thing.Position.InHorDistOf(pawn.Position, maxDistance)) && thing.HostileTo(pawn))
					{
						if (!invisible)
						{
							Pawn pawn2 = thing as Pawn;
							if (pawn2 != null && (pawn2.IsPsychologicallyInvisible() || !pawn2.pather.Moving))
							{
								continue;
							}
						}

						count++;
					}
				}

				return count >= maxCount;
			}, regionsToScan);
			return count >= maxCount;
		}
	}

}
