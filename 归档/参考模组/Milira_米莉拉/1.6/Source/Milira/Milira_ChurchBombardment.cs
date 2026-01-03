using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Milira;

[StaticConstructorOnStartup]
public class Milira_ChurchBombardment : OrbitalStrike
{
	public class BombardmentProjectile : IExposable
	{
		private int lifeTime;

		private int maxLifeTime;

		public IntVec3 targetCell;

		private const float StartZ = 60f;

		private const float Scale = 2.5f;

		private const float Angle = 180f;

		public int LifeTime => lifeTime;

		public BombardmentProjectile()
		{
		}

		public BombardmentProjectile(int lifeTime, IntVec3 targetCell)
		{
			this.lifeTime = lifeTime;
			maxLifeTime = lifeTime;
			this.targetCell = targetCell;
		}

		public void Tick()
		{
			lifeTime--;
		}

		public void Draw(Material material)
		{
			if (lifeTime > 0)
			{
				Vector3 pos = targetCell.ToVector3() + Vector3.forward * Mathf.Lerp(60f, 0f, 1f - (float)lifeTime / (float)maxLifeTime);
				pos.z += 1.25f;
				pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
				Matrix4x4 matrix = default(Matrix4x4);
				matrix.SetTRS(pos, Quaternion.Euler(0f, 180f, 0f), new Vector3(2.5f, 1f, 2.5f));
				Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
			}
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref lifeTime, "lifeTime", 0);
			Scribe_Values.Look(ref maxLifeTime, "maxLifeTime", 0);
			Scribe_Values.Look(ref targetCell, "targetCell");
		}
	}

	private int startTick;

	public float impactAreaRadius = 20f;

	public FloatRange explosionRadiusRange = new FloatRange(6f, 8f);

	public int bombIntervalTicks = Rand.Range(6, 9);

	public int warmupTicks = 300;

	public int explosionCount = 90;

	private int ticksToNextEffect;

	private IntVec3 nextExplosionCell = IntVec3.Invalid;

	private List<BombardmentProjectile> projectiles = new List<BombardmentProjectile>();

	public const int EffectiveAreaRadius = 23;

	private const int StartRandomFireEveryTicks = 20;

	private const int EffectDuration = 60;

	private static readonly Material ProjectileMaterial = MaterialPool.MatFrom("Things/Projectile/Bullet_Big", ShaderDatabase.Transparent, Color.white);

	public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
	{
		new CurvePoint(0f, 0f),
		new CurvePoint(0.1f, 0f),
		new CurvePoint(1f, 1f)
	};

	public override void StartStrike()
	{
		if (!base.Spawned)
		{
			Log.Error("Called StartStrike() on unspawned thing.");
			return;
		}
		duration = bombIntervalTicks * explosionCount;
		startTick = Find.TickManager.TicksGame;
	}

	protected override void Tick()
	{
		if (base.Destroyed)
		{
			return;
		}
		if (warmupTicks > 0)
		{
			warmupTicks--;
			if (warmupTicks == 0)
			{
				StartStrike();
			}
		}
		EffectTick();
	}

	private void EffectTick()
	{
		if (!nextExplosionCell.IsValid)
		{
			ticksToNextEffect = warmupTicks - bombIntervalTicks;
			GetNextExplosionCell();
		}
		ticksToNextEffect--;
		if (ticksToNextEffect <= 0 && base.TicksLeft >= bombIntervalTicks)
		{
			SoundDefOf.Bombardment_PreImpact.PlayOneShot(new TargetInfo(nextExplosionCell, base.Map));
			projectiles.Add(new BombardmentProjectile(60, nextExplosionCell));
			ticksToNextEffect = bombIntervalTicks;
			GetNextExplosionCell();
		}
		for (int num = projectiles.Count - 1; num >= 0; num--)
		{
			projectiles[num].Tick();
			if (projectiles[num].LifeTime <= 0)
			{
				GenExplosion.DoExplosion(projectiles[num].targetCell, base.Map, explosionRadiusRange.RandomInRange, DamageDefOf.Bomb, instigator, -1, -1f, null, projectile: def, weapon: weaponDef);
				projectiles.RemoveAt(num);
			}
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip = false);
		if (!projectiles.NullOrEmpty())
		{
			for (int i = 0; i < projectiles.Count; i++)
			{
				projectiles[i].Draw(ProjectileMaterial);
			}
		}
	}

	private void GetNextExplosionCell()
	{
		nextExplosionCell = (from x in GenRadial.RadialCellsAround(base.Position, impactAreaRadius, useCenter: true)
			where x.InBounds(base.Map)
			select x).RandomElementByWeight((IntVec3 x) => DistanceChanceFactor.Evaluate(x.DistanceTo(base.Position) / impactAreaRadius));
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref impactAreaRadius, "impactAreaRadius", 15f);
		Scribe_Values.Look(ref explosionRadiusRange, "explosionRadiusRange", new FloatRange(6f, 8f));
		Scribe_Values.Look(ref bombIntervalTicks, "bombIntervalTicks", 18);
		Scribe_Values.Look(ref warmupTicks, "warmupTicks", 0);
		Scribe_Values.Look(ref startTick, "startTick", 0);
		Scribe_Values.Look(ref ticksToNextEffect, "ticksToNextEffect", 0);
		Scribe_Values.Look(ref nextExplosionCell, "nextExplosionCell");
		Scribe_Collections.Look(ref projectiles, "projectiles", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (!nextExplosionCell.IsValid)
			{
				GetNextExplosionCell();
			}
			if (projectiles == null)
			{
				projectiles = new List<BombardmentProjectile>();
			}
		}
	}
}
