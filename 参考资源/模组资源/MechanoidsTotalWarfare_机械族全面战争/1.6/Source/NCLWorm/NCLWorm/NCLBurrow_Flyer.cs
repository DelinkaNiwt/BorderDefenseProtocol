using RimWorld;
using UnityEngine;
using Verse;

namespace NCLWorm;

public class NCLBurrow_Flyer : PawnFlyer
{
	private int positionLastComputedTick = -1;

	private Vector3 groundPos;

	public override Vector3 DrawPos
	{
		get
		{
			RecomputePosition();
			return groundPos;
		}
	}

	protected override void Tick()
	{
		base.Tick();
		GenExplosion.DoExplosion(base.Position, base.Map, 1f, DamageDefOf.Bomb, null);
	}

	private void RecomputePosition()
	{
		if (positionLastComputedTick != ticksFlying)
		{
			positionLastComputedTick = ticksFlying;
			float t = (float)ticksFlying / (float)ticksFlightTime;
			float t2 = def.pawnFlyer.Worker.AdjustedProgress(t);
			groundPos = Vector3.Lerp(startVec, base.DestinationPos, t2);
			base.Position = groundPos.ToIntVec3();
		}
	}

	public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
	{
		RecomputePosition();
		if (phase == DrawPhase.Draw)
		{
			DrawShadow(groundPos);
		}
	}

	private void DrawShadow(Vector3 drawLoc)
	{
		Material material = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);
		if (!(material == null))
		{
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawLoc, Quaternion.identity, Vector3.one);
			Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
		}
	}
}
