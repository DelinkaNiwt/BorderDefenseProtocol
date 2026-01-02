using Verse;

namespace AncotLibrary;

public static class AncotUtility_Dialog
{
	public static void CloseDialog()
	{
		Find.WindowStack.TryRemove(typeof(Dialog_NodeTree));
	}
}
