using Verse;

namespace NCLWorm;

public abstract class NCLCallTool_Bool : NCLCallTool
{
	public string TextLong;

	[MustTranslate]
	public string TextYes = "NCLYes";

	[MustTranslate]
	public string TextNo = "NCLNo";

	[MustTranslate]
	public string letter = "letter";

	[MustTranslate]
	public string letterText = "letterText";

	public override void Action()
	{
		Pawn usedBy = windows.usedBy;
		windows.Close();
		Find.WindowStack.Add(new Window_NCLcall(usedBy, NCLCall, TextLong, useBaseFunction: false, this));
	}

	public virtual void SecAction()
	{
		windows.Close();
	}

	public virtual void TriAction()
	{
		Pawn usedBy = windows.usedBy;
		windows.Close();
		Find.WindowStack.Add(new Window_NCLcall(usedBy, NCLCall, null, useBaseFunction: true, this));
	}
}
