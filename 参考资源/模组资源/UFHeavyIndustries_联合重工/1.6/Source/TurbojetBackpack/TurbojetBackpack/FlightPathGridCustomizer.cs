using RimWorld;
using Unity.Collections;
using Verse;

namespace TurbojetBackpack;

public class FlightPathGridCustomizer : PathRequest.IPathGridCustomizer
{
	private Map map;

	private TurbojetMode mode;

	public FlightPathGridCustomizer(Map map, TurbojetMode mode)
	{
		this.map = map;
		this.mode = mode;
	}

	public NativeArray<ushort> GetOffsetGrid()
	{
		NativeArray<ushort> result = new NativeArray<ushort>(map.cellIndices.NumGridCells, Allocator.TempJob);
		Building[] innerArray = map.edificeGrid.InnerArray;
		ushort value = 20000;
		ushort value2 = 5;
		int x = map.Size.x;
		int z = map.Size.z;
		int num = 0;
		for (int i = 0; i < z; i++)
		{
			for (int j = 0; j < x; j++)
			{
				IntVec3 c = new IntVec3(j, 0, i);
				bool flag = map.roofGrid.Roofed(c);
				Building building = innerArray[num];
				bool flag2 = building != null && (building.def.fillPercent >= 1f || building.def.passability == Traversability.Impassable);
				bool flag3 = building is Building_Door;
				if (mode == TurbojetMode.HoverMoving)
				{
					if (flag2 && !flag3)
					{
						result[num] = value;
					}
					else
					{
						result[num] = value2;
					}
				}
				else if (mode == TurbojetMode.HoverAlways)
				{
					if (flag)
					{
						result[num] = value;
					}
					else
					{
						result[num] = value2;
					}
				}
				else
				{
					result[num] = 0;
				}
				num++;
			}
		}
		return result;
	}
}
