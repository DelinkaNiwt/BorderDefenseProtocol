using Verse;

namespace ATFieldGenerator;

public class Building_ATFieldGenerator : Building
{
	public Comp_AbsoluteTerrorField comp_ATField;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		comp_ATField = GetComp<Comp_AbsoluteTerrorField>();
	}
}
