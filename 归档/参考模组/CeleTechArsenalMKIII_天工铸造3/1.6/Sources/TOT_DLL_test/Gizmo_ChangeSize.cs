using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Gizmo_ChangeSize : Gizmo
{
	public Gizmo_ChangeSize()
	{
		Order = -90f;
	}

	public override float GetWidth(float maxWidth)
	{
		return 140f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms p)
	{
		return new GizmoResult(GizmoState.Clear);
	}
}
