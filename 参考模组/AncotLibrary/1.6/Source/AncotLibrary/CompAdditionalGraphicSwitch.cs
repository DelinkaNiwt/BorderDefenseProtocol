using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompAdditionalGraphicSwitch : ThingComp
{
	private string graphicPath;

	private string graphicLabel = "";

	private Graphic graphic;

	private CompProperties_AdditionalGraphicSwitch Props => (CompProperties_AdditionalGraphicSwitch)props;

	private Graphic Graphic => graphic ?? (graphic = GetGraphic());

	private Graphic GetGraphic()
	{
		graphic = GraphicDatabase.Get(typeof(Graphic_Multi), graphicPath, ShaderTypeDefOf.Cutout.Shader, Props.drawSize, Color.white, Color.white);
		return graphic;
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (graphicPath == null)
		{
			SwitchGraphicTo(0);
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	public IEnumerable<Gizmo> GetGizmos()
	{
		if (!parent.Faction.IsPlayer)
		{
			yield break;
		}
		yield return new Command_Action
		{
			defaultLabel = graphicLabel,
			defaultDesc = (Props.gizmoDesc ?? ((string)"Ancot.SwitchAdditionalGraphic".Translate())),
			icon = AncotLibraryIcon.SwitchA,
			action = delegate
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				for (int i = 0; i < Props.additionalGraph.Count; i++)
				{
					int localI = i;
					FloatMenuOption item = new FloatMenuOption(Props.additionalGraph[localI].label, delegate
					{
						Log.Message(localI);
						SwitchGraphicTo(localI);
					}, MenuOptionPriority.Default, null, null, 29f);
					list.Add(item);
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
		};
	}

	public override void PostDraw()
	{
		base.PostDraw();
		Mesh mesh = parent.Graphic.MeshAt(parent.Rotation);
		Vector3 drawPos = parent.DrawPos;
		drawPos.y += Props.layerOffset;
		Graphics.DrawMesh(mesh, drawPos, Quaternion.identity, Graphic.MatAt(parent.Rotation), 0);
	}

	private void SwitchGraphicTo(int num)
	{
		graphicLabel = Props.additionalGraph[num].label;
		graphicPath = Props.additionalGraph[num].textPath;
		graphic = GetGraphic();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref graphicPath, "graphicPath");
		Scribe_Values.Look(ref graphicLabel, "graphicLabel");
	}
}
