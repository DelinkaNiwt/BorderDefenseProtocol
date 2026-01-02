using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompProperties_AdditionalGraphicSwitch : CompProperties
{
	public float layerOffset = 0.01f;

	public Vector2 drawSize = new Vector2(1f, 1f);

	public string gizmoDesc;

	public List<GraphLabelWithTextPath> additionalGraph = new List<GraphLabelWithTextPath>();

	public CompProperties_AdditionalGraphicSwitch()
	{
		compClass = typeof(CompAdditionalGraphicSwitch);
	}
}
