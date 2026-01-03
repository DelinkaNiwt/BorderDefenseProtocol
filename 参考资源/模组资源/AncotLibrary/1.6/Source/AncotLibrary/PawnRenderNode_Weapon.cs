using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnRenderNode_Weapon : PawnRenderNode
{
	public ThingWithComps weapon;

	public PawnRenderNode_Weapon(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
		PawnRenderNodeProperties_Weapon pawnRenderNodeProperties_Weapon = props as PawnRenderNodeProperties_Weapon;
	}

	protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
	{
		PawnRenderNodeProperties_Weapon Props = props as PawnRenderNodeProperties_Weapon;
		yield return GraphicDatabase.Get<Graphic_Multi>(Props.texPath_Undrafted, ShaderFor(pawn), Vector2.one, ColorFor(pawn));
		yield return GraphicDatabase.Get<Graphic_Multi>(Props.texPath, ShaderFor(pawn), Vector2.one, ColorFor(pawn));
	}

	public override Color ColorFor(Pawn pawn)
	{
		Color result = Color.white;
		PawnRenderNodeProperties_Weapon pawnRenderNodeProperties_Weapon = props as PawnRenderNodeProperties_Weapon;
		if (weapon.def.MadeFromStuff && pawnRenderNodeProperties_Weapon.colored)
		{
			result = weapon.Stuff.stuffProps.color;
		}
		return result;
	}
}
