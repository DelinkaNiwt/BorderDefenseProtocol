using UnityEngine;
using Verse;

namespace TurbojetBackpack;

public class TailData
{
	public FleckDef fleckDef;

	public ThingDef moteDef;

	public float scale = 1f;

	public int interval = 1;

	public Color? color;

	public bool drawConnectingLine = false;

	public float offsetZ = 0f;
}
