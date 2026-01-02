using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class Command_Land : Command
{
	public ThingDef thingDef;

	public Thing thing;

	public Action<IntVec3, Rot4> action;

	public Action<IntVec3> action_RightClickMap;

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		Event current = Event.current;
		if ((int)current.type == 0 && current.button == 1)
		{
			Vector2 mousePosition = current.mousePosition;
			if (Find.CurrentMap != null)
			{
				IntVec3 intVec = UI.MouseCell();
				if (!intVec.InNoBuildEdgeArea(Find.CurrentMap))
				{
					action_RightClickMap?.Invoke(intVec);
					current.Use();
				}
			}
		}
		return base.GizmoOnGUI(topLeft, maxWidth, parms);
	}

	public override void ProcessInput(Event ev)
	{
		base.ProcessInput(ev);
		SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
		Find.DesignatorManager.Select(new Designator_Land(icon, thing, thingDef, action));
	}
}
