using Verse;

namespace AlienRace;

public class Graphic_Multi_RotationFromData : Graphic_Multi
{
	public bool? westFlipped;

	public override bool ShouldDrawRotated => data?.drawRotated ?? false;

	public override bool WestFlipped => westFlipped ?? base.WestFlipped;
}
