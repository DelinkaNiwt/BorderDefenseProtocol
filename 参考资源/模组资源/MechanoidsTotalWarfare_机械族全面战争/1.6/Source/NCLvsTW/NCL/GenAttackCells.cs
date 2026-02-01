using System.Collections.Generic;
using Verse;

namespace NCL;

public static class GenAttackCells
{
	public static List<IntVec3> NineCells = new List<IntVec3>
	{
		new IntVec3(1, 0, 1),
		new IntVec3(1, 0, 0),
		new IntVec3(1, 0, -1),
		new IntVec3(0, 0, 1),
		new IntVec3(0, 0, 0),
		new IntVec3(0, 0, -1),
		new IntVec3(-1, 0, 1),
		new IntVec3(-1, 0, 0),
		new IntVec3(-1, 0, -1)
	};

	public static List<IntVec3> TwentyFiveCells = new List<IntVec3>
	{
		new IntVec3(-2, 0, -2),
		new IntVec3(-2, 0, -1),
		new IntVec3(-2, 0, 0),
		new IntVec3(-2, 0, 1),
		new IntVec3(-2, 0, 2),
		new IntVec3(-1, 0, -2),
		new IntVec3(-1, 0, -1),
		new IntVec3(-1, 0, 0),
		new IntVec3(-1, 0, 1),
		new IntVec3(-1, 0, 2),
		new IntVec3(0, 0, -2),
		new IntVec3(0, 0, -1),
		new IntVec3(0, 0, 0),
		new IntVec3(0, 0, 1),
		new IntVec3(0, 0, 2),
		new IntVec3(1, 0, -2),
		new IntVec3(1, 0, -1),
		new IntVec3(1, 0, 0),
		new IntVec3(1, 0, 1),
		new IntVec3(1, 0, 2),
		new IntVec3(2, 0, -2),
		new IntVec3(2, 0, -1),
		new IntVec3(2, 0, 0),
		new IntVec3(2, 0, 1),
		new IntVec3(2, 0, 2)
	};

	public static readonly IntVec3[] NineCellsLocal = new IntVec3[9]
	{
		new IntVec3(1, 0, 1),
		new IntVec3(1, 0, 0),
		new IntVec3(1, 0, -1),
		new IntVec3(0, 0, 1),
		new IntVec3(0, 0, 0),
		new IntVec3(0, 0, -1),
		new IntVec3(-1, 0, 1),
		new IntVec3(-1, 0, 0),
		new IntVec3(-1, 0, -1)
	};
}
