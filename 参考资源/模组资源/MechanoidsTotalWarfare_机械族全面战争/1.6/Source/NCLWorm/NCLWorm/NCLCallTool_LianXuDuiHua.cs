using System.Collections.Generic;
using Verse;

namespace NCLWorm;

public class NCLCallTool_LianXuDuiHua : NCLCallTool
{
	public string UseHello;

	public List<NCLCallTool> NextCallTools;

	public override void Action()
	{
		Pawn usedBy = windows.usedBy;
		windows.Close();
		Find.WindowStack.Add(new Window_NCLcall(usedBy, NCLCall, UseHello, useBaseFunction: false, this));
	}
}
