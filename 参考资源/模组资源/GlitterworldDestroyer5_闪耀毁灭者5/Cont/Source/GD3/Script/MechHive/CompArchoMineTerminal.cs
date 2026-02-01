using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using UnityEngine;

namespace GD3
{
	public class CompProperties_ArchoMineTerminalHackable : CompProperties_Hackable
	{
		public float dropDataChance = 0.3f;

		public CompProperties_ArchoMineTerminalHackable()
		{
			compClass = typeof(CompArchoMineTerminal);
		}
	}

	public class CompArchoMineTerminal : CompHackable
	{
		private new CompProperties_ArchoMineTerminalHackable Props => (CompProperties_ArchoMineTerminalHackable)props;

		protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
		{
			base.OnHacked(hacker, suppressMessages);
			List<Pawn> drones = parent.Map.mapPawns.AllPawnsSpawned.ToList().FindAll(p => p.kindDef == GDDefOf.Drone_ArchoHunter);
			if (!drones.NullOrEmpty())
			{
				Pawn drone = drones.RandomElement();
				drone.Kill(null);
				Messages.Message("GD.DroneKilled".Translate(), drone, MessageTypeDefOf.NeutralEvent);
			}
            else
            {
				Messages.Message("GD.DroneNotFound".Translate(), MessageTypeDefOf.NeutralEvent);
			}
			if (Rand.Chance(Props.dropDataChance))
            {
				IntVec3 pos = parent.InteractionCell;
				GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.BlackPersonaData), pos, parent.Map, ThingPlaceMode.Near);
            }
		}
	}
}
