using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class DrawWeaponExtraEquipped
{
	public static void DrawExtraMatStatic(Thing eq, Vector3 drawLoc, float aimAngle)
	{
		string texPath = eq.def.graphicData.texPath;
		try
		{
			float num = aimAngle - 90f;
			Mesh mesh;
			if (aimAngle > 20f && aimAngle < 160f)
			{
				mesh = MeshPool.plane10;
				num += eq.def.equippedAngleOffset;
			}
			else if (aimAngle > 200f && aimAngle < 340f)
			{
				mesh = MeshPool.plane10Flip;
				num -= 180f;
				num -= eq.def.equippedAngleOffset;
			}
			else
			{
				mesh = MeshPool.plane10;
				num += eq.def.equippedAngleOffset;
			}
			num %= 360f;
			CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
			if (compEquippable != null)
			{
				EquipmentUtility.Recoil(eq.def, EquipmentUtility.GetRecoilVerb(compEquippable.AllVerbs), out var drawOffset, out var angleOffset, aimAngle);
				drawLoc += drawOffset;
				num += angleOffset;
			}
			Material material = ((!(eq.Graphic is Graphic_StackCount graphic_StackCount)) ? eq.Graphic.MatSingleFor(eq) : graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingleFor(eq));
			Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(eq.Graphic.drawSize.x, 0f, eq.Graphic.drawSize.y), pos: drawLoc, q: Quaternion.AngleAxis(num, Vector3.up));
			Graphics.DrawMesh(mesh, matrix, material, 0);
			eq.TryGetComp<Comp_WeaponRenderDynamic>()?.PostDrawExtraGlower(mesh, matrix);
			eq.TryGetComp<Comp_WeaponRenderStatic>()?.PostDrawExtraGlower(mesh, matrix);
		}
		catch (Exception)
		{
		}
	}
}
