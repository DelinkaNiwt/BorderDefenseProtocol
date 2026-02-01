using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class VisualEffect_Line : VisualEffect_Particle
{
	protected Vector3 destination;

	protected IntVec3 intDestination;

	protected int intDestinationIndex;

	protected bool inBoundsDestination;

	public Vector3 destinationOffset;

	public Thing destinationAnchor;

	public Vector3 Destination
	{
		get
		{
			return destination;
		}
		set
		{
			destination = value;
			destination.y = def.altitude.AltitudeFor(def.altitudeAdjustment);
			intDestination = destination.ToIntVec3();
			intDestinationIndex = parentComponent.map.cellIndices.CellToIndex(intDestination);
			inBoundsDestination = intDestination.InBounds(parentComponent.map);
		}
	}

	public VisualEffect_Line(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
	}

	protected override void PreInitialize(EffectContext context)
	{
		base.PreInitialize(context);
		Vector3 vector = context.destination;
		if (def.applyRotationToDestinationDrawOffset)
		{
			vector += context.rotation * def.destinationDrawOffset;
		}
		else
		{
			vector += def.destinationDrawOffset;
		}
		Destination = vector;
	}

	protected override void Initialize(EffectContext context)
	{
		base.Initialize(context);
		if (def.attachToTarget && destinationAnchor != null)
		{
			destinationAnchor = context.destinationAnchor;
			destinationOffset = def.destinationDrawOffset;
			CalculateDestinationPosition();
		}
	}

	public override bool IsInViewOf(ref CellRect viewRect, FogGrid fogGrid, CellIndices cellIndices)
	{
		return base.IsInViewOf(ref viewRect, fogGrid, cellIndices) || (inBoundsDestination && viewRect.Contains(intDestination) && !fogGrid.IsFogged(intDestinationIndex));
	}

	protected override bool CalculatePosition()
	{
		return base.CalculatePosition() && CalculateDestinationPosition();
	}

	protected virtual bool CalculateDestinationPosition()
	{
		if (destinationAnchor != null && base.IsActive)
		{
			if (!destinationAnchor.SpawnedOrAnyParentSpawned)
			{
				return false;
			}
			Destination = destinationAnchor.DrawPosHeld ?? (Vector3.zero + destinationOffset);
		}
		return true;
	}

	protected override void DrawInternal()
	{
		if (base.Position != Destination)
		{
			Vector3 vector = Destination - base.Position;
			float num = vector.MagnitudeHorizontal() * def.length;
			if (num > 0.01f)
			{
				Matrix4x4 matrix = Matrix4x4.TRS(base.Position + vector / 2f, Quaternion.LookRotation(vector), new Vector3(sizeFactor, 1f, num));
				DrawInternal(ref matrix);
			}
		}
	}
}
