using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Text;
using UnityEngine;

namespace GD3
{
	public class CompTerror : ThingComp
	{
		public CompProperties_Terror Props
		{
			get
			{
				return (CompProperties_Terror)this.props;
			}
		}

        public Pawn Attacker
        {
            get
            {
                return this.parent as Pawn;
            }
        }

		public List<Pawn> ListPawn
        {
            get
            {
                if (Attacker == null || Attacker.Map?.mapPawns == null)
                {
                    return null;
                }
                IEnumerable<Pawn> list = from x in Attacker.Map.mapPawns.AllPawnsSpawned
                                         where x != null && !x.Downed && x.RaceProps.Humanlike && !x.IsPrisoner && !x.mindState.mentalStateHandler.InMentalState && x.Position.DistanceTo(Attacker.Position) <= this.Props.range
                                         select x;
                return list.ToList();
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            this.ticks++;
            if (Attacker.Faction == null)
            {
                return;
            }
            if (this.ticks > this.Props.interval && !Attacker.Downed)
            {
                this.ticks = 0;
                if (ListPawn == null || ListPawn.Count <= 0)
                {
                    return;
                }
                for (int i = 0; i < this.ListPawn.Count; i++)
                {
                    Pawn pawn = this.ListPawn[i];
                    if (IsBrave(pawn) || pawn.Faction != null && Attacker.Faction != null && !pawn.Faction.HostileTo(Attacker.Faction) || pawn.IsPrisoner)
                    {
                        continue;
                    }
                    if (pawn != null && this.Props.applyThought && pawn.needs.mood != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(this.Props.thought);
                    }
                    Hediff terror = pawn.health?.hediffSet?.GetFirstHediffOfDef(Props.hediff);
                    bool flag = pawn != null;
                    if (flag)
                    {
                        if (terror != null)
                        {
                            terror.Severity += (this.Props.severityToAdd);
                        }
                        else
                        {
                            Hediff hediff = HediffMaker.MakeHediff(this.Props.hediff, pawn);
                            hediff.Severity = this.Props.severityToAdd;
                            pawn.health.AddHediff(hediff);
                        }
                    }
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("GD_TerrorSource".Translate());
            stringBuilder.AppendLine("GD_TerrorTip".Translate() + ":" + (this.Props.terrorLevel).ToString());
            return stringBuilder.ToString();
        }

        public bool IsBrave(Pawn p)
        {
            bool flag1 = p.skills.GetSkill(SkillDefOf.Shooting).PermanentlyDisabled;
            bool flag2 = p.skills.GetSkill(SkillDefOf.Melee).PermanentlyDisabled;
            if (flag1 && flag2)
            {
                return false;
            }
            if (p.story.traits.HasTrait(TraitDefOf.Wimp))
            {
                return false;
            }
            if (!flag1)
            {
                lvl1 = p.skills.GetSkill(SkillDefOf.Shooting).Level;
            }
            else
            {
                lvl1 = 0;
            }
            if (!flag2)
            {
                lvl2 = p.skills.GetSkill(SkillDefOf.Melee).Level;
            }
            else
            {
                lvl2 = 0;
            }
            if (lvl1 + lvl2 > this.Props.terrorLevel)
            {
                return true;
            }
            return false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
        }

        public int ticks = 0;

        private int lvl1;

        private int lvl2;
    }
}
