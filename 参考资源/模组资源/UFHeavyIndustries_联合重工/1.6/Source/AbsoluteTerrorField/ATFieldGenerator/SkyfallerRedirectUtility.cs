using RimWorld;
using UnityEngine;
using Verse;

namespace ATFieldGenerator;

public static class SkyfallerRedirectUtility
{
	public static bool RedirectSkyfallerPos(ref IntVec3 pos, Map map, Faction incomingFaction, ThingDef skyfallerDef)
	{
		if (map == null)
		{
			return true;
		}
		ATFieldManager aTFieldManager = ATFieldManager.Get(map);
		if (aTFieldManager == null || aTFieldManager.activeFields.Count == 0)
		{
			return true;
		}
		IntVec3 root = pos;
		bool flag = false;
		for (int i = 0; i < 5; i++)
		{
			bool flag2 = false;
			for (int j = 0; j < aTFieldManager.activeFields.Count; j++)
			{
				Comp_AbsoluteTerrorField comp_AbsoluteTerrorField = aTFieldManager.activeFields[j];
				if (!comp_AbsoluteTerrorField.Active || !root.InHorDistOf(comp_AbsoluteTerrorField.parent.Position, comp_AbsoluteTerrorField.radius))
				{
					continue;
				}
				bool flag3 = false;
				if (comp_AbsoluteTerrorField.redirectSkyfallers)
				{
					flag3 = true;
				}
				else
				{
					bool flag4 = false;
					Faction faction = comp_AbsoluteTerrorField.parent.Faction;
					if (faction != null)
					{
						if (incomingFaction == null)
						{
							flag4 = skyfallerDef == null || ((!(skyfallerDef.defName == "DropPodIncoming") && !(skyfallerDef.defName == "TransportPodIncoming")) ? true : false);
						}
						else if (incomingFaction.HostileTo(faction))
						{
							flag4 = true;
						}
					}
					else
					{
						flag4 = true;
					}
					if (flag4)
					{
						flag3 = true;
					}
				}
				if (flag3)
				{
					comp_AbsoluteTerrorField.SpawnInterceptEffect(root.ToVector3Shifted(), 50f);
					Vector3 vector = comp_AbsoluteTerrorField.parent.Position.ToVector3Shifted();
					Vector3 vector2 = (root.ToVector3Shifted() - vector).normalized;
					if (vector2 == Vector3.zero)
					{
						vector2 = Vector3.right;
					}
					root = (vector + vector2 * (comp_AbsoluteTerrorField.radius + 5f)).ToIntVec3().ClampInsideMap(map);
					flag2 = true;
					flag = true;
					break;
				}
			}
			if (!flag2)
			{
				break;
			}
		}
		if (flag)
		{
			pos = CellFinder.StandableCellNear(root, map, 5f);
			return true;
		}
		return true;
	}
}
