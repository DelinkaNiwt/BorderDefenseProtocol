using Verse;

namespace AncotLibrary;

public class ThingWithComps_WarmingGraphic : ThingWithComps
{
	public CompEquippable CompEquippable => this.TryGetComp<CompEquippable>();

	public ExtraGraphicData_Extension Props => def.GetModExtension<ExtraGraphicData_Extension>();

	public override Graphic Graphic
	{
		get
		{
			if (CompEquippable.verbTracker.PrimaryVerb.WarmingUp)
			{
				return Props.graphicData.Graphic;
			}
			return base.Graphic;
		}
	}
}
