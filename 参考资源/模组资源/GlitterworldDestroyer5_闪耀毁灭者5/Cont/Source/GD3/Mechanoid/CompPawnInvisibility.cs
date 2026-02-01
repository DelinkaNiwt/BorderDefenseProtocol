using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Text;
using UnityEngine;

namespace GD3
{
	public class CompProperties_Invisibility : CompProperties
	{
		public CompProperties_Invisibility()
		{
			this.compClass = typeof(CompPawnInvisibility);
		}

		public float maxDistance;

		public IntRange interval;

		public int actPeriod;

		public float energyCostNum = 0.075f;

		public HediffDef hediffToAdd;

		public HediffDef hediffDef;

		public string toggleLabelKey;

		public string toggleDescKey;

		public string toggleIconPath;
	}

	public class CompPawnInvisibility : ThingComp
	{
		public CompProperties_Invisibility Props
		{
			get
			{
				return (CompProperties_Invisibility)this.props;
			}
		}

		public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
			this.abilityActivate = true;
        }

		public Need_MechEnergy energy => ((Pawn)this.parent).needs.energy;

		public override void CompTick()
		{
			base.CompTick();
			ThingWithComps parent = this.parent;
			Pawn pawn = parent as Pawn;
			if (energy != null && energy.CurLevel <= 5 || ModsConfig.BiotechActive && (pawn.CurJobDef == JobDefOf.MechCharge || pawn.CurJobDef == JobDefOf.SelfShutdown))
            {
				this.abilityActivate = false;
				return;
            }
			if (pawn == null || pawn.Faction == null)
            {
				return;
            }
			bool flag = pawn != null && pawn.Spawned && Find.TickManager.TicksGame >= this.readyToUseTicks && !pawn.Downed;
			this.distance = this.Props.maxDistance;
			if (flag && this.abilityActivate && !pawn.stances.stunner.StunFromEMP)
			{
				this.readyToUseTicks = Find.TickManager.TicksGame + this.Props.interval.RandomInRange;
				IEnumerable<Pawn> enumerable = from x in pawn.Map.mapPawns.AllPawns
				where x.Position.DistanceTo(pawn.Position) < this.distance && x.RaceProps.IsMechanoid && x.def.defName != "Mech_Firefly" && x.Faction == pawn.Faction
											   select x;
				if (pawn.Faction == Faction.OfPlayer && enumerable.Count() > 0 && energy != null)
                {
					this.ApplyEnergyCost(enumerable.ToList());
                }
				FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 2.0f);
				foreach (Pawn pawn2 in enumerable)
				{
					Hediff plagueOnPawn = pawn2.health?.hediffSet?.GetFirstHediffOfDef(Props.hediffToAdd);
					bool flag2 = pawn2 != pawn;
					if (flag2)
					{
						int num = this.Props.actPeriod;
						if (plagueOnPawn != null)
						{
							HediffComp_Disappears hediffComp_Disappears = plagueOnPawn.TryGetComp<HediffComp_Disappears>();
							if (hediffComp_Disappears != null)
							{
								hediffComp_Disappears.ticksToDisappear = 600;
							}
						}
						else
						{
							Hediff hediff = HediffMaker.MakeHediff(this.Props.hediffToAdd, pawn2);
							HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
							if (hediffComp_Disappears != null)
							{
								hediffComp_Disappears.ticksToDisappear = 600;
							}
							pawn2.health.AddHediff(hediff);
						}
					}
				}
			}
		}

		public void ApplyEnergyCost(List<Pawn> list)
        {
			if (energy.CurLevel <= 5)
            {
				return;
            }
			for (int i = 0; i < list.Count; i++)
            {
				if (energy.CurLevel <= 5)
				{
					break;
				}
				Pawn p = list[i];
				float bodySize = p.BodySize;
				energy.CurLevel -= this.Props.energyCostNum * bodySize;
            }
        }

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Pawn pawn = (Pawn)this.parent;
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			bool flag = this.parent.Faction == Faction.OfPlayer;
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
				bool isNotValid = energy != null && energy.CurLevel <= 5;
				if (isNotValid)
				{
					berserkButton.Disable("GD.FireflyNoEnergy".Translate());
				}
				bool laying = ModsConfig.BiotechActive && (pawn.CurJobDef == JobDefOf.MechCharge || pawn.CurJobDef == JobDefOf.SelfShutdown);
				if (laying)
				{
					berserkButton.Disable("GD.FireflyLaying".Translate());
				}
				yield return berserkButton;
			}
			yield break;
		}
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
			Scribe_Values.Look<bool>(ref this.abilityActivate, "abilityActivate", true, false);
		}

		private int readyToUseTicks = 0;

		private float distance;

		private bool abilityActivate = true;
	}
}
