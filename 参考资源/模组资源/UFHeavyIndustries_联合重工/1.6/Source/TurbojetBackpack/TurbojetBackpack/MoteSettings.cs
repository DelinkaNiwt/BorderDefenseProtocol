using UnityEngine;
using Verse;

namespace TurbojetBackpack;

public class MoteSettings
{
	public ThingDef moteDef;

	public int interval = 3;

	public FloatRange scale = new FloatRange(1f, 1.5f);

	public FloatRange speed = new FloatRange(3f, 5f);

	public float spread = 0f;

	public float angleOffset = 0f;

	public Vector3 offset = Vector3.zero;

	public Color? color;
}
