using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using Verse.Sound;

namespace GD3
{
	public class CompProperties_Apocriton : CompProperties
	{
		public CompProperties_Apocriton()
		{
			this.compClass = typeof(CompApocriton);
		}

		public int affectRangeFst;

		public int affectRangeSec;
	}

	public class CompApocriton : ThingComp
	{
		public CompProperties_Apocriton Props
		{
			get
			{
				return (CompProperties_Apocriton)this.props;
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			Pawn pawn = this.parent as Pawn;
			Map map = pawn.Map;
			bool flag = pawn != null && pawn.Spawned;
			if (flag)
			{
				bool flag2 = pawn.health.summaryHealth.SummaryHealthPercent <= 0.5f;
				if (flag2 && !this.hatredTriggered)
				{
					IEnumerable<Pawn> enumerable = from x in pawn.Map.mapPawns.AllPawns
												   where x.Position.DistanceTo(pawn.Position) < this.Props.affectRangeFst
												   select x;
					FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect, 20);
					foreach (Pawn pawn2 in enumerable)
					{
						bool flag4 = pawn2 != pawn && pawn2.RaceProps.IsFlesh && pawn2.Faction != null && pawn2.Faction.IsPlayer;
						if (flag4)
                        {
							if (!this.hatredTriggered)
							{
								bool flag3 = pawn2.MentalStateDef == null && pawn2.GetStatValue(StatDefOf.PsychicSensitivity) >= 2.0f;
								if (flag3)
								{
									pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "CausedByApocriton".Translate(), true, false, false, null, false, false);
								}
							}
						}
						this.hatredTriggered = true;
					}
				}	
			}
		}

		public void MentalBreakOnKilled(Map map)
		{
			/*IEnumerable<Pawn> enumerable = map.mapPawns.AllPawns;
			List<Pawn> list = enumerable.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				Pawn pawn3 = list[i];
				bool flag4 = pawn3.RaceProps.IsFlesh && !pawn3.Dead && (pawn3.Faction != null && pawn3.Faction.HostileTo(this.parent.Faction));
				if (flag4)
				{
					bool flag3 = pawn3.GetStatValue(StatDefOf.PsychicSensitivity) >= 2.0f;
					if (flag3)
					{
						Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, pawn3, null);
						BodyPartRecord part;
						pawn3.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).TryRandomElement(out part);
						pawn3.health.AddHediff(hediff, part, null, null);
					}
				}
			}*/
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref this.hatredTriggered, "hatredTriggered", false, false);
		}

		private bool hatredTriggered;
	}
}
