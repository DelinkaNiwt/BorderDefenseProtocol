using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class CompPowerPlantSunLightGatheringTower : CompPowerPlant
{
	private const float NightPower = 0f;

	private static readonly Vector2 BarSize = new Vector2(2.3f, 0.14f);

	private static readonly Material PowerPlantSolarBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));

	private static readonly Material PowerPlantSolarBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));

	public CompAffectedByFacilities compAffectedByFacilities => parent.TryGetComp<CompAffectedByFacilities>();

	public int FacilitiesNum => compAffectedByFacilities.LinkedFacilitiesListForReading.Count();

	public float PowerConsumption
	{
		get
		{
			if (MiliraDefOf.Milira_EffectivePhotoelectricTransform.IsFinished)
			{
				return base.Props.PowerConsumption - 25f;
			}
			return base.Props.PowerConsumption;
		}
	}

	protected override float DesiredPowerOutput => Mathf.Lerp(0f, 0f - PowerConsumption, parent.Map.skyManager.CurSkyGlow) * (float)FacilitiesNum;

	public override void PostDrawExtraSelectionOverlays()
	{
		base.PostDrawExtraSelectionOverlays();
		GenDraw.DrawRadiusRing(parent.Position, 3.4f, Color.white);
	}
}
