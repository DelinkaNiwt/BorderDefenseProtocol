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
	public class CompDeath : ThingComp
	{
		public CompProperties_Death Props
		{
			get
			{
				return this.props as CompProperties_Death;
			}
		}

		public Apparel Medal
		{
			get
			{
				return this.parent as Apparel;
			}
		}

		private bool CanPayback
		{
			get
			{
				return this.lastAttackTick <= Find.TickManager.TicksGame - 45f;
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			if (this.readyToUseTicks > 0)
            {
				readyToUseTicks--;
            }
			if (this.curTarget != null)
            {
				Pawn pawn = this.curTarget.Pawn;
				if (pawn.Dead)
                {
					this.curTarget = null;
					return;
                }
				if (!pawn.Downed)
                {
					this.TryPayback(pawn);
				}
				if (pawn.GetStatValue(StatDefOf.PsychicSensitivity) != 0)
				{
					Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(GDDefOf.GD_MedalSupport, false);
					if (hediff == null)
					{
						hediff = pawn.health.AddHediff(GDDefOf.GD_MedalSupport, pawn.health.hediffSet.GetBrain(), null, null);
						hediff.Severity = 1f;
					}
					if (pawn.mindState.mentalStateHandler.InMentalState)
					{
						if (pawn.mindState.mentalStateHandler.CurStateDef != MentalStateDefOf.Berserk)
						{
							pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
							pawn.mindState.mentalStateHandler.Reset();
						}
					}
					if (!pawn.mindState.mentalStateHandler.InMentalState)
					{
						pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "CausedByDeathMedal".Translate(), true, false, false, null, false, false);
					}
				}
			}
		}

		public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
			{
				yield return gizmo;
			}
			bool flag = this.Medal.Wearer.Faction == Faction.OfPlayer;
			if (flag)
			{
				Command_Action affectCorpse = new Command_Action
				{
					defaultLabel = this.Props.toggleLabelKey.Translate(),
					defaultDesc = this.Props.toggleDescKey.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					action = delegate ()
					{
						Find.Targeter.BeginTargeting(this.ConnectCorpseTargetParameters(), new Action<LocalTargetInfo>(this.StartAffect), null, new Func<LocalTargetInfo, bool>(this.CanAffect), null, null, null, true, null);
					}
				};
				Command_Action killCorpse = new Command_Action
				{
					defaultLabel = this.Props.toggleLabelKey2.Translate(),
					defaultDesc = this.Props.toggleDescKey2.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					action = delegate ()
					{
						this.DamageUntilDead(this.curTarget.Pawn);
						if (this.curTarget.Pawn.Dead)
                        {
							this.curTarget = null;
						}
					}
				};
				bool cooling = this.readyToUseTicks > 0;
				if (cooling)
                {
					affectCorpse.Disable("GD.DeathCoolDown".Translate(Mathf.Floor(this.readyToUseTicks / 60)));
				}
				bool isValid = this.curTarget.IsValid;
				if (!isValid)
				{
					killCorpse.Disable("GD.NoDeathTarget".Translate());
				}
                else
                {
					affectCorpse.Disable("GD.TargetExist".Translate());
				}
				yield return affectCorpse;
				yield return killCorpse;
			}
			yield break;
		}

		public TargetingParameters ConnectCorpseTargetParameters()
		{
			return new TargetingParameters
			{
				canTargetPawns = true,
				canTargetBuildings = false,
				canTargetHumans = true,
				canTargetMechs = true,
				canTargetAnimals = true,
				canTargetLocations = false,
				validator = ((TargetInfo x) => this.CanAffect((LocalTargetInfo)x))
			};
		}

		public bool CanAffect(LocalTargetInfo target)
		{
			Pawn pawn = target.Pawn;
			return CanAffectCorpse(pawn, this.readyToUseTicks) && this.curTarget == null;
		}

		public static AcceptanceReport CanAffectCorpse(Pawn pawn, int ticks)
		{
			AcceptanceReport result;
			if (pawn == null || pawn.Dead)
			{
				result = false;
			}
			else
			{
				bool flag2 = pawn.def.defName == "Mech_BlackScyther" || pawn.def.defName == "Mech_BlackLancer" || pawn.def.defName == "Mech_BlackApocriton";
				if (flag2)
				{
					result = "GD.PawnIsBlackMech".Translate();
				}
				else
				{
					bool flag3 = pawn.Downed;
					if (flag3)
					{
						result = "GD.PawnDowned".Translate();
					}
					else
					{
						bool flag4 = ticks > 0;
						if (flag4)
                        {
							result = "GD.DeathCoolDown".Translate(Mathf.Floor(ticks / 60));
                        }
                        else
                        {
							result = true;
						}
					}
				}
			}
			return result;
		}

		public void StartAffect(LocalTargetInfo target)
		{
			this.curTarget = target;
			this.readyToUseTicks = 3600;
			HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(target.Pawn);
			GenExplosion.DoExplosion(target.Pawn.Position, target.Pawn.Map, -1, GDDefOf.MechBandShockwave, target.Pawn, -1, -1, GDDefOf.Explosion_MechBandShockwave, null, null, null, null, 0.2f, 1, null, null, 255, false, null, 0f, 1, 0.4f, false, null, null, null, true, 1f, 0f, true, null, 1f);
		}

		private void TryPayback(Pawn pawn)
		{
			if (pawn != null && this.CanPayback)
			{
				List<Building> list = pawn.Map.listerBuildings.allBuildingsNonColonist;
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].Position.DistanceTo(pawn.Position) <= 33.9f && list[i].Faction.HostileTo(this.Medal.Wearer.Faction))
					{
						this.lastAttackTick = Find.TickManager.TicksGame;
						Map mapHeld = pawn.MapHeld;
						mapHeld.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(mapHeld, list[i].PositionHeld));
						int buildingHealth = list[i].HitPoints;
						int repayDamage = 80 + buildingHealth / 3;
						GenExplosion.DoExplosion(list[i].PositionHeld, list[i].Map, 4.9f, GDDefOf.BombCharge, pawn, repayDamage, 1.8f, GDDefOf.Explosion_Bomb, null, null, pawn, null, 0.2f, 1, null, null, 255, false, null, 0f, 1, 0.4f, false, null, null, null, true, 1f, 0f, true, null, 1f);
					}
				}
				List<Pawn> list2 = pawn.Map.mapPawns.AllPawns;
				for (int i = 0; i < list2.Count; i++)
                {
					if (list2[i].Position.DistanceTo(pawn.Position) <= 33.9f && list2[i].Faction.HostileTo(this.Medal.Wearer.Faction) && list2[i] != pawn)
                    {
						this.lastAttackTick = Find.TickManager.TicksGame;
						Map mapHeld2 = pawn.MapHeld;
						mapHeld2.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(mapHeld2, list2[i].PositionHeld));
						GenExplosion.DoExplosion(list2[i].PositionHeld, list2[i].Map, 2.9f, GDDefOf.BombFrostBite, pawn, 10, 1.8f, GDDefOf.Explosion_Bomb, null, null, pawn, null, 0.2f, 1, null, null, 255, false, null, 0f, 1, 0.4f, false, null, null, null, true, 1f, 0f, true, null, 1f);
						return;
					}
                }
			}
		}

		private void DamageUntilDead(Pawn p)
		{
			HediffSet hediffSet = p.health.hediffSet;
			int num = 0;
			while (!p.Dead && num < 200 && HittablePartsViolence(hediffSet).Any())
			{
				num++;
				BodyPartRecord bodyPartRecord = HittablePartsViolence(hediffSet).RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
				int num2 = Rand.RangeInclusive(400, 900);
				DamageDef def = (bodyPartRecord.depth != BodyPartDepth.Outside) ? DamageDefOf.Blunt : RandomViolenceDamageType();
				DamageInfo dinfo = new DamageInfo(def, num2, 999f, -1f, null, bodyPartRecord);
				dinfo.SetIgnoreInstantKillProtection(ignore: true);
				p.TakeDamage(dinfo);
			}

			if (!p.Dead)
			{
				Log.Error(string.Concat(p, " not killed during GiveInjuriesToKill"));
			}
		}

		private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
		{
			return from x in bodyModel.GetNotMissingParts()
				   where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
				   select x;
		}

		private static DamageDef RandomViolenceDamageType()
		{
			switch (Rand.RangeInclusive(0, 4))
			{
				case 0:
					return DamageDefOf.Bullet;
				case 1:
					return DamageDefOf.Blunt;
				case 2:
					return DamageDefOf.Stab;
				case 3:
					return DamageDefOf.Scratch;
				case 4:
					return DamageDefOf.Cut;
				default:
					return null;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
			Scribe_Values.Look<int>(ref this.lastAttackTick, "lastAttackTick", 0, false);
			Scribe_Values.Look<bool>(ref this.abilityActivate, "abilityActivate", false, false);
		}

		private int readyToUseTicks;

		private bool abilityActivate = false;

		private int lastAttackTick;

		public LocalTargetInfo curTarget = LocalTargetInfo.Invalid;
	}
}