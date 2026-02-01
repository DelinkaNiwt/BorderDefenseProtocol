using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class PawnRenderNode_Annihilator : PawnRenderNode
	{
		public PawnRenderNode_Annihilator(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
			: base(pawn, props, tree)
		{
		}

		public override GraphicMeshSet MeshSetFor(Pawn pawn)
		{
			Graphic graphic = GraphicFor(pawn);
			if (graphic != null)
			{
				return MeshPool.GetMeshSetForSize(graphic.drawSize.x, graphic.drawSize.y);
			}
			return null;
		}

		public override Graphic GraphicFor(Pawn pawn)
		{
			Annihilator annihilator = pawn as Annihilator;
			Graphic graphic;
			Color color = Color.white;
			if (pawn.Faction != Faction.OfPlayer)
            {
				if (annihilator.Damaged)
				{
					graphic = pawn.kindDef.lifeStages[1].bodyGraphicData.Graphic;
				}
				else
				{
					graphic = pawn.kindDef.lifeStages[0].bodyGraphicData.Graphic;
				}
			}
            else
            {
				if (annihilator.Damaged)
				{
					graphic = pawn.kindDef.lifeStages[3].bodyGraphicData.Graphic;
				}
				else
				{
					graphic = pawn.kindDef.lifeStages[2].bodyGraphicData.Graphic;
				}
			}
			graphic.color = color;
			graphic.colorTwo = color;
			return graphic;
		}
	}
}
