using UnityEngine;
using Verse;

namespace NCL;

[StaticConstructorOnStartup]
public class Command_AutoReversion_Action : Command
{
	public CompFormChange compFormChange;

	public TransformData transformData;

	private static readonly Texture2D cooldownBarTex = SolidColorMaterials.NewSolidColorTexture(new Color32(9, 203, 4, 64));

	public override void ProcessInput(Event ev)
	{
		base.ProcessInput(ev);
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
		float num = 1f - Mathf.InverseLerp(compFormChange.Props.revertData.revertAfterTicks, 0f, compFormChange.revertTickCounter);
		Widgets.FillableBar(rect, Mathf.Clamp01(num), cooldownBarTex, null, doBorder: false);
		if (compFormChange.cooldownNow > 0)
		{
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(rect, num.ToStringPercent("F0"));
			Text.Anchor = TextAnchor.UpperLeft;
		}
		if (result.State == GizmoState.Interacted)
		{
			return result;
		}
		return new GizmoResult(result.State);
	}
}
