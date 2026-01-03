using UnityEngine;
using Verse;

namespace Milira;

public class PawnRenderNode_MilianHairBase : PawnRenderNode
{
	public new PawnRenderNodeProperties_MilianHair Props => (PawnRenderNodeProperties_MilianHair)props;

	public PawnRenderNode_MilianHairBase(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override Color ColorFor(Pawn pawn)
	{
		Color color = Color.white;
		if (pawn.Faction != null && MiliraRaceSettings.MiliraRace_ModSetting_MilianHairColor)
		{
			CompMilianHairSwitch compMilianHairSwitch = pawn.TryGetComp<CompMilianHairSwitch>();
			if (pawn.Faction.IsPlayer && MiliraRaceSettings.MiliraRace_ModSetting_MilianHairColor_PlayerColorOverride && compMilianHairSwitch.colorOverride != default(Color))
			{
				return compMilianHairSwitch.colorOverride;
			}
			_ = pawn.Faction.AllegianceColor;
			if (true)
			{
				color = pawn.Faction.AllegianceColor;
				if (MiliraRaceSettings.MiliraRace_ModSetting_MilianHairColorOffset)
				{
					Rand.PushState(pawn.thingIDNumber);
					PawnRenderNodeProperties_MilianHair pawnRenderNodeProperties_MilianHair = Props;
					Color.RGBToHSV(color, out var H, out var S, out var V);
					float v = V + Rand.Range(0f - pawnRenderNodeProperties_MilianHair.hairColorOffset, pawnRenderNodeProperties_MilianHair.hairColorOffset);
					color = Color.HSVToRGB(H, S, v);
					Rand.PopState();
				}
			}
		}
		return color;
	}

	public override GraphicMeshSet MeshSetFor(Pawn pawn)
	{
		Vector2 hairMeshSize = MiliraDefOf.MilianHeadN1.hairMeshSize;
		return MeshPool.GetMeshSetForSize(hairMeshSize.x, hairMeshSize.y);
	}
}
