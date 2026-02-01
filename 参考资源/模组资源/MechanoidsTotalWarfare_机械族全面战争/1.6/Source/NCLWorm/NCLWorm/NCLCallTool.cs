using Verse;

namespace NCLWorm;

public abstract class NCLCallTool
{
	[Unsaved(false)]
	public string label = "DefaultLabel";

	public NCLCallDef NCLCall;

	public string FirstUseMess;

	public Window_NCLcall windows;

	public GraphicData GraphicData;

	public bool FirstUseToMess => FirstUseMess.NullOrEmpty();

	public virtual void Action()
	{
		windows.Close();
	}

	public virtual AcceptanceReport Canuse()
	{
		return true;
	}

	public virtual bool NoCanSee()
	{
		return false;
	}
}
