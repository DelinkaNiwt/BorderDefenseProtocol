using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace AncotLibrary;

public class Gizmo_SetFuelLevelAerocraftNotGrounded : Gizmo_Slider
{
	private CompRefuelable refuelable;

	private static bool draggingBar;

	protected override float Target
	{
		get
		{
			return refuelable.TargetFuelLevel / refuelable.Props.fuelCapacity;
		}
		set
		{
			refuelable.TargetFuelLevel = value * refuelable.Props.fuelCapacity;
		}
	}

	protected override float ValuePercent => refuelable.FuelPercentOfMax;

	protected override string Title => refuelable.Props.FuelGizmoLabel;

	protected override bool IsDraggable => refuelable.Props.targetFuelLevelConfigurable;

	protected override string BarLabel => refuelable.Fuel.ToStringDecimalIfSmall() + " / " + refuelable.Props.fuelCapacity.ToStringDecimalIfSmall();

	protected override bool DraggingBar
	{
		get
		{
			return draggingBar;
		}
		set
		{
			draggingBar = value;
		}
	}

	public Gizmo_SetFuelLevelAerocraftNotGrounded(CompRefuelable refuelable)
	{
		this.refuelable = refuelable;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		if (SteamDeck.IsSteamDeckInNonKeyboardMode)
		{
			return base.GizmoOnGUI(topLeft, maxWidth, parms);
		}
		return base.GizmoOnGUI(topLeft, maxWidth, parms);
	}

	protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
	{
		if (refuelable.Props.showAllowAutoRefuelToggle)
		{
			headerRect.xMax -= 24f;
			Rect rect = new Rect(headerRect.xMax, headerRect.y, 24f, 24f);
			GUI.DrawTexture(rect, (Texture)refuelable.Props.FuelIcon);
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
				mouseOverElement = true;
			}
		}
		base.DrawHeader(headerRect, ref mouseOverElement);
	}

	protected override string GetTooltip()
	{
		return "";
	}
}
