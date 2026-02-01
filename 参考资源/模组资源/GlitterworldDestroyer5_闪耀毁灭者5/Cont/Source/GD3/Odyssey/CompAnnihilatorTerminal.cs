using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using UnityEngine;

namespace GD3
{
	public class CompProperties_AnnihilatorTerminalHackable : CompProperties_Hackable
	{
		public int delay = 1200;

		public CompProperties_AnnihilatorTerminalHackable()
		{
			compClass = typeof(CompAnnihilatorTerminal);
		}
	}

	public class CompAnnihilatorTerminal : CompHackable
	{
		private new CompProperties_AnnihilatorTerminalHackable Props => (CompProperties_AnnihilatorTerminalHackable)props;

		public int comingTick = -1;

		public bool finished;

        public override void CompTick()
        {
            base.CompTick();
			if (!finished && comingTick != -1 && comingTick <= Find.TickManager.TicksGame)
            {
				finished = true;

				Map map = parent.Map;
				IntVec3 cell = map.Center;

				Pawn pawn = PawnGenerator.GeneratePawn(GDDefOf_Another.Mech_Annihilator, Faction.OfMechanoids);
				((Annihilator)pawn).animation = GDDefOf.Annihilator_Jumping;
				Skyfaller_LandingMech landing = (Skyfaller_LandingMech)SkyfallerMaker.MakeSkyfaller(GDDefOf.GD_Skyfaller_LandingMech, pawn);
				GenSpawn.Spawn(landing, cell, map);
				landing.ticksToImpact += 300;
				Messages.Message("GD.AnnihilatorArrival".Translate(), landing, MessageTypeDefOf.NeutralEvent);
			}
        }

        protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
		{
			base.OnHacked(hacker, suppressMessages);
			comingTick = Find.TickManager.TicksGame + Props.delay;
			Messages.Message("GD.AnnihilatorComing".Translate(Props.delay.ToStringSecondsFromTicks()), MessageTypeDefOf.NeutralEvent);
		}

        public override void PostExposeData()
        {
            base.PostExposeData();
			Scribe_Values.Look(ref comingTick, "comingTick", -1);
			Scribe_Values.Look(ref finished, "finished");
		}
    }
}
