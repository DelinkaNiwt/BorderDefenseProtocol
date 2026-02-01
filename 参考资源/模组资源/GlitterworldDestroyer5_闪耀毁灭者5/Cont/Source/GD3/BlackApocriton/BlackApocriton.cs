using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class BlackApocriton : Pawn, BossMusic
    {
		public static float AntiMeleeDistance => 7.9f;

		private static List<Hediff> tmpHediffs = new List<Hediff>();

		public int preventCaneDrawTick;

		public int caneAttackTick = -1;

		public int thunderArrowsTick = -1;

		public int lastSwapVictimTick = -1;

		public int cometRainTick = -1;

		public LocalTargetInfo caneTargetInfo;

		public static int SwapVictimCooldown = 1200;

		public static int WingsFixCooldown = 960;

		public bool attackWings;

		public bool wingsBroken;

		public int wingsFixTick = -1;

		public bool beaten;

		private bool devAlwaysSwap;

		private BodyPartRecord wingsInt;

		public BodyPartRecord Wings => wingsInt == null ? wingsInt = health.hediffSet.GetBodyPartRecord(GDDefOf.MechanicalWing_Apocriton) : wingsInt;

		private FinalBattleDummy pointerInt;

		public FinalBattleDummy Pointer => pointerInt == null ? pointerInt = (FinalBattleDummy)MapHeld?.listerThings.AllThings.Find(t => t is FinalBattleDummy) : pointerInt;

		private CompBlackApocriton comp;

		public CompBlackApocriton Comp => comp == null ? comp = this.TryGetComp<CompBlackApocriton>() : comp;
		
		public bool CanUsePsychicAttack => !health.hediffSet.PartIsMissing(Wings) && !beaten;

		public static Dictionary<IntRange, string> Strings
		{
			get
			{
				Dictionary<IntRange, string> strings = new Dictionary<IntRange, string>();
				strings.Add(new IntRange(300, 600), "GD.ApocritonEnd.0");
				strings.Add(new IntRange(600, 1100), "GD.ApocritonEnd.1");
				strings.Add(new IntRange(1100, 1600), "GD.ApocritonEnd.2");
				strings.Add(new IntRange(1600, 2200), "GD.ApocritonEnd.3");
				strings.Add(new IntRange(2200, 2800), "GD.ApocritonEnd.4");
				return strings;
			}
		}

		public SongDef Music => GDDefOf.MechCommander;

		public bool IsPlaying => GenHostility.IsActiveThreatToPlayer(this) && (Pointer == null || Pointer.ticker > 1000) && !beaten;

		protected override void Tick()
        {
            base.Tick();
			if (Comp.inMission)
            {
				return;
            }
			if (preventCaneDrawTick > 0)
            {
				preventCaneDrawTick--;
				Drawer.renderer.SetAllGraphicsDirty();
            }
			if (cometRainTick > 0)
            {
				cometRainTick--;
				if (cometRainTick < 300 && cometRainTick % 15 == 0)
                {
					ApplyCometRain();
                }
            }
			if (caneAttackTick == Find.TickManager.TicksGame)
            {
				ApplyPocketThunder();
            }
			if (thunderArrowsTick == Find.TickManager.TicksGame)
            {
				ApplyThunderArrows();
			}
			if (this.IsHashIntervalTick(40))
            {
				if (wingsBroken && wingsFixTick <= Find.TickManager.TicksGame)
                {
					WingsFixed();
                }
				if (CanUsePsychicAttack && this.PositionHeld.CloseToEdge(MapHeld, 7))
                {
					ReturnCenter();
                }
				HealSelf();
            }
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
			if (GDUtility.ExtraDrawer.preventInteraction)
            {
				absorbed = true;
            }
			if (attackWings && Rand.Chance(0.8f))
            {
				IEnumerable<BodyPartRecord> part = health.hediffSet.GetNotMissingParts(BodyPartHeight.Middle, BodyPartDepth.Undefined, GDDefOf.BlackApocritonWing);
				if (part.Any())
                {
					dinfo.SetHitPart(part.RandomElement());
                }
            }
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
			if (Comp.inMission || beaten)
			{
				return;
			}
			Pawn instigator = dinfo.Instigator as Pawn;
			if (instigator != null && instigator == mindState.meleeThreat)
            {
				AntiMelee(instigator);
            }
			bool wingsBroken = health.hediffSet.GetPartHealth(Wings) <= 0;
			if (wingsBroken != this.wingsBroken)
            {
				this.wingsBroken = wingsBroken;
				if (this.wingsBroken)
                {
					WingsBroke();
                }
			}
			if (dinfo.Def.harmsHealth && Pointer != null)
            {
				int num = (int)(totalDamageDealt * 0.5f);
				if (dinfo.HitPart == Wings) num = (int)(num * 0.4f);
				Pointer.trueProgress += num;
            }
			if (dinfo.Def.harmsHealth && Pointer != null && Pointer.ReadyToEnd && instigator != null && instigator != this)
            {
				End(dinfo.Angle);
			}
        }

        #region 彗星雨
		public void SetCometRain()
        {
			GDDefOf.ThrowGrenade.PlayOneShot(this);
			ThrowingCane effect = (ThrowingCane)ThingMaker.MakeThing(GDDefOf.GD_ThrowingCane);
			effect.fleck = GDDefOf.GDBlueSpark;
			effect.pitch = 1.6f;
			GenSpawn.Spawn(effect, PositionHeld, MapHeld);
			preventCaneDrawTick = 180;
			cometRainTick = 420;
			ResetAllAbilities(300);
		}

		public void ApplyCometRain()
        {
			float multiplier = 4f;
			float _angle = cometRainTick * multiplier;
			float angle = -_angle + 90;
			Vector3 vec = new Vector3(Mathf.Cos(angle * Mathf.PI / 180f), 0, Mathf.Sin(angle * Mathf.PI / 180f)) * 50 * cometRainTick / 300f;
			IntVec3 pos = PositionHeld + vec.ToIntVec3();
			if (pos.InBounds(MapHeld))
            {
				ThrowComet(pos, 3, (_angle - 90) % 360f, 40, true);
            }
		}
        #endregion

        #region 蜂翅损坏与修复
        public void WingsBroke()
        {
			wingsFixTick = Find.TickManager.TicksGame + WingsFixCooldown;
			GenExplosion.DoExplosion(PositionHeld, MapHeld, -1, GDDefOf.MechBandShockwave, this, -1, -1, GDDefOf.Explosion_MechBandShockwave, null, null, null, null, 0.2f, 1, null, null, 255, false, null, 0f, 1, 0.4f, false, null, null, null, true, 1f, 0f, true, null, 1f);
			GDDefOf.Pawn_Mech_Apocriton_Wounded.PlayOneShot(this);
			Messages.Message("GD.WingsBroke".Translate(WingsFixCooldown.TicksToSeconds()), this, MessageTypeDefOf.NeutralEvent);
			if (Pointer != null)
			{
				Pointer.trueProgress += 1000;
			}
		}

		public void WingsFixed()
        {
			FleckMaker.Static(PositionHeld, MapHeld, GDDefOf.ApocritonResurrectFlashGrowing, 3.0f);
			FleckMaker.Static(Position, Map, FleckDefOf.PsycastAreaEffect, 3.0f);
			SoundDefOf.MechSerumUsed.PlayOneShot(this);
			GDDefOf.Pawn_Mech_Apocriton_Call.PlayOneShot(this);
			tmpHediffs.Clear();
			tmpHediffs.AddRange(health.hediffSet.hediffs);
			for (int i = 0; i < tmpHediffs.Count; i++)
			{
				Hediff hediff = tmpHediffs[i];
				if (hediff != null && (hediff is Hediff_Injury || hediff is Hediff_MissingPart) && hediff.Part == Wings)
				{
					health.RemoveHediff(hediff);
					continue;
				}
			}
			tmpHediffs.Clear();
			wingsBroken = false;
		}
        #endregion

        #region 反近战折越
        public void AntiMelee(Pawn pawn)
		{
			if (!CanUsePsychicAttack)
			{
				return;
			}
			IntVec3 cell = CellFinderLoose.GetFleeDest(pawn, new List<Thing> { this }, AntiMeleeDistance);
			if (pawn != null)
			{
				pawn.stances.stunner.StunFor(30, this, false, false);
				SkipUtility.SkipTo(pawn, cell, Map);
			}
			Thing thing = ThingMaker.MakeThing(GDDefOf.GD_DummyMine, null);
			GenPlace.TryPlaceThing(thing, pawn.Position, Map, ThingPlaceMode.Near, null, null, default(Rot4));
		}
        #endregion

        #region 站位互换
        public void TrySwapVictim(Pawn sighter, Verb verb)
        {
			if (Find.TickManager.TicksGame - lastSwapVictimTick <= SwapVictimCooldown && !Rand.Chance(0.1f) && !devAlwaysSwap)
            {
				return;
            }
			if (!CanUsePsychicAttack)
            {
				return;
            }
			IEnumerable<Pawn> pawns = MapHeld.mapPawns.AllPawnsSpawned.Where(p => p.Faction == Faction.OfPlayer && p != sighter);
			if (!pawns.Any())
            {
				return;
            }
			lastSwapVictimTick = Find.TickManager.TicksGame;

			Pawn victim = pawns.RandomElement();
			IntVec3 pos = PositionHeld;
			SoundDefOf.Psycast_Skip_Entry.PlayOneShot(this);
			SoundDefOf.Psycast_Skip_Exit.PlayOneShot(victim);
			SkipUtility.SkipTo(this, victim.PositionHeld, MapHeld);
			SkipUtility.SkipTo(victim, pos, MapHeld);
			FleckMaker.ConnectingLine(DrawPos, victim.DrawPos, GDDefOf.GDSwapLine, MapHeld, 1f);

			if (verb != null && (verb.WarmingUp || verb.Bursting))
            {
				verb.Reset();
				verb.TryStartCastOn(victim);
            }
			ResetAllAbilities(60);
		}

		public void ReturnCenter()
		{
			IntVec3 pos = MapHeld.Center;
			SoundDefOf.Psycast_Skip_Entry.PlayOneShot(this);
			SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pos, MapHeld));
			SkipUtility.SkipTo(this, pos, MapHeld);
			FleckMaker.ConnectingLine(DrawPos, pos.ToVector3Shifted(), GDDefOf.GDSwapLine, MapHeld, 1f);

			ResetAllAbilities(60);
		}
		#endregion

		#region 球状闪电
		public void SetCaneAttack(LocalTargetInfo target)
        {
			Drawer.renderer.SetAnimation(GDDefOf.MechCane_Normal);
			caneAttackTick = Find.TickManager.TicksGame + GDDefOf.MechCane_Normal.durationTicks;
			caneTargetInfo = target;
			ResetAllAbilities(60);
		}

		public void ApplyPocketThunder()
		{
			Projectile proj = (Projectile)ThingMaker.MakeThing(GDDefOf.Bullet_PocketThunder);
			GenSpawn.Spawn(proj, PositionHeld, MapHeld);
			GDDefOf.ThumpCannon_Fire.PlayOneShot(this);
			FleckMaker.Static(Position, Map, FleckDefOf.PsycastAreaEffect, 1.0f);
			proj.Launch(this, caneTargetInfo, caneTargetInfo, ProjectileHitFlags.IntendedTarget, true, proj);
		}
		#endregion

		#region 闪电箭雨
		public void SetThunderArrows()
		{
			GDDefOf.ThrowGrenade.PlayOneShot(this);
			ThrowingCane effect = (ThrowingCane)ThingMaker.MakeThing(GDDefOf.GD_ThrowingCane);
			effect.fleck = GDDefOf.GDRedSpark;
			GenSpawn.Spawn(effect, PositionHeld, MapHeld);
			preventCaneDrawTick = 180;
			thunderArrowsTick = Find.TickManager.TicksGame + 120;
			ResetAllAbilities(300);
		}

		public void ApplyThunderArrows()
		{
			float maxRange = GDDefOf.BlackApocriton_Arrows.verbProperties.range + 10f;
			List<IntVec3> cells = GenRadial.RadialCellsAround(PositionHeld, maxRange, false).ToList();
			List<IntVec3> external = cells.FindAll(c => c.DistanceTo(PositionHeld) > maxRange * 2 / 3);
			List<IntVec3> middle = cells.FindAll(c => c.DistanceTo(PositionHeld) > maxRange * 1 / 3 && c.DistanceTo(PositionHeld) <= maxRange * 2 / 3);
			List<IntVec3> interna = cells.FindAll(c => c.DistanceTo(PositionHeld) <= maxRange * 1 / 3);

			IEnumerable<IntVec3> selected;
			int num = (int)(external.Count * 0.03f);
			selected = external.TakeRandom(num);
			foreach (IntVec3 cell in selected)
            {
				if (!cell.InBounds(MapHeld))
				{
					continue;
				}
				ThunderArrow thing = (ThunderArrow)ThingMaker.MakeThing(GDDefOf.GD_ThunderArrow);
				thing.launcher = this;
				thing.timeToLaunch = Find.TickManager.TicksGame + Rand.Range(420, 480);
				GenSpawn.Spawn(thing, cell, MapHeld);
            }

			num = (int)(middle.Count * 0.06f);
			selected = middle.TakeRandom(num);
			foreach (IntVec3 cell in selected)
			{
				if (!cell.InBounds(MapHeld))
				{
					continue;
				}
				ThunderArrow thing = (ThunderArrow)ThingMaker.MakeThing(GDDefOf.GD_ThunderArrow);
				thing.launcher = this;
				thing.timeToLaunch = Find.TickManager.TicksGame + Rand.Range(300, 360);
				GenSpawn.Spawn(thing, cell, MapHeld);
			}

			num = (int)(interna.Count * 0.105f);
			selected = interna.TakeRandom(num);
			foreach (IntVec3 cell in selected)
			{
				if (!cell.InBounds(MapHeld))
                {
					continue;
                }
				ThunderArrow thing = (ThunderArrow)ThingMaker.MakeThing(GDDefOf.GD_ThunderArrow);
				thing.launcher = this;
				thing.timeToLaunch = Find.TickManager.TicksGame + Rand.Range(180, 240);
				GenSpawn.Spawn(thing, cell, MapHeld);
			}
		}
        #endregion

		public void End(float angle)
        {
			beaten = true;

			preventCaneDrawTick = 999999;
			Pointer.endTicker = 0;
			Drawer.renderer.SetAnimation(GDDefOf.BlackApocriton_Shake);
			foreach (Pawn p in MapHeld.mapPawns.AllPawnsSpawned)
            {
				p.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            }
			if (angle == -1) angle = Rand.Range(0, 360);
			float _angle = -angle + 90;
			Vector3 target = new Vector3(Mathf.Cos(_angle * Mathf.PI / 180f), 0, Mathf.Sin(_angle * Mathf.PI / 180f)) * 4.9f + DrawPos;
			if (!target.InBounds(MapHeld)) target = GDUtility.RandomPointInCircle(4.9f) + DrawPos;
			IntVec3 cell = (target + GDUtility.RandomPointInCircle(2.9f, true)).ToIntVec3();
			GDDefOf.BlackCane_Head_Directional.Spawn(PositionHeld, cell, MapHeld).Cleanup();
			GDDefOf.BlackCane_Stick_Directional.Spawn(PositionHeld, cell, MapHeld).Cleanup();
			GDDefOf.MechCaneBroke.PlayOneShotOnCamera();

			Find.CameraDriver.shaker.DoShake(0.08f, 90);
			Find.MusicManagerPlay.ForceSilenceFor(180);
			GDUtility.ExtraDrawer.StartWhiteOverlay(60);
			GDUtility.ExtraDrawer.StartDialog(Strings, true);
		}

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
				yield return gizmo;
            }
			if (Comp.inMission)
			{
				yield break;
			}
			Command_Toggle attackWings = new Command_Toggle
			{
				defaultLabel = "GD.AttackWings".Translate(),
				defaultDesc = "GD.AttackWingsDesc".Translate(WingsFixCooldown.TicksToSeconds()),
				icon = ContentFinder<Texture2D>.Get("UI/Buttons/AttackWings", true),
				toggleAction = delegate ()
				{
					this.attackWings = !this.attackWings;
				},
				isActive = (() => this.attackWings)
			};
			yield return attackWings;
			if (DebugSettings.ShowDevGizmos)
            {
				Command_Toggle command_Toggle = new Command_Toggle
                {
					defaultLabel = "DEV: Always swap",
					toggleAction = delegate ()
					{
						devAlwaysSwap = !devAlwaysSwap;
					},
					isActive = (() => devAlwaysSwap)
				};
				Command_Action command_Action = new Command_Action
				{
					defaultLabel = "DEV: Break wings",
					action = delegate
                    {
						if (health.hediffSet.PartIsMissing(Wings))
                        {
							return;
                        }
						this.TakeDamage(new DamageInfo(DamageDefOf.Stab, 5000, 999, -1, null, Wings));
                    }
				};
				yield return command_Toggle;
				yield return command_Action;

				if (GDSettings.DeveloperMode)
				{
					Command_Action command_Action2 = new Command_Action
					{
						defaultLabel = "DEV: End fight",
						action = delegate
						{
							if (Pointer != null)
							{
								Pointer.trueProgress = Pointer.ProgressFull;
							}
						}
					};
					yield return command_Action2;
				}
			}
        }

		public void ResetAllAbilities(int tick)
        {
			foreach (Ability a in abilities.AllAbilitiesForReading)
            {
				if (a.CooldownTicksRemaining > tick)
                {
					continue;
                }
				a.StartCooldown(tick);
            }
        }

        private void HealSelf()
		{
			if (!CanUsePsychicAttack)
			{
				return;
			}
			tmpHediffs.Clear();
			tmpHediffs.AddRange(health.hediffSet.hediffs);
			bool flag = false;
			for (int i = 0; i < tmpHediffs.Count && i < 1; i++)
			{
				if (tmpHediffs.TryRandomElement(h => h.Part != Wings, out Hediff hediff) && (hediff is Hediff_Injury || hediff is Hediff_MissingPart))
				{
					health.RemoveHediff(hediff);
					flag = true;
					continue;
				}
			}
			tmpHediffs.Clear();
			if (!flag)
            {
				return;
            }
			if (Rand.Chance(0.2f)) FleckMaker.Static(PositionHeld, MapHeld, GDDefOf.ApocritonResurrectFlashGrowing, 2.0f);
		}

		public void ThrowComet(LocalTargetInfo target, int width, float angle, float range = 50, bool monodirectional = false)
        {
			Map map = MapHeld;
			List<IntVec3> vecs = GDUtility.GetStraightLineRange(target, map, width, angle, range, monodirectional);
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
					comet.ticksToImpact = 100 + 4 * i;
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

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
			if (!beaten && MapHeld != null && !DebugSettings.ShowDevGizmos && !Comp.inMission)
            {
				return;
            }
            base.Destroy(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Values.Look(ref preventCaneDrawTick, "preventCaneDrawTick");
			Scribe_Values.Look(ref caneAttackTick, "caneAttackTick", -1);
			Scribe_Values.Look(ref thunderArrowsTick, "thunderArrowsTick", -1);
			Scribe_Values.Look(ref lastSwapVictimTick, "lastSwapVictimTick", -1);
			Scribe_Values.Look(ref cometRainTick, "cometRainTick", -1);
			Scribe_Values.Look(ref wingsFixTick, "wingsFixTick", -1);
			Scribe_Values.Look(ref attackWings, "attackWings");
			Scribe_Values.Look(ref wingsBroken, "wingsBroken");
			Scribe_Values.Look(ref beaten, "beaten");
			Scribe_Values.Look(ref devAlwaysSwap, "devAlwaysSwap");
			Scribe_TargetInfo.Look(ref caneTargetInfo, "caneTargetInfo");
		}
    }
}
