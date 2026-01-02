using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Projectile_PoiMissile : Projectile_Explosive
{
	public ProjectileExtension_CMC modExtension;

	public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_FireGlow_Exp");

	public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLasting");

	public int Fleck_MakeFleckTickMax = 1;

	public IntRange Fleck_MakeFleckNum = new IntRange(3, 6);

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public int Fleck_MakeFleckTick;

	private bool Tryinit = false;

	public Vector3 position1;

	public Vector3 position2;

	public Vector3 P0;

	public Vector3 P1;

	public Vector3 P2;

	public Vector3 P3;

	public float Randf1;

	public float Randf2;

	public float Randf3;

	private Vector3 lasttargetpos = default(Vector3);

	private bool targetinit = false;

	public Mote_ScaleAndRotate mote;

	public Quaternion rotation;

	public float DCFExport;

	public override Vector3 ExactPosition
	{
		get
		{
			_ = position2;
			if (true)
			{
				return position2;
			}
			return base.ExactPosition;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		modExtension = def.GetModExtension<ProjectileExtension_CMC>();
	}

	private void FindNextTarget(Vector3 d)
	{
		IntVec3 center = IntVec3.FromVector3(d);
		IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(center, 11f, useCenter: true);
		foreach (IntVec3 item in enumerable)
		{
			if (item.InBounds(base.Map))
			{
				Pawn firstPawn = item.GetFirstPawn(base.Map);
				if (firstPawn != null && (firstPawn.Faction.HostileTo(launcher.Faction) || launcher == null) && !firstPawn.Downed && !firstPawn.Dead)
				{
					intendedTarget = firstPawn;
					return;
				}
			}
		}
		intendedTarget = CellRect.CenteredOn(center, 7).RandomCell;
	}

	public Vector3 BPos(float t)
	{
		t = Mathf.Clamp01(t);
		if (!Tryinit)
		{
			Randf1 = Rand.Range(-0.1f, 0.1f);
			Randf2 = Rand.Range(-0.1f, 0.05f);
			Randf3 = Rand.Range(25f, 40f);
			Tryinit = true;
		}
		P0 = origin;
		P1 = origin + (destination - origin) * (0.3f + Randf1);
		P2 = origin + (destination - origin) * (0.8f + Randf2) + new Vector3(0f, 0f, Randf3);
		P3 = destination;
		return P0 * Mathf.Pow(1f - t, 3f) + 3f * P1 * t * Mathf.Pow(1f - t, 2f) + 3f * P2 * Mathf.Pow(t, 2f) * (1f - t) + P3 * Mathf.Pow(t, 3f);
	}

	protected override void Tick()
	{
		if (this.DestroyedOrNull())
		{
			return;
		}
		position1 = BPos(base.DistanceCoveredFraction);
		position2 = BPos(base.DistanceCoveredFraction - 0.01f);
		rotation = Quaternion.LookRotation(position1 - position2);
		position1.y = AltitudeLayer.Projectile.AltitudeFor();
		position2.y = AltitudeLayer.Projectile.AltitudeFor();
		DCFExport = base.DistanceCoveredFraction;
		if (intendedTarget != null && intendedTarget.Thing != null)
		{
			if (!targetinit)
			{
				lasttargetpos = intendedTarget.Thing.DrawPos;
				targetinit = true;
			}
			if (intendedTarget != null)
			{
				_ = lasttargetpos;
				if ((intendedTarget.Thing.DrawPos - lasttargetpos).magnitude > 5f || intendedTarget.Cell.AnyGas(base.Map, GasType.BlindSmoke))
				{
					intendedTarget = null;
					Messages.Message("Message_MissileLostTarget".Translate(), MessageTypeDefOf.SilentInput);
					goto IL_034c;
				}
			}
			destination = intendedTarget.Thing.DrawPos;
			lasttargetpos = destination;
			if (mote.DestroyedOrNull())
			{
				ThingDef cMC_Mote_MissileLocked = CMC_Def.CMC_Mote_MissileLocked;
				if (launcher != null && launcher.Faction != null)
				{
					cMC_Mote_MissileLocked.graphicData.color = launcher.Faction.Color;
				}
				Vector3 vector = new Vector3(0f, 0f, 0f);
				vector.y = AltitudeLayer.PawnRope.AltitudeFor();
				mote = (Mote_ScaleAndRotate)ThingMaker.MakeThing(cMC_Mote_MissileLocked);
				mote.Attach(intendedTarget.Thing, vector);
				mote.Scale = def.graphicData.drawSize.x * 2f;
				mote.iniscale = def.graphicData.drawSize.x * 2f;
				mote.exactPosition = intendedTarget.Thing.DrawPos + vector;
				mote.solidTimeOverride = 9999f;
				mote.tickimpact = ticksToImpact + base.TickSpawned;
				mote.tickspawned = base.TickSpawned;
				GenSpawn.Spawn(mote, intendedTarget.Thing.Position, base.Map);
			}
			else
			{
				mote.MaintainMote();
			}
		}
		else if (base.DistanceCoveredFraction < 0.67f)
		{
			FindNextTarget(destination);
		}
		goto IL_034c;
		IL_034c:
		base.Tick();
	}

	protected override void TickInterval(int delta)
	{
		base.TickInterval(delta);
		if (!this.DestroyedOrNull())
		{
			Vector3 drawPos = DrawPos;
			drawPos.y = AltitudeLayer.Projectile.AltitudeFor();
			float num = (drawPos - intendedTarget.CenterVector3).AngleFlat();
			float velocityAngle = Fleck_Angle.RandomInRange + num;
			float scale = def.graphicData.drawSize.x / 1.92f;
			float randomInRange = Fleck_Speed2.RandomInRange;
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(drawPos, base.Map, FleckDef2, scale);
			dataStatic.rotationRate = Fleck_Rotation.RandomInRange;
			dataStatic.velocityAngle = velocityAngle;
			dataStatic.velocitySpeed = randomInRange;
			dataStatic.rotation = Rand.Range(0, 360);
			base.Map.flecks.CreateFleck(dataStatic);
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		ModExtentsion_Fragments modExtentsion_Fragments = def.GetModExtension<ModExtentsion_Fragments>();
		FleckMaker.ThrowFireGlow(base.Position.ToVector3Shifted(), base.Map, 3.5f);
		FleckMaker.ThrowSmoke(base.Position.ToVector3Shifted(), base.Map, 5.5f);
		FleckMaker.ThrowHeatGlow(base.Position, base.Map, 3.5f);
		if (modExtentsion_Fragments != null)
		{
			IntVec3 intVec = IntVec3.FromVector3(base.Position.ToVector3Shifted());
			IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(intVec, modExtentsion_Fragments.radius, useCenter: true);
			foreach (IntVec3 item in enumerable)
			{
				if (item.InBounds(base.Map) && Rand.Chance(0.1f * Mathf.Sqrt((item - intVec).Magnitude / (float)modExtentsion_Fragments.radius)))
				{
					ProjectileHitFlags hitFlags = ProjectileHitFlags.All;
					Projectile newThing = ThingMaker.MakeThing(CMC_Def.Bullet_CMC_Fragments) as Projectile;
					Projectile projectile = (Projectile)GenSpawn.Spawn(newThing, base.Position, base.Map);
					projectile.Launch(launcher, base.Position.ToVector3Shifted(), item, item, hitFlags, preventFriendlyFire, null, targetCoverDef);
				}
			}
		}
		base.Impact(hitThing, blockedByShield);
	}
}
