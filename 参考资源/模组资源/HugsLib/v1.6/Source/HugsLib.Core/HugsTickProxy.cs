using Verse;

namespace HugsLib.Core;

/// <summary>
/// Forwards ticks to the controller. Will not be saved and is never spawned.
/// </summary>
public class HugsTickProxy : Thing
{
	public bool CreatedByController { get; internal set; }

	public HugsTickProxy()
	{
		def = new ThingDef
		{
			tickerType = TickerType.Normal,
			isSaveable = false
		};
	}

	protected override void Tick()
	{
		if (CreatedByController)
		{
			HugsLibController.Instance.OnTick();
		}
	}
}
