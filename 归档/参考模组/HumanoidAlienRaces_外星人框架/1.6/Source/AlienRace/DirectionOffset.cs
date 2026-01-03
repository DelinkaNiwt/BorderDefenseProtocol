using UnityEngine;
using Verse;

namespace AlienRace;

public class DirectionOffset
{
	public Vector2 north = Vector2.zero;

	public Vector2 west = Vector2.zero;

	public Vector2 east = Vector2.zero;

	public Vector2 south = Vector2.zero;

	public Vector2 GetOffset(Rot4 rot)
	{
		if (!(rot == Rot4.North))
		{
			if (!(rot == Rot4.East))
			{
				if (!(rot == Rot4.West))
				{
					return south;
				}
				return west;
			}
			return east;
		}
		return north;
	}
}
