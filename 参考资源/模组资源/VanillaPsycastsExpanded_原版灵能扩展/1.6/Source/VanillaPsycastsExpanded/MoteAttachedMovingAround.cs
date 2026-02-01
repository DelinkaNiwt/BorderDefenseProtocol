using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

public class MoteAttachedMovingAround : MoteAttached
{
	private Vector3 curPosition;

	private float direction;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			curPosition = new Vector3(Rand.Range(-0.5f, 0.5f), 0f, Rand.Range(-0.5f, 0.5f));
			exactPosition = GetRootPosition() + curPosition;
			exactPosition.y = link1.Target.CenterVector3.y + 1f;
		}
	}

	protected override void TimeInterval(float deltaTime)
	{
		base.TimeInterval(deltaTime);
		curPosition = GetNewMoveVector();
		Vector3 rootPosition = GetRootPosition();
		exactPosition = rootPosition + curPosition;
		exactPosition.y = link1.Target.CenterVector3.y + 1f;
	}

	public Vector3 GetNewMoveVector()
	{
		Vector2 v = new Vector2(curPosition.x, curPosition.z);
		direction += Rand.Range(-22.5f, 22.5f);
		if (direction < -360f)
		{
			direction = Mathf.Abs(direction - -360f);
		}
		if (direction > 360f)
		{
			direction -= 360f;
		}
		Vector2 vector = v.Moved(direction, 0.01f);
		return Vector3.ClampMagnitude(new Vector3(vector.x, 0f, vector.y), 0.5f);
	}

	public Vector3 GetRootPosition()
	{
		Vector3 vector = def.mote.attachedDrawOffset;
		if (def.mote.attachedToHead && link1.Target.Thing is Pawn { story: not null } pawn)
		{
			vector = pawn.Drawer.renderer.BaseHeadOffsetAt((pawn.GetPosture() == PawnPosture.Standing) ? Rot4.North : pawn.Drawer.renderer.LayingFacing()).RotatedBy(pawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None));
		}
		return link1.LastDrawPos + vector;
	}
}
