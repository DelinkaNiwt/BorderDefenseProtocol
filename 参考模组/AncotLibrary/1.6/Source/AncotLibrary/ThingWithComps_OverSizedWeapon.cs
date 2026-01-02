using Verse;

namespace AncotLibrary;

public class ThingWithComps_OverSizedWeapon : ThingWithComps
{
	public bool drawExtraTexture = false;

	public ExtraGraphicData_Extension Props => def.GetModExtension<ExtraGraphicData_Extension>();

	public override Graphic Graphic
	{
		get
		{
			if (Props != null && drawExtraTexture)
			{
				return Props.graphicData.Graphic;
			}
			return base.Graphic;
		}
	}

	public override void Notify_Equipped(Pawn pawn)
	{
		base.Notify_Equipped(pawn);
		drawExtraTexture = true;
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		base.Notify_Unequipped(pawn);
		drawExtraTexture = false;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref drawExtraTexture, "drawExtraTexture", defaultValue: false);
	}
}
