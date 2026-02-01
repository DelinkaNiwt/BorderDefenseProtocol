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
	public class CompPlague : ThingComp
	{
		public CompProperties_Plague Props
		{
			get
			{
				return this.props as CompProperties_Plague;
			}
		}

		public Apparel Medal
        {
            get
            {
				return this.parent as Apparel;
            }
        }

		private List<Thing> cachedCorpse = new List<Thing>();

        public override void CompTick()
        {
			this.readyToUseTicks++;
			Corpse corpse;
			if (this.abilityActivate2)
            {
				this.RemoveBadHediffs(this.Medal.Wearer);
            }

			if (Medal.IsHashIntervalTick(300))
            {
				cachedCorpse = this.Medal.Wearer?.MapHeld?.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            }
			if (this.abilityActivate && this.readyToUseTicks > 12)
            {
				this.readyToUseTicks = 0;
				if (cachedCorpse.NullOrEmpty())
                {
					return;
                }
				for (int i = 0; i < cachedCorpse.Count; i++)
				{
					if ((corpse = cachedCorpse[i] as Corpse) != null)
					{
						IEnumerable<Thing> enumerable = GenRadial.RadialDistinctThingsAround(corpse.PositionHeld, corpse.MapHeld, 3.9f, false).Where(t => t is Pawn);
						if (!enumerable.Any())
						{
							continue;
						}
						foreach (Thing thing in enumerable)
						{
							Pawn pawn = thing as Pawn;
							bool flag2 = pawn != null && !pawn.Dead && pawn.HostileTo(Medal.Wearer);
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
			float num = maxHitPoints / 2 / victim.GetStatValue(StatDefOf.IncomingDamageFactor);
			Pawn attacker = this.Medal.Wearer;
			SoundDefOf.PsycastPsychicPulse.PlayOneShot(new TargetInfo(corpse.PositionHeld, corpse.MapHeld, false));
			victim.TakeDamage(new DamageInfo(DamageDefOf.Flame, num, 10f, 0f, attacker, bodyPartRecord, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
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

		public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
			{
				yield return gizmo;
			}
			bool flag = this.Medal.Wearer.Faction == Faction.OfPlayer;
			if (flag)
			{
				Command_Toggle berserkButton = new Command_Toggle
				{
					defaultLabel = this.Props.toggleLabelKey.Translate(this.abilityActivate ? "On".Translate() : "Off".Translate()),
					defaultDesc = this.Props.toggleDescKey.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					toggleAction = delegate ()
					{
						this.abilityActivate = !this.abilityActivate;
					},
					isActive = (() => this.abilityActivate)
				};
				Command_Toggle healerButton = new Command_Toggle
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
				yield return berserkButton;
				yield return healerButton;
			}
			yield break;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
			Scribe_Values.Look<bool>(ref this.abilityActivate, "abilityActivate", false, false);
			Scribe_Values.Look<bool>(ref this.abilityActivate2, "abilityActivate2", false, false);
			Scribe_Collections.Look(ref cachedCorpse, "cachedCorpse", LookMode.Reference);
		}

		private static List<Hediff> tmpHediffs = new List<Hediff>();

		private int readyToUseTicks;

		private bool abilityActivate = false;

		private bool abilityActivate2 = false;
	}
}