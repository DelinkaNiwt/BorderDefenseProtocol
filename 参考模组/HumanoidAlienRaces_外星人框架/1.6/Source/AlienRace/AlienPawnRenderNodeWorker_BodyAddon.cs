using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

public class AlienPawnRenderNodeWorker_BodyAddon : PawnRenderNodeWorker
{
	public static AlienPawnRenderNodeProperties_BodyAddon PropsFromNode(PawnRenderNode node)
	{
		return ((AlienPawnRenderNode_BodyAddon)node).props;
	}

	public static AlienPartGenerator.BodyAddon AddonFromNode(PawnRenderNode node)
	{
		return ((AlienPawnRenderNode_BodyAddon)node).props.addon;
	}

	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		AlienPartGenerator.BodyAddon addonFromNode = AddonFromNode(node);
		if (addonFromNode.CanDrawAddon(parms.pawn))
		{
			if (!addonFromNode.useSkipFlags.NullOrEmpty())
			{
				return !addonFromNode.useSkipFlags.Any((RenderSkipFlagDef rsfd) => parms.skipFlags.HasFlag(rsfd));
			}
			return true;
		}
		return false;
	}

	protected override Material GetMaterial(PawnRenderNode node, PawnDrawParms parms)
	{
		if (parms.flipHead && AddonFromNode(node).alignWithHead)
		{
			parms.facing = parms.facing.Opposite;
		}
		return base.GetMaterial(node, parms);
	}

	public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
	{
		if (parms.flipHead && AddonFromNode(node).alignWithHead)
		{
			parms.facing = parms.facing.Opposite;
		}
		AlienPawnRenderNodeProperties_BodyAddon props = PropsFromNode(node);
		AlienPartGenerator.BodyAddon ba = props.addon;
		ThingDef_AlienRace alienProps = (ThingDef_AlienRace)parms.pawn.def;
		if (props.addonIndex >= alienProps.alienRace.generalSettings.alienPartGenerator.bodyAddons.Count)
		{
			ba.defaultOffsets = alienProps.alienRace.generalSettings.alienPartGenerator.offsetDefaultsDictionary[ba.defaultOffset].offsets;
		}
		AlienPartGenerator.DirectionalOffset offsets = ((parms.pawn.gender == Gender.Female) ? ba.femaleOffsets : ba.offsets) ?? ba.offsets;
		Vector3 offsetVector = (ba.defaultOffsets.GetOffset(parms.facing)?.GetOffset(parms.Portrait, parms.pawn.story?.bodyType ?? BodyTypeDefOf.Male, parms.pawn.story?.headType ?? HeadTypeDefOf.Skull) ?? Vector3.zero) + (offsets.GetOffset(parms.facing)?.GetOffset(parms.Portrait, parms.pawn.story?.bodyType ?? BodyTypeDefOf.Male, parms.pawn.story?.headType ?? HeadTypeDefOf.Skull) ?? Vector3.zero);
		offsetVector.y = (ba.inFrontOfBody ? (0.3f + offsetVector.y) : (-0.3f - offsetVector.y));
		if (parms.facing == Rot4.North && ba.layerInvert)
		{
			offsetVector.y = 0f - offsetVector.y;
		}
		if (parms.facing == Rot4.East)
		{
			offsetVector.x = 0f - offsetVector.x;
		}
		return base.OffsetFor(node, parms, out pivot) + offsetVector;
	}

	public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
	{
		AlienPawnRenderNodeProperties_BodyAddon props = PropsFromNode(node);
		AlienPartGenerator.BodyAddon ba = props.addon;
		Vector2 scale = ((parms.Portrait && ba.drawSizePortrait != Vector2.zero) ? ba.drawSizePortrait : ba.drawSize) * ((!ba.scaleWithPawnDrawsize) ? (Vector2.one * 1.5f) : (ba.alignWithHead ? ((parms.Portrait ? props.alienComp.customPortraitHeadDrawSize : props.alienComp.customHeadDrawSize) * HumanlikeMeshPoolUtility.HumanlikeHeadWidthForPawn(parms.pawn)) : ((parms.Portrait ? props.alienComp.customPortraitDrawSize : props.alienComp.customDrawSize) * HumanlikeMeshPoolUtility.HumanlikeBodyWidthForPawn(parms.pawn))));
		return new Vector3(scale.x, 1f, scale.y);
	}
}
