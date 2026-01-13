using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace ATFieldGenerator;

public class ATFieldManager : MapComponent
{
	public List<Comp_AbsoluteTerrorField> activeFields = new List<Comp_AbsoluteTerrorField>();

	public ATFieldManager(Map map)
		: base(map)
	{
	}

	public static ATFieldManager Get(Map map)
	{
		return map.GetComponent<ATFieldManager>();
	}

	public void Register(Comp_AbsoluteTerrorField field)
	{
		if (!activeFields.Contains(field))
		{
			activeFields.Add(field);
		}
	}

	public void Deregister(Comp_AbsoluteTerrorField field)
	{
		if (activeFields.Contains(field))
		{
			activeFields.Remove(field);
		}
	}

	public bool HasActiveSolarFlareShield()
	{
		if (activeFields == null || activeFields.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < activeFields.Count; i++)
		{
			if (activeFields[i].Active && activeFields[i].BlockSolarFlare)
			{
				return true;
			}
		}
		return false;
	}

	public void DrawAllFields()
	{
		if (activeFields == null)
		{
			return;
		}
		World world = Find.World;
		if (world != null && world.renderer?.wantedMode == WorldRenderMode.None && Find.CurrentMap == map)
		{
			for (int num = activeFields.Count - 1; num >= 0; num--)
			{
				activeFields[num].DrawShield();
			}
		}
	}
}
