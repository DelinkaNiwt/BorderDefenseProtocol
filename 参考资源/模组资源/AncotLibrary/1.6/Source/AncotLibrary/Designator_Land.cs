using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class Designator_Land : Designator
{
	private readonly ThingDef thingDef;

	private readonly Thing thing;

	private Rot4 placingRot = Rot4.North;

	private readonly Action<IntVec3, Rot4> action;

	public Designator_Land(Texture icon, Thing thing, ThingDef def, Action<IntVec3, Rot4> action)
	{
		thingDef = def;
		this.thing = thing;
		this.action = action;
		defaultLabel = "Land " + def.label;
		defaultDesc = "Select a position to land " + def.label;
		base.icon = icon;
		useMouseIcon = true;
	}

	public override AcceptanceReport CanDesignateCell(IntVec3 c)
	{
		if (!c.InBounds(base.Map))
		{
			return false;
		}
		return GenConstruct.CanPlaceBlueprintAt(thingDef, c, placingRot, base.Map);
	}

	public override void DesignateSingleCell(IntVec3 c)
	{
		action?.Invoke(c, placingRot);
		Find.DesignatorManager.Deselect();
	}

	public override void SelectedUpdate()
	{
		base.SelectedUpdate();
		IntVec3 intVec = UI.MouseCell();
		if (intVec.InBounds(base.Map))
		{
			GhostDrawer.DrawGhostThing(intVec, placingRot, thingDef, null, Color.white, AltitudeLayer.Blueprint);
		}
	}

	public override void SelectedProcessInput(Event ev)
	{
		base.SelectedProcessInput(ev);
		if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
		{
			placingRot.Rotate(RotationDirection.Clockwise);
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		}
		if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
		{
			placingRot.Rotate(RotationDirection.Counterclockwise);
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		}
	}
}
