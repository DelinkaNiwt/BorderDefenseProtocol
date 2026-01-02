using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class Gizmo_SwitchWeapon_Hediff : Command_Action
{
	public HediffComp_AlternateWeapon comp;

	public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
	{
		base.DrawIcon(rect, buttonMat, parms);
		Rect rect2 = new Rect(rect.x + 50f, rect.y + 50f, 25f, 25f);
		GUI.DrawTexture(rect2, (Texture)AncotLibraryIcon.SwitchA);
	}
}
