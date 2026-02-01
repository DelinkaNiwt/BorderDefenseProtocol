using Verse;

namespace NCL;

public static class MoteExtensions
{
	public static void LinkMote(this Mote mote, Thing target)
	{
		if (mote is MoteAttached attachedMote)
		{
			attachedMote.Attach(target);
		}
		else
		{
			mote.exactPosition = target.DrawPos;
		}
	}
}
