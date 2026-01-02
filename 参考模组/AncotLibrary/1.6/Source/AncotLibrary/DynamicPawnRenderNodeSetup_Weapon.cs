using System;
using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class DynamicPawnRenderNodeSetup_Weapon : DynamicPawnRenderNodeSetup
{
	public override bool HumanlikeOnly => false;

	public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn, PawnRenderTree tree)
	{
		ThingDef_Custom def = pawn.equipment?.Primary?.def as ThingDef_Custom;
		if (def?.RenderNodeProperties == null)
		{
			yield break;
		}
		foreach (PawnRenderNodeProperties renderNodeProperty in def.RenderNodeProperties)
		{
			if (tree.ShouldAddNodeToTree(renderNodeProperty))
			{
				PawnRenderNode_Weapon pawnRenderNode = (PawnRenderNode_Weapon)Activator.CreateInstance(renderNodeProperty.nodeClass, pawn, renderNodeProperty, tree);
				pawnRenderNode.weapon = pawn.equipment.Primary;
				yield return (node: pawnRenderNode, parent: null);
			}
		}
	}
}
