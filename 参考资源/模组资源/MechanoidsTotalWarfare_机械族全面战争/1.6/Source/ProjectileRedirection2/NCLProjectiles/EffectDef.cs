using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class EffectDef : Def
{
	public static readonly int TypeCount = Enum.GetValues(typeof(EffectType)).Length;

	public static readonly int PriorityCount = Enum.GetValues(typeof(EffectPriority)).Length;

	public EffectType type;

	public EffectPriority priority;

	public List<EffectDef> subeffects;

	[Unsaved(false)]
	public Material material;

	[Unsaved(false)]
	public List<Material> materials;

	public string materialPath;

	public List<string> materialPaths;

	public int renderQueueOverride = -1;

	public int materialInterval = 1;

	public bool randomizeMaterial;

	public bool randomizeAngle;

	public bool directionalMaterial;

	public AltitudeLayer altitude = AltitudeLayer.Projectile;

	public int altitudeAdjustment;

	public int altitudeDrift;

	public bool syncAltitudeDrift;

	public ShaderTypeDef shaderType;

	public List<ShaderParameter> shaderParameters;

	public ColorCurve colorCurve;

	public Color? color;

	public int count = 1;

	public float size = 1f;

	public float minSize;

	public FloatRange sizeRange = FloatRange.Zero;

	public float length = 1f;

	public float minWidth;

	public float minLength;

	public float opacity = 1f;

	public float minOpacity;

	public Vector3 drawSize = Vector3.one;

	public Vector3 drawOffset = Vector3.zero;

	public Vector3 destinationDrawOffset = Vector3.zero;

	public FloatRange drawDriftDistance = FloatRange.Zero;

	public bool applyDriftToPosition = true;

	public bool applyDriftToDestination;

	public bool applyDriftToOrigin;

	public FloatRange driftOffset = FloatRange.Zero;

	public FloatRange height = FloatRange.Zero;

	public FloatRange startingDistance = FloatRange.Zero;

	public FloatRange distance = FloatRange.Zero;

	public FloatRange rotationOffset = FloatRange.Zero;

	public FloatRange rotationRate = FloatRange.Zero;

	public float minRadius;

	public float radius;

	public FloatRange orbitRate = FloatRange.Zero;

	public FloatRange orbitOffset = FloatRange.Zero;

	public FloatRange flipRate = FloatRange.Zero;

	public FloatRange flipOffset = FloatRange.Zero;

	public bool useEvenDriftSpread;

	public bool drawIfIntercepted = true;

	public bool scaleSizeWithParent;

	public bool scaleDistanceWithParent;

	public bool attachToOrigin;

	public bool attachToParent;

	public bool attachToTarget;

	public bool attachPersistently = true;

	public bool inheritRotation = true;

	public bool inheritRotationFromPath;

	public bool inheritRotationFromOrbit;

	public bool applyRotationToDrawOffset = true;

	public bool applyRotationToDestinationDrawOffset = true;

	public bool applyRotationToOrbit = true;

	public bool mirrorWestRotations;

	public bool neverDrawRotated;

	public bool useColorOverride = true;

	public AdditionalMotionProperties additionalMotion;

	public SoundDef startSound;

	public SoundDef endSound;

	public IntRange duration = new IntRange(60, 60);

	public FloatRange animationDuration = FloatRange.ZeroToOne;

	public IntRange? delay;

	public IntRange? delayStep;

	public int interval;

	public int triggerAt = -1;

	public int startAfter = -1;

	public int endBefore = -1;

	public int flipStopsAt = -1;

	public bool inheritDuration;

	public bool subtractParentElapsed;

	public bool randomize;

	public float chance = 1f;

	public string progressFunction;

	public string sizeFunction;

	public string widthFunction;

	public string lengthFunction;

	public string heightFunction;

	public string pathingFunction;

	public string opacityFunction;

	public string rotationFunction;

	public string radiusFunction;

	public string flipFunction;

	public string colorFunction;

	[Unsaved(false)]
	public bool isLargeParticle;

	public float Size
	{
		get
		{
			if (sizeRange.max > 0f)
			{
				return size * sizeRange.RandomInRange;
			}
			return size;
		}
	}

	public bool HasMaterial => material != null || !materials.NullOrEmpty();

	public Material Material
	{
		get
		{
			if (!materials.NullOrEmpty())
			{
				return materials.RandomElement();
			}
			return material;
		}
	}

	public Material MaterialForProgress(float progress)
	{
		if (materials.NullOrEmpty())
		{
			return material;
		}
		progress = Mathf.InverseLerp(animationDuration.min, animationDuration.max, progress);
		return materials[Mathf.RoundToInt(progress * (float)(materials.Count - 1))];
	}

	public Material MaterialForRotation(float angle)
	{
		angle %= 360f;
		if (angle > 340f || angle < 20f)
		{
			return materials[0];
		}
		if (angle > 200f)
		{
			return materials[3];
		}
		if (angle > 160f)
		{
			return materials[2];
		}
		return materials[1];
	}

	public bool ShouldBeActive(int ticksElapsed)
	{
		if (triggerAt > -1)
		{
			return ticksElapsed == triggerAt;
		}
		return (startAfter <= -1 || ticksElapsed > startAfter) && (endBefore <= -1 || ticksElapsed < endBefore);
	}

	public bool CheckInterval(EffectContext context)
	{
		return CheckInterval(context.parentTicksElapsed);
	}

	public bool CheckInterval(int ticksElapsed)
	{
		if (startAfter > 0)
		{
			ticksElapsed -= startAfter;
		}
		return interval < 2 || ticksElapsed % interval == 0;
	}

	public override IEnumerable<string> ConfigErrors()
	{
		if (directionalMaterial && materialPaths.Count != 4)
		{
			yield return "(NCL Projectiles) EffectDef with directionalMaterial set to true but does not have exactly 4 materialPaths: " + defName;
		}
	}

	public override void PostLoad()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			Shader shader = shaderType?.Shader ?? ShaderDatabase.TransparentPostLight;
			if (!materialPath.NullOrEmpty())
			{
				if (type == EffectType.Animated && materialPaths.NullOrEmpty())
				{
					IEnumerable<Texture2D> enumerable = from x in ContentFinder<Texture2D>.GetAllInFolder(materialPath)
						where !x.name.EndsWith(Graphic_Single.MaskSuffix)
						orderby x.name
						select x;
					materials = new List<Material>();
					foreach (Texture2D item in enumerable)
					{
						MaterialRequest req = new MaterialRequest(item, shader)
						{
							shaderParameters = shaderParameters
						};
						Material material = MaterialPool.MatFrom(req);
						if (renderQueueOverride > -1)
						{
							material.renderQueue = renderQueueOverride;
						}
						materials.Add(material);
					}
				}
				else
				{
					MaterialRequest req = new MaterialRequest(ContentFinder<Texture2D>.Get(materialPath), shader)
					{
						shaderParameters = shaderParameters
					};
					this.material = MaterialPool.MatFrom(req);
					if (renderQueueOverride > -1)
					{
						this.material.renderQueue = renderQueueOverride;
					}
				}
			}
			if (!materialPaths.NullOrEmpty())
			{
				materials = new List<Material>(materialPaths.Count);
				foreach (string materialPath in materialPaths)
				{
					MaterialRequest req2 = new MaterialRequest(ContentFinder<Texture2D>.Get(materialPath), shader)
					{
						shaderParameters = shaderParameters
					};
					Material material2 = MaterialPool.MatFrom(req2);
					if (renderQueueOverride > -1)
					{
						material2.renderQueue = renderQueueOverride;
					}
					materials.Add(material2);
				}
			}
			isLargeParticle = drawSize.x > 1f || drawSize.z > 1f;
		});
	}
}
