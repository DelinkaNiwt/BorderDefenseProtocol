using System;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

[StaticConstructorOnStartup]
public class VisualEffect_Particle : VisualEffect
{
	protected Material material;

	protected Mesh mesh = MeshPool.plane10;

	protected Vector3 drawSize;

	protected Vector3 positionOffset;

	protected float angle;

	protected float originalAngle;

	protected Quaternion rotation;

	protected float rotationRate;

	protected Quaternion rotationDelta;

	protected Func<float, float> rotationFunction;

	protected float opacity = 1f;

	protected Func<float, float> opacityFunction;

	protected Color? color;

	protected Func<float, float> colorFunction;

	protected static readonly MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

	public VisualEffect_Particle(EffectMapComponent parentComponent, EffectContext context)
		: base(parentComponent, context)
	{
		if (def.colorCurve != null)
		{
			colorFunction = AnimationUtility.GetFunctionByName(def.colorFunction, AnimationUtility.Linear);
		}
		if (def.useColorOverride)
		{
			color = context.color ?? def.color;
		}
	}

	protected override void PreInitialize(EffectContext context)
	{
		base.PreInitialize(context);
		drawSize = def.drawSize;
		if (def.inheritRotationFromPath)
		{
			rotation = ((context.destination == context.origin) ? Quaternion.identity : Quaternion.LookRotation(context.destination - context.origin));
			angle = (originalAngle = rotation.eulerAngles.y);
		}
		else
		{
			angle = (originalAngle = (def.inheritRotation ? context.angle : 0f));
		}
		if (def.mirrorWestRotations)
		{
			angle += def.rotationOffset.RandomInRange;
			angle %= 360f;
			if (angle < 0f)
			{
				angle += 360f;
			}
			if (angle > 90f && angle <= 270f)
			{
				angle += 180f;
				angle %= 360f;
				mesh = MeshPool.plane10Flip;
			}
		}
		else
		{
			angle += def.rotationOffset.RandomInRange;
		}
		rotationFunction = AnimationUtility.GetFunctionByName(def.rotationFunction);
		rotationRate = def.rotationRate.RandomInRange;
		rotation = Quaternion.Euler(0f, angle, 0f);
		rotationDelta = Quaternion.Euler(0f, rotationRate, 0f);
		opacityFunction = AnimationUtility.GetFunctionByName(def.opacityFunction, AnimationUtility.FadeOutLinear);
		opacity = def.opacity;
		positionOffset = (def.applyRotationToDrawOffset ? (Quaternion.Euler(0f, originalAngle, 0f) * def.drawOffset) : def.drawOffset);
		if (def.applyDriftToPosition)
		{
			float randomInRange = def.drawDriftDistance.RandomInRange;
			if (randomInRange != 0f)
			{
				positionOffset += EffectUtility.CalculateDriftOffset(randomInRange);
			}
		}
		if (def.directionalMaterial)
		{
			material = def.MaterialForRotation(angle);
			angle = 0f;
			rotation = Quaternion.identity;
		}
		else
		{
			material = def.Material;
		}
	}

	protected override void Initialize(EffectContext context)
	{
		CalculateOpacity();
		if (anchor != null)
		{
			CalculatePosition();
		}
		else
		{
			SetPosition(position + positionOffset);
		}
	}

	public override bool Tick()
	{
		return base.Tick() && CalculateRotation() && CalculatePosition() && CalculateOpacity();
	}

	protected virtual bool CalculateRotation()
	{
		if (base.IsActive && rotationRate != 0f)
		{
			if (rotationFunction != null)
			{
				rotationDelta = Quaternion.Euler(0f, rotationRate * rotationFunction(base.Progress), 0f);
			}
			rotation *= rotationDelta;
		}
		return true;
	}

	protected virtual bool CalculatePosition()
	{
		if (anchor != null && base.IsActive)
		{
			Vector3 anchorPosition = GetAnchorPosition(anchor);
			if (anchorPosition == Vector3.zero)
			{
				return false;
			}
			SetPosition(anchorPosition + positionOffset);
		}
		return true;
	}

	protected float LerpOpacity(float value)
	{
		if (def.minOpacity <= 0f)
		{
			return def.opacity * value;
		}
		return Mathf.Lerp(def.minOpacity, def.opacity, value);
	}

	protected virtual bool CalculateOpacity()
	{
		if (opacityFunction != null && base.IsActive)
		{
			opacity = LerpOpacity(opacityFunction(base.Progress));
		}
		return true;
	}

	protected static Vector3 GetAnchorPosition(Thing anchor)
	{
		return anchor?.DrawPosHeld ?? Vector3.zero;
	}

	protected override void DrawInternal()
	{
		if (!(material == null))
		{
			Matrix4x4 matrix = Matrix4x4.TRS(base.Position, rotation, drawSize * sizeFactor);
			DrawInternal(ref matrix);
		}
	}

	protected virtual void DrawInternal(ref Matrix4x4 matrix)
	{
		if (color.HasValue)
		{
			Color value = color.Value;
			value.a *= opacity;
			materialPropertyBlock.Clear();
			materialPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
			Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, materialPropertyBlock);
		}
		else if (colorFunction != null && def.colorCurve != null)
		{
			Color value2 = def.colorCurve.Evaluate(colorFunction(progress));
			value2.a *= opacity;
			materialPropertyBlock.Clear();
			materialPropertyBlock.SetColor(ShaderPropertyIDs.Color, value2);
			Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, materialPropertyBlock);
		}
		else
		{
			Graphics.DrawMesh(mesh, matrix, FadedMaterialPool.FadedVersionOf(material, opacity), 0);
		}
	}
}
