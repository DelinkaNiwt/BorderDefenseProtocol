using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
    public class Command_TargetPart : Command_Action
    {
        public Annihilator annihilator;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                if (annihilator != null)
                {
                    options.Add(new FloatMenuOption("GD.Target.Any".Translate(), delegate
                    {
                        annihilator.targetGroup = null;
                    }));
                    options.Add(new FloatMenuOption("GD.Target.LegLF".Translate(), delegate
                    {
                        annihilator.targetGroup = GDDefOf.Annihilator_Leg_LFront;
                    }));
                    options.Add(new FloatMenuOption("GD.Target.LegRF".Translate(), delegate
                    {
                        annihilator.targetGroup = GDDefOf.Annihilator_Leg_RFront;
                    }));
                    options.Add(new FloatMenuOption("GD.Target.LegLM".Translate(), delegate
                    {
                        annihilator.targetGroup = GDDefOf.Annihilator_Leg_LMiddle;
                    }));
                    options.Add(new FloatMenuOption("GD.Target.LegRM".Translate(), delegate
                    {
                        annihilator.targetGroup = GDDefOf.Annihilator_Leg_RMiddle;
                    }));
                    options.Add(new FloatMenuOption("GD.Target.LegLH".Translate(), delegate
                    {
                        annihilator.targetGroup = GDDefOf.Annihilator_Leg_LHind;
                    }));
                    options.Add(new FloatMenuOption("GD.Target.LegRH".Translate(), delegate
                    {
                        annihilator.targetGroup = GDDefOf.Annihilator_Leg_RHind;
                    }));
                }
                return options;
            }
        }
    }
}
