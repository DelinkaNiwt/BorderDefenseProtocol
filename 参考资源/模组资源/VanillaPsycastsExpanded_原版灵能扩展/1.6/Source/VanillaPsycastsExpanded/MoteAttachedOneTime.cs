using VanillaPsycastsExpanded.Graphics;
using Verse;

namespace VanillaPsycastsExpanded;

public class MoteAttachedOneTime : MoteAttached, IAnimationOneTime
{
	public bool shouldDestroy;

	public int currentIndex;

	public int CurrentIndex()
	{
		return currentIndex;
	}

	protected override void Tick()
	{
		base.Tick();
		if (this.IsHashIntervalTick((Graphic.data as GraphicData_Animated).ticksPerFrame) && currentIndex < (Graphic as Graphic_Animated).SubGraphicCount)
		{
			currentIndex++;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref currentIndex, "currentIndex", 0);
	}
}
