using UnityEngine;
using Verse;

namespace NCLProjectiles;

public struct EffectContext
{
	public Map map;

	public EffectDef def;

	public Thing anchor;

	public Thing destinationAnchor;

	public Vector3 position;

	public Vector3 origin;

	public Vector3 destination;

	public Quaternion rotation;

	public float angle;

	public float orbitAngle;

	public float parentScale;

	public int parentDuration;

	public int parentTicksElapsed;

	public int delayOffset;

	public Color? color;

	public EffectContext(Map map, EffectDef def)
	{
		anchor = null;
		destinationAnchor = null;
		position = default(Vector3);
		origin = default(Vector3);
		destination = default(Vector3);
		rotation = default(Quaternion);
		angle = 0f;
		orbitAngle = 0f;
		parentDuration = 0;
		parentTicksElapsed = 0;
		delayOffset = 0;
		color = null;
		parentScale = 1f;
		this.map = map;
		this.def = def;
	}

	public EffectContext CreateSubEffectContext(EffectDef subEffectDef)
	{
		EffectContext result = new EffectContext(map, subEffectDef);
		result.anchor = anchor;
		result.destinationAnchor = destinationAnchor;
		result.position = position;
		result.origin = origin;
		result.destination = destination;
		result.rotation = rotation;
		result.angle = angle;
		result.orbitAngle = orbitAngle;
		result.parentScale = parentScale;
		result.parentDuration = parentDuration;
		result.parentTicksElapsed = parentTicksElapsed;
		result.delayOffset = delayOffset;
		result.color = color;
		return result;
	}

	public EffectContext CreateSubEffectContext(EffectDef subEffectDef, int ticksElapsed)
	{
		EffectContext result = new EffectContext(map, subEffectDef);
		result.anchor = anchor;
		result.destinationAnchor = destinationAnchor;
		result.position = position;
		result.origin = origin;
		result.destination = destination;
		result.rotation = rotation;
		result.angle = angle;
		result.orbitAngle = orbitAngle;
		result.parentScale = parentScale;
		result.parentDuration = parentDuration;
		result.parentTicksElapsed = ticksElapsed;
		result.delayOffset = delayOffset;
		result.color = color;
		return result;
	}
}
