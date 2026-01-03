using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class DroneGizmo : Gizmo
{
	private CompDrone powerCell;

	private HashSet<CompDrone> groupedComps;

	private static readonly Texture2D BarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));

	private static readonly Texture2D BarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));

	private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

	private static readonly Texture2D DragBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

	private static bool draggingBar;

	public DroneGizmo(CompDrone carrier)
	{
		powerCell = carrier;
	}

	public override float GetWidth(float maxWidth)
	{
		return 160f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(10f);
		Widgets.DrawWindowBackground(rect);
		string text = (powerCell.Props.labelOverride.NullOrEmpty() ? ((string)"MechPowerCell".Translate()) : powerCell.Props.labelOverride);
		Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, Text.CalcHeight(text, rect2.width) + 8f);
		Text.Font = GameFont.Small;
		Widgets.Label(rect3, text);
		Rect rect4 = new Rect(rect2.x, rect3.yMax, rect2.width, rect2.height - rect3.height);
		DraggableBarForGroup(rect4);
		Text.Anchor = TextAnchor.MiddleCenter;
		TaggedString taggedString = Mathf.CeilToInt((float)powerCell.PowerTicksLeft / 2500f).ToString() + "LetterHour".Translate();
		Widgets.Label(rect4, taggedString);
		Text.Anchor = TextAnchor.UpperLeft;
		string tooltip;
		if (!powerCell.Props.tooltipOverride.NullOrEmpty())
		{
			tooltip = powerCell.Props.tooltipOverride;
		}
		else
		{
			tooltip = "Ancot.DroneTip_Power".Translate(taggedString, powerCell.PercentRecharge.ToStringPercentEmptyZero());
		}
		TooltipHandler.TipRegion(rect4, () => tooltip, Gen.HashCombineInt(powerCell.GetHashCode(), 34242419));
		Rect rect5 = new Rect(rect2.x + rect2.width - 50f, rect2.y, 24f, 24f);
		if (Widgets.ButtonImageFitted(rect5, powerCell.autoRepair ? AncotLibraryIcon.AutoRepair_On : AncotLibraryIcon.AutoRepair_Off))
		{
			powerCell.autoRepair = !powerCell.autoRepair;
			if (!groupedComps.NullOrEmpty())
			{
				foreach (CompDrone groupedComp in groupedComps)
				{
					groupedComp.autoRepair = powerCell.autoRepair;
				}
			}
		}
		Rect rect6 = new Rect(rect2.x + rect2.width - 20f, rect2.y, 24f, 24f);
		if (Widgets.ButtonImageFitted(rect6, powerCell.workMode?.uiIcon ?? AncotLibraryIcon.SwitchA))
		{
			Find.WindowStack.Add(new FloatMenu(GetWorkModeOptions(powerCell, groupedComps).ToList()));
		}
		Widgets.DrawHighlightIfMouseover(rect5);
		TooltipHandler.TipRegion(rect5, "Ancot.DroneTip_AutoRepair".Translate());
		TooltipHandler.TipRegion(rect6, "Ancot.DroneTip_WorkMode".Translate());
		Widgets.DrawHighlightIfMouseover(rect6);
		return new GizmoResult(GizmoState.Clear);
	}

	private void DraggableBarForGroup(Rect rect)
	{
		float percentRecharge = powerCell.PercentRecharge;
		Widgets.DraggableBar(rect, BarTex, BarHighlightTex, EmptyBarTex, DragBarTex, ref draggingBar, powerCell.PercentFull, ref powerCell.PercentRecharge, null, 50, 0.1f, 0.9f);
		if (powerCell.PercentRecharge == percentRecharge || groupedComps.NullOrEmpty())
		{
			return;
		}
		foreach (CompDrone groupedComp in groupedComps)
		{
			if (groupedComp != null && groupedComp != powerCell)
			{
				groupedComp.PercentRecharge = powerCell.PercentRecharge;
			}
		}
	}

	public static IEnumerable<FloatMenuOption> GetWorkModeOptions(CompDrone powerCell, HashSet<CompDrone> groupedComps = null)
	{
		foreach (DroneWorkModeDef wm in DefDatabase<DroneWorkModeDef>.AllDefsListForReading.OrderBy((DroneWorkModeDef d) => d.uiOrder))
		{
			yield return new FloatMenuOption(wm.LabelCap, delegate
			{
				powerCell.workMode = wm;
				powerCell.Mech.jobs.StopAll();
				if (!groupedComps.NullOrEmpty())
				{
					foreach (CompDrone groupedComp in groupedComps)
					{
						groupedComp.workMode = wm;
						groupedComp.Mech.jobs.StopAll();
					}
				}
			}, wm.uiIcon, Color.white)
			{
				tooltip = new TipSignal(wm.description, wm.index ^ 0xDFE8661)
			};
		}
	}

	public override bool GroupsWith(Gizmo other)
	{
		if (other is DroneGizmo)
		{
			return true;
		}
		return false;
	}

	public override void MergeWith(Gizmo other)
	{
		base.MergeWith(other);
		if (other is DroneGizmo droneGizmo)
		{
			if (groupedComps == null)
			{
				groupedComps = new HashSet<CompDrone>();
			}
			groupedComps.Add(droneGizmo.powerCell);
			if (droneGizmo.groupedComps != null)
			{
				groupedComps.AddRange(droneGizmo.groupedComps);
			}
		}
	}
}
