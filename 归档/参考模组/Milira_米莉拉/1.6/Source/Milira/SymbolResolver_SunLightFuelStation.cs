using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace Milira;

public class SymbolResolver_SunLightFuelStation : SymbolResolver
{
	private struct SpawnDescriptor
	{
		public IntVec3 offset;

		public ThingDef def;

		public Rot4 rot;
	}

	public override void Resolve(ResolveParams rp)
	{
		SpawnDescriptor[] array = new SpawnDescriptor[57];
		SpawnDescriptor spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(0, 0, 0),
			def = MiliraDefOf.Milira_SunLightGatheringTower,
			rot = Rot4.North
		};
		array[0] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(4, 0, 0),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[1] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(4, 0, 2),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[2] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(4, 0, -2),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[3] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(6, 0, 0),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[4] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(6, 0, 2),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[5] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(6, 0, -2),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[6] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(8, 0, 1),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[7] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(8, 0, -1),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[8] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(8, 0, 3),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[9] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(8, 0, -3),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[10] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-4, 0, 0),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[11] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-4, 0, 2),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[12] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-4, 0, -2),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[13] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-6, 0, 0),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[14] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-6, 0, 2),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[15] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-6, 0, -2),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[16] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-8, 0, 1),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[17] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-8, 0, -1),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[18] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-8, 0, 3),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[19] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-8, 0, -3),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[20] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(0, 0, 4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[21] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(2, 0, 4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[22] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-2, 0, 4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[23] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(0, 0, 6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[24] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(2, 0, 6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[25] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-2, 0, 6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[26] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(1, 0, 8),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[27] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-1, 0, 8),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[28] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(3, 0, 8),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[29] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-3, 0, 8),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[30] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(0, 0, -4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[31] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(2, 0, -4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[32] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-2, 0, -4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[33] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(0, 0, -6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[34] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(2, 0, -6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[35] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-2, 0, -6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[36] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(1, 0, -8),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[37] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-1, 0, -8),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[38] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(3, 0, -8),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[39] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-3, 0, -8),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[40] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(4, 0, 4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[41] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(4, 0, 6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[42] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(6, 0, 4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[43] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(6, 0, 6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[44] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(4, 0, -4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[45] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(4, 0, -6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[46] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(6, 0, -4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[47] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(6, 0, -6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[48] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-4, 0, -4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[49] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-4, 0, -6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[50] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-6, 0, -4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[51] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-6, 0, -6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[52] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-4, 0, 4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[53] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-4, 0, 6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[54] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-6, 0, 4),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[55] = spawnDescriptor;
		spawnDescriptor = new SpawnDescriptor
		{
			offset = new IntVec3(-6, 0, 6),
			def = MiliraDefOf.Milira_Heliostat,
			rot = Rot4.North
		};
		array[56] = spawnDescriptor;
		IntVec3 centerCell = rp.rect.CenterCell;
		IntVec3 intVec = new IntVec3(-1, 0, -3);
		SpawnDescriptor[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			SpawnDescriptor spawnDescriptor2 = array2[i];
			Thing thing = ThingMaker.MakeThing(spawnDescriptor2.def);
			thing.SetFaction(rp.faction);
			if (rp.hpPercentRange.HasValue)
			{
				thing.HitPoints = Mathf.Clamp(Mathf.RoundToInt((float)thing.MaxHitPoints * rp.hpPercentRange.Value.RandomInRange), 1, thing.MaxHitPoints);
				GenLeaving.DropFilthDueToDamage(thing, thing.MaxHitPoints - thing.HitPoints);
			}
			GenSpawn.Spawn(thing, centerCell + intVec + spawnDescriptor2.offset, BaseGen.globalSettings.map, spawnDescriptor2.rot);
		}
	}
}
