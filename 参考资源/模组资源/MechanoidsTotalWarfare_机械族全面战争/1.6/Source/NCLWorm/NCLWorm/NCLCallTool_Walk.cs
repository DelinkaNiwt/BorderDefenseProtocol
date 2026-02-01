using System.Collections.Generic;
using Verse;

namespace NCLWorm;

public class NCLCallTool_Walk : NCLCallTool
{
	public List<string> Randomstring;

	public override void Action()
	{
		Pawn usedBy = windows.usedBy;
		windows.Close();
		Find.WindowStack.Add(new Window_NCLcall(usedBy, NCLCall, Randomstring.RandomElement(), useBaseFunction: true, this));
	}
}
