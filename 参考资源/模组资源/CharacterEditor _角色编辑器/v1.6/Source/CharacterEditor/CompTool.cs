using Verse;

namespace CharacterEditor;

internal static class CompTool
{
	internal static HediffComp GetHediffComp(Pawn p, bool conditionToPass, string typeEndsWith)
	{
		if (p == null || p.AllComps.NullOrEmpty() || !conditionToPass)
		{
			return null;
		}
		foreach (HediffComp allComp in p.health.hediffSet.GetAllComps())
		{
			if (allComp.GetType().ToString().EndsWith(typeEndsWith))
			{
				return allComp;
			}
		}
		return null;
	}
}
