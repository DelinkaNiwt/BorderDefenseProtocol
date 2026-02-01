using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class LayoutWorker_BlackSite : LayoutWorker_Structure
	{
		protected override float RoomToExteriorDoorRatio => 0.33f;

		protected override ThingDef GetWallDoorStuff(StructureGenParams parms)
		{
			return ThingDefOf.Uranium;
		}

		public LayoutWorker_BlackSite(LayoutDef def)
			: base(def)
		{
		}

		protected override StructureLayout GetStructureLayout(StructureGenParams parms, CellRect rect)
		{
			LayoutStructureSketch sketch = parms.sketch;
			float areaPrunePercent = base.Def.areaPrunePercent;
			int minRoomHeight = base.Def.minRoomHeight;
			return RoomLayoutGenerator.GenerateRandomLayout(minRoomWidth: base.Def.minRoomWidth, minRoomHeight: minRoomHeight, 
				areaPrunePercent: areaPrunePercent, canRemoveRooms: true, generateDoors: false, maxMergeRoomsRange: IntRange.One, 
				sketch: sketch, container: rect, corridor: base.Def.corridorDef ?? GDDefOf.GDBlackCorridor, 
				corridorExpansion: 2, corridorShapes: base.Def.corridorShapes, canDisconnectRooms: base.Def.canDisconnectRooms);
		}
	}

}
