using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Text;
using UnityEngine;

namespace GD3
{
	public class CompRadio : ThingComp
	{
		public CompProperties_Radio Props
		{
			get
			{
				return (CompProperties_Radio)this.props;
			}
		}

		public List<Pawn> Pawns
        {
            get
            {
				List<Pawn> pawns = this.parent.Map.mapPawns.AllPawns;
				return pawns;
			}
        }

		public CompPowerTrader CompPower
        {
            get
            {
				return this.parent.TryGetComp<CompPowerTrader>();
            }
        }

		public override void CompTick()
		{
			base.CompTick();
			Building building = this.parent as Building;
			Map map = building.Map;
			bool flag = building != null && map != null && Pawns.Count > 0 && CompPower.PowerOn;
			if (flag)
            {
				this.readyToUseTicks++;
				if (readyToUseTicks >= 30)
                {
					this.readyToUseTicks = 0;
					if (this.abilityActivateA)
					{
						CompPower.powerOutputInt = -this.Props.powerCostFst;
					}
					else if (this.abilityActivateB)
					{
						CompPower.powerOutputInt = -this.Props.powerCostSec;
					}
					else if (this.abilityActivateC)
					{
						CompPower.powerOutputInt = -this.Props.powerCostThd;
					}
					else
					{
						CompPower.powerOutputInt = 0;
						return;
					}
					for (int i = 0; i < Pawns.Count; i++)
					{
						Pawn pawn = Pawns[i];
						if (pawn.Faction == null || (pawn.Faction != null && !pawn.Faction.IsPlayer))
                        {
							continue;
                        }
						if (this.abilityActivateA)
						{
							AddRadioHediff(pawn, GDDefOf.PsychicRadioSupport_Fst);
						}
						else if (this.abilityActivateB)
						{
							AddRadioHediff(pawn, GDDefOf.PsychicRadioSupport_Sec);
						}
						else if (this.abilityActivateC)
						{
							AddRadioHediff(pawn, GDDefOf.PsychicRadioSupport_Thd);
						}
					}
				}
            }
		}

		private void AddRadioHediff(Pawn pawn, HediffDef hediffDef)
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
				hediffComp_Disappears.ticksToDisappear = 31;
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			bool flag = this.parent.Faction == Faction.OfPlayer;
			if (flag)
			{
				Command_Toggle AbilityButtonA = new Command_Toggle
				{
					defaultLabel = "PsychicRadioALabel".Translate(this.abilityActivateA ? "On".Translate() : "Off".Translate()),
					defaultDesc = "PsychicRadioADesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Buttons/PsychicRadioToggleA", true),
					toggleAction = delegate ()
					{
						this.abilityActivateA = !this.abilityActivateA;
						if (this.abilityActivateA)
                        {
							this.abilityActivateB = false;
							this.abilityActivateC = false;
						}
					},
					isActive = (() => this.abilityActivateA)
				};
				yield return AbilityButtonA;
			}
			if (flag)
			{
				Command_Toggle AbilityButtonB = new Command_Toggle
				{
					defaultLabel = "PsychicRadioBLabel".Translate(this.abilityActivateB ? "On".Translate() : "Off".Translate()),
					defaultDesc = "PsychicRadioBDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Buttons/PsychicRadioToggleB", true),
					toggleAction = delegate ()
					{
						this.abilityActivateB = !this.abilityActivateB;
						if (this.abilityActivateB)
						{
							this.abilityActivateA = false;
							this.abilityActivateC = false;
						}
					},
					isActive = (() => this.abilityActivateB)
				};
				yield return AbilityButtonB;
			}
			if (flag)
			{
				Command_Toggle AbilityButtonC = new Command_Toggle
				{
					defaultLabel = "PsychicRadioCLabel".Translate(this.abilityActivateC ? "On".Translate() : "Off".Translate()),
					defaultDesc = "PsychicRadioCDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Buttons/PsychicRadioToggleC", true),
					toggleAction = delegate ()
					{
						this.abilityActivateC = !this.abilityActivateC;
						if (this.abilityActivateC)
						{
							this.abilityActivateA = false;
							this.abilityActivateB = false;
						}
					},
					isActive = (() => this.abilityActivateC)
				};
				yield return AbilityButtonC;
			}
			yield break;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003268 File Offset: 0x00001468
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
			Scribe_Values.Look<bool>(ref this.abilityActivateA, "abilityActivateA", false, false);
			Scribe_Values.Look<bool>(ref this.abilityActivateB, "abilityActivateB", false, false);
			Scribe_Values.Look<bool>(ref this.abilityActivateC, "abilityActivateC", false, false);
		}

		// Token: 0x04000010 RID: 16
		private int readyToUseTicks = 0;

		private bool abilityActivateA = false;

		private bool abilityActivateB = false;

		private bool abilityActivateC = false;
	}
}