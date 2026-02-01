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
	public class CompConquer : ThingComp
	{
		public CompProperties_Conquer Props
		{
			get
			{
				return this.props as CompProperties_Conquer;
			}
		}

		public Apparel Medal
		{
			get
			{
				return this.parent as Apparel;
			}
		}

		public bool CanBerserk
        {
            get
            {
				return this.readyToUseTicks2 <= 0;
            }
        }

		public override void CompTick()
		{
			base.CompTick();
			this.readyToUseTicks++;
			if (this.readyToUseTicks2 > 0)
            {
				this.readyToUseTicks2--;
            }
			Corpse corpse;
			if (this.abilityActivate2 && this.readyToUseTicks % 12 == 0)
			{
				List<Pawn> list = this.Medal.Wearer.Map.mapPawns.AllPawns;
				if (list.Count > 0)
				{
					for (int i = 0; i < list.Count; i++)
					{
						Pawn p = list[i];
						if (p.Faction == null || !p.Faction.HostileTo(Faction.OfPlayer))
						{
							continue;
						}
						this.AddHediff(p, GDDefOf.MedalFamineHediff);
					}
				}
				List<Thing> list2 = this.Medal.Wearer.Map.listerThings.AllThings;
				for (int i = 0; i < list2.Count; i++)
				{
					if ((corpse = list2[i] as Corpse) != null && corpse.InnerPawn.Faction != null && corpse.InnerPawn.Faction.HostileTo(this.Medal.Wearer.Faction))
					{
						IEnumerable<Pawn> enumerable = from x in corpse.Map.mapPawns.AllPawns
													   where x.Position.DistanceTo(corpse.Position) < 5.9f
													   select x;
						if (enumerable.Count() == 0)
						{
							//list.Remove(corpse);
							continue;
						}
						List<Pawn> list3 = enumerable.ToList();
						for (i = 0; i < list3.Count; i++)
						{
							Pawn pawn = list3[i];
							bool flag2 = pawn != null && !pawn.Dead && pawn.Faction == corpse.InnerPawn.Faction;
							if (flag2)
							{
								FleckMaker.ConnectingLine(corpse.DrawPos, pawn.DrawPos, GDDefOf.PsycastPsychicLine, corpse.Map, 1f);
								GDDefOf.PocketThunderEffect.Spawn(corpse.Position, corpse.Map, 1f).EffectTick(new TargetInfo(corpse.Position, corpse.Map, false), null);
								DamageVictim(corpse, pawn);
							}
						}
						break;
					}
				}
			}
			if (this.abilityActivate3 && this.readyToUseTicks % 12 == 0)
			{
				this.RemoveBadHediffs(this.Medal.Wearer);
				int num = 2;
				IEnumerable<BodyPartRecord> source;
				source = from x in this.Medal.Wearer.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
						 where !x.def.conceptual
						 select x;
				if (source.Count() <= 0)
				{
					return;
				}
				for (int i = 0; i < num; i++)
				{
					Hediff_Injury hediff_Injury = CompConquer.FindInjury(this.Medal.Wearer, source);
					if (hediff_Injury != null && hediff_Injury.CanHealNaturally() && !hediff_Injury.IsPermanent())
					{
						float healPower = 4.0f;
						if (hediff_Injury.Severity >= 4.0f)
						{
							hediff_Injury.Heal(healPower);
						}
						else
						{
							HealthUtility.Cure(hediff_Injury);
						}
					}
				}
			}
			if (this.readyToUseTicks > 60)
            {
				this.readyToUseTicks = 0;
            }
		}

		private void DamageVictim(Corpse corpse, Pawn victim)
		{
			if (victim.Dead)
			{
				return;
			}
			HediffSet hediffSet = victim.health.hediffSet;
			IEnumerable<BodyPartRecord> source = from x in HittablePartsViolence(hediffSet)
												 where !victim.health.hediffSet.hediffs.Any((Hediff y) => y.Part == x && y.CurStage != null && y.CurStage.partEfficiencyOffset < 0f)
												 select x;
			BodyPartRecord bodyPartRecord = source.RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
			if (bodyPartRecord == null)
			{
				return;
			}
			int maxHitPoints = bodyPartRecord.def.hitPoints;
			int num = Rand.RangeInclusive(maxHitPoints / 2, maxHitPoints / 2);
			Pawn attacker = this.Medal.Wearer;
			SoundDefOf.PsycastPsychicPulse.PlayOneShot(new TargetInfo(corpse.PositionHeld, corpse.MapHeld, false));
			victim.TakeDamage(new DamageInfo(DamageDefOf.Flame, (float)num, 10f, 0f, attacker, bodyPartRecord, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
		}
		private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
		{
			return from x in bodyModel.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
				   where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
				   select x;
		}
		private void RemoveBadHediffs(Pawn p)
		{
			if (p.Dead)
			{
				return;
			}
			if (p.mindState.mentalStateHandler.InMentalState)
            {
				p.mindState.mentalStateHandler.Reset();
            }
			tmpHediffs.Clear();
			tmpHediffs.AddRange(p.health.hediffSet.hediffs);
			for (int i = 0; i < tmpHediffs.Count; i++)
			{
				Hediff hediff = tmpHediffs[i];
				if (hediff != null && hediff.def.isBad && !(hediff is Hediff_Injury) && !(hediff is Hediff_MissingPart) && !(hediff is Hediff_Addiction) && !(hediff is Hediff_AddedPart))
				{
					p.health.RemoveHediff(hediff);
					continue;
				}
			}
			tmpHediffs.Clear();
		}

		private void AddHediff(Pawn pawn, HediffDef hediffDef)
		{
			Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef, false);
			if (hediff == null)
			{
				hediff = pawn.health.AddHediff(hediffDef, pawn.health.hediffSet.GetBrain(), null, null);
				hediff.Severity = 1f;
			}
			HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
			if (hediffComp_Disappears != null)
			{
				hediffComp_Disappears.ticksToDisappear = 61;
			}
		}

		private static Hediff_Injury FindInjury(Pawn pawn, IEnumerable<BodyPartRecord> allowedBodyParts = null)
		{
			Hediff_Injury hediff_Injury = null;
			List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
			for (int i = 0; i < hediffs.Count; i++)
			{
				Hediff_Injury hediff_Injury2 = hediffs[i] as Hediff_Injury;
				if (hediff_Injury2 != null && hediff_Injury2.Visible && hediff_Injury2.def.everCurableByItem && (allowedBodyParts == null || allowedBodyParts.Contains(hediff_Injury2.Part)) && (hediff_Injury == null || hediff_Injury2.Severity > hediff_Injury.Severity))
				{
					hediff_Injury = hediff_Injury2;
				}
			}
			return hediff_Injury;
		}

		private void AllBerserk()
        {
			List<Pawn> list = this.Medal.Wearer.Map.mapPawns.AllPawnsSpawned.ToList();
			for (int i = 0; i < list.Count; i++)
            {
				Pawn p = list[i];
				if (p != null && !p.Dead && !p.Downed && p.def.defName != "Mech_BlackScyther" && p.def.defName != "Mech_BlackLancer" && p.Faction != null && p.Faction.HostileTo(this.Medal.Wearer.Faction) && !p.mindState.mentalStateHandler.InMentalState)
                {
					p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                }
            }
			GenExplosion.DoExplosion(this.Medal.Wearer.Position, this.Medal.Wearer.Map, -1, GDDefOf.MechBandShockwave, this.Medal.Wearer, -1, -1, GDDefOf.Explosion_MechBandShockwave, null, null, null, null, 0.2f, 1, null, null, 255, false, null, 0f, 1, 0.4f, false, null, null, null, true, 1f, 0f, true, null, 1f);
			this.readyToUseTicks2 = 3600;
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
				Command_Action bombardmentButton = new Command_Action
				{
					defaultLabel = this.Props.toggleLabelKey.Translate(),
					defaultDesc = this.Props.toggleDescKey.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					hotKey = KeyBindingDefOf.Misc1,
					action = delegate ()
					{
						Find.Targeter.BeginTargeting(this.ConnectCorpseTargetParameters(), new Action<LocalTargetInfo>(this.BombardBig), null, new Func<LocalTargetInfo, bool>(this.CanAffect), null, null, null, true, null);
					}
				};
				Command_Action berserkButton = new Command_Action
				{
					defaultLabel = this.Props.toggleLabelKey4.Translate(),
					defaultDesc = this.Props.toggleDescKey4.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					action = delegate ()
					{
						this.AllBerserk();
					}
				};
				Command_Toggle hediffButton = new Command_Toggle
				{
					defaultLabel = this.Props.toggleLabelKey2.Translate(this.abilityActivate2 ? "On".Translate() : "Off".Translate()),
					defaultDesc = this.Props.toggleDescKey2.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					toggleAction = delegate ()
					{
						this.abilityActivate2 = !this.abilityActivate2;
					},
					isActive = (() => this.abilityActivate2)
				};
				Command_Toggle healerButton = new Command_Toggle
				{
					defaultLabel = this.Props.toggleLabelKey3.Translate(this.abilityActivate3 ? "On".Translate() : "Off".Translate()),
					defaultDesc = this.Props.toggleDescKey3.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					toggleAction = delegate ()
					{
						this.abilityActivate3 = !this.abilityActivate3;
					},
					isActive = (() => this.abilityActivate3)
				};
				if (!this.CanBerserk)
				{
					berserkButton.Disable("GD.ConquerCooling".Translate(Mathf.Floor(this.readyToUseTicks2 / 60)));
				}
				yield return bombardmentButton;
				yield return berserkButton;
				yield return hediffButton;
				yield return healerButton;
			}
			yield break;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
			Scribe_Values.Look<int>(ref this.readyToUseTicks2, "readyToUseTicks2", 0, false);
			Scribe_Values.Look<bool>(ref this.abilityActivate2, "abilityActivate2", false, false);
			Scribe_Values.Look<bool>(ref this.abilityActivate3, "abilityActivate3", false, false);
		}

		public TargetingParameters ConnectCorpseTargetParameters()
		{
			return new TargetingParameters
			{
				canTargetPawns = false,
				canTargetBuildings = false,
				canTargetHumans = false,
				canTargetMechs = false,
				canTargetAnimals = false,
				canTargetLocations = true,
				validator = ((TargetInfo x) => this.CanAffect((LocalTargetInfo)x))
			};
		}

		public bool CanAffect(LocalTargetInfo target)
		{
			IntVec3 vec3 = target.Cell;
			return vec3.IsValid;
		}

		private void BombardBig(LocalTargetInfo target)
		{
			IntVec3 vec3 = target.Cell;
			GDDefOf.PocketThunderEffect.Spawn(vec3, this.Medal.Wearer.Map, 1f).EffectTick(new TargetInfo(vec3, this.Medal.Wearer.Map, false), null);
			GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.GD_DummyBombardment, null), vec3, this.Medal.Wearer.Map, ThingPlaceMode.Near, null, null, default(Rot4));
		}

		private static List<Hediff> tmpHediffs = new List<Hediff>();

		private int readyToUseTicks;

		private int readyToUseTicks2;

		private bool abilityActivate2 = false;

		private bool abilityActivate3 = false;
	}
}