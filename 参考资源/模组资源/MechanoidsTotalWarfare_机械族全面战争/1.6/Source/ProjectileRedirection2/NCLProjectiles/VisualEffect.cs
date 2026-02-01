using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCLProjectiles;

public abstract class VisualEffect
{
	public EffectMapComponent parentComponent;

	public EffectDef def;

	public Thing anchor;

	protected Vector3 position;

	protected IntVec3 intPosition;

	protected int intPositionIndex;

	protected bool inBounds;

	protected int delay;

	protected int duration;

	protected int progressTicks;

	protected float progress;

	protected Func<float, float> progressFunction;

	protected float minSize;

	protected float baseSize = 1f;

	protected float sizeFactor = 1f;

	protected Func<float, float> sizeFunction;

	protected bool startSoundPlayed;

	protected float DurationFactor => (float)duration / (float)def.duration.max;

	public Vector3 Position => position;

	public IntVec3 IntPosition => intPosition;

	protected float RawProgress => (float)progressTicks / (float)duration;

	protected float Progress
	{
		get
		{
			if (progressFunction != null)
			{
				return progressFunction(RawProgress);
			}
			return RawProgress;
		}
	}

	public virtual bool IsDone => progressTicks > duration;

	public bool IsActive => delay < 1;

	public VisualEffect(EffectMapComponent parentComponent, EffectContext context)
	{
		this.parentComponent = parentComponent;
		def = context.def;
		PreInitialize(context);
		Initialize(context);
	}

	protected virtual void PreInitialize(EffectContext context)
	{
		if (def.attachToParent)
		{
			anchor = context.anchor;
			if (anchor != null)
			{
				SetPosition(anchor.DrawPosHeld ?? Vector3.zero);
			}
		}
		else if (def.attachToTarget)
		{
			if (def.attachPersistently)
			{
				anchor = context.destinationAnchor;
				if (anchor != null)
				{
					SetPosition(anchor.DrawPosHeld ?? Vector3.zero);
				}
			}
			else
			{
				SetPosition(context.destination);
			}
		}
		else if (def.attachToOrigin)
		{
			SetPosition(context.origin);
		}
		else
		{
			SetPosition(context.position);
		}
		minSize = def.minSize;
		baseSize = def.Size;
		if (def.scaleSizeWithParent)
		{
			minSize *= context.parentScale;
			baseSize *= context.parentScale;
		}
		sizeFactor = baseSize;
		sizeFunction = AnimationUtility.GetFunctionByName(def.sizeFunction);
		EffectDef effectDef = def;
		delay = (effectDef.delay.HasValue ? effectDef.delay.GetValueOrDefault().RandomInRange : 0) + context.delayOffset;
		duration = ((def.inheritDuration && context.parentDuration > 0) ? context.parentDuration : def.duration.RandomInRange);
		if (def.subtractParentElapsed)
		{
			duration -= context.parentTicksElapsed;
		}
		progressFunction = AnimationUtility.GetFunctionByName(def.progressFunction);
	}

	protected abstract void Initialize(EffectContext context);

	protected virtual void SetPosition(Vector3 pos, bool normalize = true)
	{
		if (normalize)
		{
			pos.y = def.altitude.AltitudeFor(def.altitudeAdjustment);
		}
		position = pos;
		intPosition = position.ToIntVec3();
		intPositionIndex = parentComponent.map.cellIndices.CellToIndex(intPosition);
		inBounds = intPosition.InBounds(parentComponent.map);
	}

	public virtual bool IsInViewOf(ref CellRect viewRect, FogGrid fogGrid, CellIndices cellIndices)
	{
		if (def.isLargeParticle)
		{
			return inBounds && viewRect.ShouldBeVisibleFrom(intPosition, def.drawSize) && !fogGrid.IsFogged(intPositionIndex);
		}
		return inBounds && viewRect.Contains(intPosition) && !fogGrid.IsFogged(intPositionIndex);
	}

	public virtual bool Tick()
	{
		if (delay > 0)
		{
			delay--;
		}
		else
		{
			progressTicks++;
			progress = Progress;
			if (def.startSound != null && !startSoundPlayed)
			{
				def.startSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(intPosition, parentComponent.map)));
				startSoundPlayed = true;
			}
		}
		return !IsDone && CalculateSize();
	}

	protected float LerpSizeFactor(float value)
	{
		if (minSize <= 0f)
		{
			return baseSize * value;
		}
		return Mathf.Lerp(minSize, baseSize, value);
	}

	protected virtual bool CalculateSize()
	{
		if (sizeFunction != null)
		{
			sizeFactor = LerpSizeFactor(sizeFunction(progress));
		}
		return true;
	}

	public virtual void Draw()
	{
		if (IsActive && !IsDone)
		{
			DrawInternal();
		}
	}

	public virtual void End()
	{
		progressTicks = duration + 1;
	}

	protected abstract void DrawInternal();
}
