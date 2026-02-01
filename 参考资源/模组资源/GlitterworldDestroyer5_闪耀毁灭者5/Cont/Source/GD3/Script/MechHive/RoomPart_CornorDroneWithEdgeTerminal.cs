using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
	public class RoomPart_CornorDroneWithEdgeTerminal : RoomPartWorker
	{
		public RoomPart_CornorDroneWithEdgeTerminal(RoomPartDef def)
			: base(def)
		{
		}

		public override void FillRoom(Map map, LayoutRoom room, Faction faction, float threatPoints)
		{
			GDUtility.TryGeneratePawnInRandomCorner(map, room, GDDefOf.Drone_ArchoHunter, GDUtility.BlackMechanoid);
			RoomGenUtility.FillAroundEdges(GDDefOf.GD_DroneTerminal, 1, new IntRange(1, 1), room, map);
		}
	}

}
