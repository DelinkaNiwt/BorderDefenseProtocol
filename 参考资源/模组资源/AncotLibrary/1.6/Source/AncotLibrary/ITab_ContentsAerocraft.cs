using RimWorld;
using Verse;

namespace AncotLibrary;

public class ITab_ContentsAerocraft : ITab_ContentsTransporter
{
	public Building_Aerocraft Aerocraft => base.SelThing as Building_Aerocraft;

	public override bool IsVisible
	{
		get
		{
			if (Aerocraft != null && Aerocraft.FlightState != AerocraftState.Grounded)
			{
				return false;
			}
			return base.IsVisible;
		}
	}

	public ITab_ContentsAerocraft()
	{
		labelKey = "Ancot.TabAerocraftContents".Translate();
		containedItemsKey = "Ancot.ContainedItems_Aerocraft".Translate();
	}

	protected override void OnDropThing(Thing t, int count)
	{
		if (Aerocraft == null || Aerocraft.FlightState == AerocraftState.Grounded)
		{
			base.OnDropThing(t, count);
		}
	}
}
