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
	public class CompFamine : ThingComp
	{
		public CompProperties_Famine Props
		{
			get
			{
				return this.props as CompProperties_Famine;
			}
		}

		public Apparel Medal
		{
			get
			{
				return this.parent as Apparel;
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			this.readyToUseTicks++;
			if (this.readyToUseTicks > 60)
			{
				this.readyToUseTicks = 0;
				if (this.abilityActivate)
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
                }
				if (this.abilityActivate2)
                {
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
						Hediff_Injury hediff_Injury = CompFamine.FindInjury(this.Medal.Wearer, source);
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
				Command_Toggle hediffButton = new Command_Toggle
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
				yield return hediffButton;
				yield return healerButton;
			}
			yield break;
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

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
			Scribe_Values.Look<bool>(ref this.abilityActivate, "abilityActivate", false, false);
			Scribe_Values.Look<bool>(ref this.abilityActivate2, "abilityActivate2", false, false);
		}

		private static List<Hediff> tmpHediffs = new List<Hediff>();

		private int readyToUseTicks;

		private bool abilityActivate = false;

		private bool abilityActivate2 = false;
	}
}