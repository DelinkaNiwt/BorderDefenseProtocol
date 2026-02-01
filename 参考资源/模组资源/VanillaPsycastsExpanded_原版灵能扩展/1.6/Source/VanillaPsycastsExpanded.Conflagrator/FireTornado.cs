using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Conflagrator;

[StaticConstructorOnStartup]
[HarmonyPatch]
public class FireTornado : ThingWithComps
{
	private static readonly MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();

	private static readonly Material TornadoMaterial = MaterialPool.MatFrom("Effects/Conflagrator/FireTornado/FireTornadoFat", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.Tornado);

	private static readonly FloatRange PartsDistanceFromCenter = new FloatRange(1f, 5f);

	private static readonly float ZOffsetBias = -4f * PartsDistanceFromCenter.min;

	private static readonly FleckDef FireTornadoPuff = DefDatabase<FleckDef>.GetNamed("VPE_FireTornadoDustPuff");

	private static ModuleBase directionNoise;

	public int ticksLeftToDisappear = -1;

	private float direction;

	private int leftFadeOutTicks = -1;

	private Vector2 realPosition;

	private int spawnTick;

	private Sustainer sustainer;

	private float FadeInOutFactor
	{
		get
		{
			float a = Mathf.Clamp01((float)(Find.TickManager.TicksGame - spawnTick) / 120f);
			float b = ((leftFadeOutTicks < 0) ? 1f : Mathf.Min((float)leftFadeOutTicks / 120f, 1f));
			return Mathf.Min(a, b);
		}
	}

	[HarmonyPatch(typeof(WeatherBuildupUtility), "AddSnowRadial")]
	[HarmonyPrefix]
	public static void FixSnowUtility(ref float radius)
	{
		if (radius > GenRadial.MaxRadialPatternRadius)
		{
			radius = GenRadial.MaxRadialPatternRadius - 1f;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref realPosition, "realPosition");
		Scribe_Values.Look(ref direction, "direction", 0f);
		Scribe_Values.Look(ref spawnTick, "spawnTick", 0);
		Scribe_Values.Look(ref leftFadeOutTicks, "leftFadeOutTicks", 0);
		Scribe_Values.Look(ref ticksLeftToDisappear, "ticksLeftToDisappear", 0);
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			Vector3 vector = base.Position.ToVector3Shifted();
			realPosition = new Vector2(vector.x, vector.z);
			direction = Rand.Range(0f, 360f);
			spawnTick = Find.TickManager.TicksGame;
			leftFadeOutTicks = -1;
		}
		CreateSustainer();
	}

	public static void ThrowPuff(Vector3 loc, Map map, float scale, Color color)
	{
		if (loc.ShouldSpawnMotesAt(map))
		{
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, FireTornadoPuff, 1.9f * scale);
			dataStatic.rotationRate = Rand.Range(-60, 60);
			dataStatic.velocityAngle = Rand.Range(0, 360);
			dataStatic.velocitySpeed = Rand.Range(0.6f, 0.75f);
			dataStatic.instanceColor = color;
			map.flecks.CreateFleck(dataStatic);
		}
	}

	protected override void Tick()
	{
		if (!base.Spawned)
		{
			return;
		}
		if (sustainer == null)
		{
			Log.Error("Tornado sustainer is null.");
			CreateSustainer();
		}
		sustainer.Maintain();
		UpdateSustainerVolume();
		GetComp<CompWindSource>().wind = 5f * FadeInOutFactor;
		if (leftFadeOutTicks > 0)
		{
			leftFadeOutTicks--;
			if (leftFadeOutTicks == 0)
			{
				Destroy();
			}
			return;
		}
		if (directionNoise == null)
		{
			directionNoise = new Perlin(0.0020000000949949026, 2.0, 0.5, 4, 1948573612, QualityMode.Medium);
		}
		direction += (float)directionNoise.GetValue(Find.TickManager.TicksAbs, (float)(thingIDNumber % 500) * 1000f, 0.0) * 0.78f;
		realPosition = realPosition.Moved(direction, 0.028333334f);
		IntVec3 intVec = new Vector3(realPosition.x, 0f, realPosition.y).ToIntVec3();
		if (intVec.InBounds(base.Map))
		{
			base.Position = intVec;
			if (this.IsHashIntervalTick(15))
			{
				DoFire();
			}
			if (this.IsHashIntervalTick(60))
			{
				SpawnChemfuel();
			}
			if (ticksLeftToDisappear > 0)
			{
				ticksLeftToDisappear--;
				if (ticksLeftToDisappear == 0)
				{
					leftFadeOutTicks = 120;
					Messages.Message("MessageTornadoDissipated".Translate(), new TargetInfo(base.Position, base.Map), MessageTypeDefOf.PositiveEvent);
				}
			}
			if (this.IsHashIntervalTick(4) && !CellImmuneToDamage(base.Position))
			{
				float num = Rand.Range(0.6f, 1f);
				Vector3 vector = new Vector3(realPosition.x, 0f, realPosition.y);
				vector.y = AltitudeLayer.MoteOverhead.AltitudeFor();
				ThrowPuff(vector + Vector3Utility.RandomHorizontalOffset(1.5f), base.Map, Rand.Range(1.5f, 3f), new Color(num, num, num));
			}
		}
		else
		{
			leftFadeOutTicks = 120;
			Messages.Message("MessageTornadoLeftMap".Translate(), new TargetInfo(base.Position, base.Map), MessageTypeDefOf.PositiveEvent);
		}
	}

	private void DoFire()
	{
		foreach (IntVec3 item in (from c in GenRadial.RadialCellsAround(base.Position, 4.2f, useCenter: true)
			where c.InBounds(base.Map) && !CellImmuneToDamage(c)
			select c).InRandomOrder().Take(Rand.Range(3, 5)))
		{
			Fire firstThing = item.GetFirstThing<Fire>(base.Map);
			if (firstThing == null)
			{
				FireUtility.TryStartFireIn(item, base.Map, 15f, this);
			}
			else
			{
				firstThing.fireSize += 1f;
			}
		}
		foreach (Pawn item2 in GenRadial.RadialDistinctThingsAround(base.Position, base.Map, 4.2f, useCenter: true).OfType<Pawn>())
		{
			Fire fire = (Fire)item2.GetAttachment(ThingDefOf.Fire);
			if (fire == null)
			{
				item2.TryAttachFire(15f, this);
			}
			else
			{
				fire.fireSize += 1f;
			}
		}
	}

	private void SpawnChemfuel()
	{
		foreach (IntVec3 item in (from c in GenRadial.RadialCellsAround(base.Position, 4.2f, useCenter: true)
			where c.InBounds(base.Map) && FilthMaker.CanMakeFilth(c, base.Map, ThingDefOf.Filth_Fuel) && !CellImmuneToDamage(c)
			select c).InRandomOrder().Take(Rand.Range(1, 3)))
		{
			FilthMaker.TryMakeFilth(item, base.Map, ThingDefOf.Filth_Fuel);
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Rand.PushState();
		Rand.Seed = thingIDNumber;
		for (int i = 0; i < 90; i++)
		{
			DrawTornadoPart(PartsDistanceFromCenter.RandomInRange, Rand.Range(0f, 360f), Rand.Range(0.9f, 1.1f), Rand.Range(0.52f, 0.88f));
		}
		Rand.PopState();
	}

	private void DrawTornadoPart(float distanceFromCenter, float initialAngle, float speedMultiplier, float colorMultiplier)
	{
		int ticksGame = Find.TickManager.TicksGame;
		float num = 1f / distanceFromCenter;
		float num2 = 25f * speedMultiplier * num;
		float num3 = (initialAngle + (float)ticksGame * num2) % 360f;
		Vector2 vector = realPosition.Moved(num3, AdjustedDistanceFromCenter(distanceFromCenter));
		vector.y += distanceFromCenter * 4f;
		vector.y += ZOffsetBias;
		Vector3 vector2 = new Vector3(vector.x, AltitudeLayer.Weather.AltitudeFor() + 3f / 74f * Rand.Range(0f, 1f), vector.y);
		float num4 = distanceFromCenter * 3f;
		float num5 = 1f;
		if (num3 > 270f)
		{
			num5 = GenMath.LerpDouble(270f, 360f, 0f, 1f, num3);
		}
		else if (num3 > 180f)
		{
			num5 = GenMath.LerpDouble(180f, 270f, 1f, 0f, num3);
		}
		float num6 = Mathf.Min(distanceFromCenter / (PartsDistanceFromCenter.max + 2f), 1f);
		float num7 = Mathf.InverseLerp(0.18f, 0.4f, num6);
		Vector3 vector3 = new Vector3(Mathf.Sin((float)ticksGame / 1000f + (float)(thingIDNumber * 10)) * 2f, 0f, 0f);
		Vector3 pos = vector2 + vector3 * num7;
		float a = Mathf.Max(1f - num6, 0f) * num5 * FadeInOutFactor;
		Color value = new Color(colorMultiplier, colorMultiplier, colorMultiplier, a);
		matPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
		Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0f, num3, 0f), new Vector3(num4, 1f, num4));
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, TornadoMaterial, 0, null, 0, matPropertyBlock);
	}

	private static float AdjustedDistanceFromCenter(float distanceFromCenter)
	{
		float num = Mathf.Min(distanceFromCenter / 8f, 1f);
		num *= num;
		return distanceFromCenter * num;
	}

	private void UpdateSustainerVolume()
	{
		sustainer.info.volumeFactor = FadeInOutFactor;
	}

	private void CreateSustainer()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			SoundDef tornado = SoundDefOf.Tornado;
			sustainer = tornado.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
			UpdateSustainerVolume();
		});
	}

	private bool CellImmuneToDamage(IntVec3 c)
	{
		if (c.Roofed(base.Map) && c.GetRoof(base.Map).isThickRoof)
		{
			return true;
		}
		Building edifice = c.GetEdifice(base.Map);
		if (edifice != null && edifice.def.category == ThingCategory.Building)
		{
			if (!edifice.def.building.isNaturalRock)
			{
				if (edifice.def == ThingDefOf.Wall)
				{
					return edifice.Faction == null;
				}
				return false;
			}
			return true;
		}
		return false;
	}
}
