using Verse;

namespace NCLWorm;

public class NCLCallTool_ReStart : NCLCallTool
{
	public override void Action()
	{
		Pawn usedBy = windows.usedBy;
		windows.Close();
		Find.WindowStack.Add(new Window_NCLcall(usedBy, NCLCall));
	}
}
