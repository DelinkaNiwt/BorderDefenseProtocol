using RimWorld;
using Verse;

namespace AncotLibrary;

public class StatWorker_TickToSecond : StatWorker
{
	public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
	{
		val /= 60f;
		return base.ValueToString(val, finalized, numberSense);
	}
}
