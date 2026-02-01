using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class PawnRenderNode_Annihilator_Part : PawnRenderNode
	{
		public new PawnRenderNodeProperties_AnnihilatorPart Props => (PawnRenderNodeProperties_AnnihilatorPart)props;

		public PawnRenderNode_Annihilator_Part(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
			: base(pawn, props, tree)
		{
		}

		public override GraphicMeshSet MeshSetFor(Pawn pawn)
		{
			Graphic graphic = GraphicFor(pawn);
			if (graphic != null)
			{
				if (props.overrideMeshSize.HasValue)
				{
					return MeshPool.GetMeshSetForSize(props.overrideMeshSize.Value.x, props.overrideMeshSize.Value.y);
				}
				return MeshPool.GetMeshSetForSize(graphic.drawSize.x, graphic.drawSize.y);
			}
			return null;
		}

		public override Graphic GraphicFor(Pawn pawn)
		{
			Annihilator annihilator = pawn as Annihilator;
			Graphic graphic;
			string path = TexPathFor(pawn);
			Color color = Color.white;
			if (annihilator.Faction != Faction.OfPlayer)
            {
				path += "_Ancient";
            }
			if (!annihilator.health.hediffSet.GetNotMissingParts().Any(r => r.groups.Contains(Props.linkedWithGroup)))
			{
				graphic = GraphicDatabase.Get<Graphic_Single>(path + "_Damaged");
			}
			else
			{
				graphic = GraphicDatabase.Get<Graphic_Single>(path + "_Normal");
			}
			graphic.color = color;
			graphic.colorTwo = color;
			return graphic;
		}
	}
}
