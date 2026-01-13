using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ECT;

public class Building_TurretGunHasSpeed : Building_TurretGun
{
	public float curAngle;

	private float cachedRotateSpeed = 1f;

	private bool cachedNoAutoAttack = false;

	private bool cachedSmartTargeting = true;

	private List<float> recoilStates;

	private List<int> recoilTimers;

	private ModExtension_ShootWithOffset shootExt;

	private Graphic gunBaseGraphic;

	private Material barrelMaterial;

	public float rotateSpeed => cachedRotateSpeed;

	public bool noautoattack => cachedNoAutoAttack;

	public bool SmartTargeting => cachedSmartTargeting;

	public Vector3 turretOrientation => Vector3.forward.RotatedBy(curAngle);

	public float deltaAngle => (currentTargetInt == null) ? 0f : Vector3.SignedAngle(turretOrientation, (currentTargetInt.CenterVector3 - DrawPos).Yto0(), Vector3.up);

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		ModExtension_HasSpeedTurret modExtension = def.GetModExtension<ModExtension_HasSpeedTurret>();
		if (modExtension != null)
		{
			cachedRotateSpeed = modExtension.speed;
			cachedNoAutoAttack = modExtension.noautoattack;
			cachedSmartTargeting = modExtension.smartPenetrationTargeting;
		}
		if (gun == null)
		{
			return;
		}
		shootExt = gun.def.GetModExtension<ModExtension_ShootWithOffset>();
		gunBaseGraphic = gun.Graphic;
		if (shootExt == null)
		{
			return;
		}
		if (!shootExt.barrelTexturePath.NullOrEmpty())
		{
			LongEventHandler.ExecuteWhenFinished(delegate
			{
				barrelMaterial = MaterialPool.MatFrom(shootExt.barrelTexturePath, ShaderDatabase.CutoutComplex, Color.white);
			});
		}
		if (!respawningAfterLoad && shootExt.offsets != null)
		{
			EnsureListSize(shootExt.offsets.Count);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref curAngle, "curAngle", 0f);
		Scribe_Collections.Look(ref recoilStates, "recoilStates", LookMode.Value);
		Scribe_Collections.Look(ref recoilTimers, "recoilTimers", LookMode.Value);
	}

	protected override void Tick()
	{
		if (base.Active && currentTargetInt != null)
		{
			if (burstWarmupTicksLeft == 1 && Mathf.Abs(deltaAngle) > rotateSpeed)
			{
				burstWarmupTicksLeft++;
			}
			curAngle += ((Mathf.Abs(deltaAngle) - rotateSpeed > 0f) ? (Mathf.Sign(deltaAngle) * rotateSpeed) : deltaAngle);
		}
		base.Tick();
		curAngle = Trim(curAngle);
		UpdateRecoil();
	}

	protected float Trim(float angle)
	{
		if (angle > 360f)
		{
			angle -= 360f;
		}
		if (angle < 0f)
		{
			angle += 360f;
		}
		return angle;
	}

	public void Notify_BarrelFired(int barrelIndex)
	{
		if (shootExt != null && barrelIndex >= 0)
		{
			EnsureListSize(barrelIndex + 1);
			recoilTimers[barrelIndex] = shootExt.recoilDurationTicks;
		}
	}

	private void EnsureListSize(int size)
	{
		if (recoilStates == null)
		{
			recoilStates = new List<float>();
		}
		if (recoilTimers == null)
		{
			recoilTimers = new List<int>();
		}
		while (recoilStates.Count < size)
		{
			recoilStates.Add(0f);
		}
		while (recoilTimers.Count < size)
		{
			recoilTimers.Add(0);
		}
	}

	private void UpdateRecoil()
	{
		if (recoilTimers == null || shootExt == null)
		{
			return;
		}
		for (int i = 0; i < recoilTimers.Count; i++)
		{
			if (recoilTimers[i] > 0)
			{
				recoilTimers[i]--;
				int recoilDurationTicks = shootExt.recoilDurationTicks;
				int recoilKickTicks = shootExt.recoilKickTicks;
				int num = recoilDurationTicks - recoilKickTicks;
				if (num <= 0)
				{
					recoilStates[i] = 0f;
				}
				else if (recoilTimers[i] > num)
				{
					int num2 = recoilKickTicks - (recoilTimers[i] - num);
					float value = (float)num2 / (float)recoilKickTicks;
					recoilStates[i] = value;
				}
				else
				{
					int num3 = num - recoilTimers[i];
					float num4 = (float)num3 / (float)num;
					recoilStates[i] = 1f - num4;
				}
			}
			else
			{
				recoilStates[i] = 0f;
			}
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Graphic.Draw(drawLoc, base.Rotation, this);
		Vector3 vector = drawLoc + Altitudes.AltIncVect;
		vector.x += def.building.turretTopOffset.x;
		vector.z += def.building.turretTopOffset.y;
		Quaternion quaternion = curAngle.ToQuat();
		Quaternion q = (curAngle - 90f).ToQuat();
		float num = AltitudeLayer.BuildingOnTop.AltitudeFor();
		if (barrelMaterial != null && shootExt != null && shootExt.offsets != null)
		{
			EnsureListSize(shootExt.offsets.Count);
			Vector3 s = new Vector3(shootExt.barrelTextureSize.x, 1f, shootExt.barrelTextureSize.y);
			for (int i = 0; i < shootExt.offsets.Count; i++)
			{
				Vector3 vector2 = new Vector3(shootExt.offsets[i].x, 0f, shootExt.offsets[i].y);
				float num2 = ((recoilStates.Count > i) ? (recoilStates[i] * shootExt.recoilAmount) : 0f);
				vector2.z -= num2;
				Vector3 vector3 = quaternion * vector2;
				Vector3 pos = vector + vector3;
				pos.y = num + 0.05f;
				Matrix4x4 matrix = default(Matrix4x4);
				matrix.SetTRS(pos, q, s);
				Graphics.DrawMesh(MeshPool.plane10, matrix, barrelMaterial, 0);
			}
		}
		if (gunBaseGraphic != null)
		{
			float turretTopDrawSize = def.building.turretTopDrawSize;
			Vector3 s2 = new Vector3(turretTopDrawSize, 1f, turretTopDrawSize);
			Vector3 pos2 = vector;
			pos2.y = num + 0.1f;
			Matrix4x4 matrix2 = default(Matrix4x4);
			matrix2.SetTRS(pos2, q, s2);
			Graphics.DrawMesh(MeshPool.plane10, matrix2, gunBaseGraphic.MatAt(base.Rotation), 0);
		}
	}

	private IAttackTargetSearcher TargSearcher()
	{
		if (mannableComp != null && mannableComp.MannedNow)
		{
			return mannableComp.ManningPawn;
		}
		return this;
	}

	private bool IsValidTarget(Thing t)
	{
		if (t is Pawn pawn)
		{
			if (base.Faction == Faction.OfPlayer && pawn.IsPrisoner)
			{
				return false;
			}
			if (AttackVerb.ProjectileFliesOverhead())
			{
				RoofDef roofDef = base.Map.roofGrid.RoofAt(t.Position);
				if (roofDef != null && roofDef.isThickRoof)
				{
					return false;
				}
			}
			if (mannableComp == null)
			{
				return !GenAI.MachinesLike(base.Faction, pawn);
			}
			if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
			{
				return false;
			}
		}
		return true;
	}

	public override LocalTargetInfo TryFindNewTarget()
	{
		if (noautoattack)
		{
			return null;
		}
		IAttackTargetSearcher attackTargetSearcher = TargSearcher();
		Faction faction = attackTargetSearcher.Thing.Faction;
		float range = AttackVerb.verbProps.range;
		if (Rand.Value < 0.5f && AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate(Building x)
		{
			float num = AttackVerb.verbProps.EffectiveMinRange(x, this);
			float num2 = x.Position.DistanceToSquared(base.Position);
			return num2 > num * num && num2 < range * range;
		}).TryRandomElement(out var result))
		{
			return result;
		}
		TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
		if (!AttackVerb.ProjectileFliesOverhead())
		{
			targetScanFlags |= TargetScanFlags.NeedLOSToAll;
			targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
		}
		if (AttackVerb.IsIncendiary_Ranged())
		{
			targetScanFlags |= TargetScanFlags.NeedNonBurning;
		}
		if (def.building.IsMortar)
		{
			targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
		}
		return (Thing)AttackTargetFinderAngle.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, turretOrientation, IsValidTarget, 0f, 9999f, SmartTargeting);
	}
}
