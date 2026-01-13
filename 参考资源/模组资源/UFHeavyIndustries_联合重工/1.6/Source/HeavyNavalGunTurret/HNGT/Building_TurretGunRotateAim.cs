using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace HNGT;

public class Building_TurretGunRotateAim : Building_TurretGun
{
	public float curAngle;

	private List<float> recoilStates;

	private List<int> recoilTimers;

	private List<int> muzzleFlashTimers;

	private Material muzzleFlashMaterial;

	private ThingDef muzzleFlashMoteDef;

	private ModExtension_BarrelWithRecoilAndFlash shootExt;

	private Graphic gunBaseGraphic;

	private Material barrelMaterial;

	public bool isFiringInterMap = false;

	private int interMapTargetTile;

	private Map interMapTargetMap;

	private int interMapBurstCounter = 0;

	private IntVec3 interMapTargetCell;

	private string interMapWorldObjectDefName;

	private int interMapBurstRoundsTotal;

	private string interMapPayloadDefName;

	public float rotateSpeed
	{
		get
		{
			ModExt_RotateAimTurret modExt_RotateAimTurret = ext;
			return modExt_RotateAimTurret.speed;
		}
	}

	public ModExt_RotateAimTurret ext => def.GetModExtension<ModExt_RotateAimTurret>();

	public Vector3 turretOrientation => Vector3.forward.RotatedBy(curAngle);

	public float deltaAngle => (currentTargetInt == null) ? 0f : Vector3.SignedAngle(turretOrientation, (currentTargetInt.CenterVector3 - DrawPos).Yto0(), Vector3.up);

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		shootExt = gun.def.GetModExtension<ModExtension_BarrelWithRecoilAndFlash>();
		gunBaseGraphic = gun.Graphic;
		if (shootExt == null)
		{
			return;
		}
		if (!shootExt.barrelTexturePath.NullOrEmpty())
		{
			LongEventHandler.ExecuteWhenFinished(delegate
			{
				barrelMaterial = MaterialPool.MatFrom(shootExt.barrelTexturePath, ShaderDatabase.DefaultShader, Color.white);
			});
		}
		if (!shootExt.muzzleFlashMoteDefName.NullOrEmpty())
		{
			muzzleFlashMoteDef = DefDatabase<ThingDef>.GetNamed(shootExt.muzzleFlashMoteDefName, errorOnFail: false);
			if (muzzleFlashMoteDef != null)
			{
				LongEventHandler.ExecuteWhenFinished(delegate
				{
					muzzleFlashMaterial = MaterialPool.MatFrom(muzzleFlashMoteDef.graphicData.texPath, ShaderDatabase.MoteGlow, Color.white);
				});
			}
		}
		if (!respawningAfterLoad && shootExt.offsets != null)
		{
			recoilStates = new List<float>();
			recoilTimers = new List<int>();
			muzzleFlashTimers = new List<int>();
			for (int num = 0; num < shootExt.offsets.Count; num++)
			{
				recoilStates.Add(0f);
				recoilTimers.Add(0);
				muzzleFlashTimers.Add(0);
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref curAngle, "curAngle", 0f);
		Scribe_Collections.Look(ref recoilStates, "recoilStates", LookMode.Value);
		Scribe_Collections.Look(ref recoilTimers, "recoilTimers", LookMode.Value);
		Scribe_Collections.Look(ref muzzleFlashTimers, "muzzleFlashTimers", LookMode.Value);
		Scribe_Values.Look(ref isFiringInterMap, "isFiringInterMap", defaultValue: false);
		Scribe_Values.Look(ref interMapTargetTile, "interMapTargetTile", 0);
		Scribe_References.Look(ref interMapTargetMap, "interMapTargetMap");
		Scribe_Values.Look(ref interMapBurstCounter, "interMapBurstCounter", 0);
		Scribe_Values.Look(ref interMapTargetCell, "interMapTargetCell");
		Scribe_Values.Look(ref interMapWorldObjectDefName, "interMapWorldObjectDefName");
		Scribe_Values.Look(ref interMapBurstRoundsTotal, "interMapBurstRoundsTotal", 3);
		Scribe_Values.Look(ref interMapPayloadDefName, "interMapPayloadDefName");
	}

	private bool CanAttackTarget(LocalTargetInfo t)
	{
		return CanAttackTarget(t.CenterVector3);
	}

	private bool CanAttackTarget(Thing t)
	{
		return CanAttackTarget(t.DrawPos);
	}

	private bool CanAttackTarget(Vector3 t)
	{
		return Vector3.Angle(turretOrientation, (t - DrawPos).Yto0()) <= rotateSpeed;
	}

	public void Notify_BarrelFired(int barrelIndex)
	{
		if (shootExt != null && barrelIndex >= 0)
		{
			if (recoilTimers != null && barrelIndex < recoilTimers.Count)
			{
				recoilTimers[barrelIndex] = shootExt.recoilDurationTicks;
			}
			if (muzzleFlashTimers != null && barrelIndex < muzzleFlashTimers.Count && muzzleFlashMoteDef != null)
			{
				int value = (int)((muzzleFlashMoteDef.mote.solidTime + muzzleFlashMoteDef.mote.fadeOutTime) * 60f);
				muzzleFlashTimers[barrelIndex] = value;
			}
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
					float num3 = (float)num2 / (float)recoilKickTicks;
					recoilStates[i] = num3 * num3;
				}
				else
				{
					int num4 = num - recoilTimers[i];
					float num5 = (float)num4 / (float)num;
					recoilStates[i] = 1f - num5 * num5;
				}
			}
			else
			{
				recoilStates[i] = 0f;
			}
		}
	}

	private void UpdateMuzzleFlash()
	{
		if (muzzleFlashTimers == null)
		{
			return;
		}
		for (int i = 0; i < muzzleFlashTimers.Count; i++)
		{
			if (muzzleFlashTimers[i] > 0)
			{
				muzzleFlashTimers[i]--;
			}
		}
	}

	public void StartInterMapFire(int targetTile, Map targetMap, IntVec3 targetCell, string worldObjectDefName, string payloadDefName, int burstRounds)
	{
		isFiringInterMap = true;
		interMapTargetTile = targetTile;
		interMapTargetMap = targetMap;
		interMapTargetCell = targetCell;
		interMapWorldObjectDefName = worldObjectDefName;
		interMapPayloadDefName = payloadDefName;
		interMapBurstRoundsTotal = burstRounds;
		interMapBurstCounter = 0;
		ResetCurrentTarget();
	}

	private void ResetInterMapState()
	{
		isFiringInterMap = false;
		interMapTargetMap = null;
		interMapBurstCounter = 0;
		interMapWorldObjectDefName = null;
		interMapPayloadDefName = null;
		interMapBurstRoundsTotal = 3;
		ResetCurrentTarget();
	}

	public void ResetCurrentTarget()
	{
		currentTargetInt = LocalTargetInfo.Invalid;
		burstWarmupTicksLeft = 0;
	}

	public new virtual void TryStartShootSomething(bool canBeginBurstImmediately)
	{
		if (progressBarEffecter != null)
		{
			progressBarEffecter.Cleanup();
			progressBarEffecter = null;
		}
		if (!base.Spawned || (AttackVerb.ProjectileFliesOverhead() && base.Map.roofGrid.Roofed(base.Position)) || !AttackVerb.Available())
		{
			ResetCurrentTarget();
			return;
		}
		if (!isFiringInterMap)
		{
			bool isValid = currentTargetInt.IsValid;
			if (forcedTarget.IsValid)
			{
				currentTargetInt = forcedTarget;
			}
			else
			{
				currentTargetInt = TryFindNewTarget();
			}
			if (!isValid && currentTargetInt.IsValid && def.building.playTargetAcquiredSound)
			{
				SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map));
			}
		}
		if (!currentTargetInt.IsValid)
		{
			ResetCurrentTarget();
			return;
		}
		float randomInRange = def.building.turretBurstWarmupTime.RandomInRange;
		if (randomInRange > 0f)
		{
			burstWarmupTicksLeft = randomInRange.SecondsToTicks();
		}
		else if (canBeginBurstImmediately)
		{
			BeginBurst();
		}
		else
		{
			burstWarmupTicksLeft = 1;
		}
	}

	private void TryLaunchInterMapAttack()
	{
		string text = interMapWorldObjectDefName;
		if (string.IsNullOrEmpty(text))
		{
			Log.Error("HNGT: " + def.defName + " tried to do a global attack, but 'interMapWorldObjectDefName' is null.");
			ResetInterMapState();
			return;
		}
		WorldObjectDef named = DefDatabase<WorldObjectDef>.GetNamed(text, errorOnFail: false);
		if (named == null)
		{
			Log.Error("HNGT: Can not find '" + text + "' WorldObjectDef.");
			ResetInterMapState();
			return;
		}
		if (!(WorldObjectMaker.MakeWorldObject(named) is WorldObject_GlobalAttackDevice worldObject_GlobalAttackDevice))
		{
			Log.Error("HNGT: WorldObjectDef '" + text + "' worldObjectClass is not a 'WorldObject_GlobalAttackDevice' or its subclass.");
			ResetInterMapState();
			return;
		}
		worldObject_GlobalAttackDevice.startTile = base.Map.Tile;
		worldObject_GlobalAttackDevice.destinationTile = interMapTargetTile;
		worldObject_GlobalAttackDevice.destinationCell = interMapTargetCell;
		worldObject_GlobalAttackDevice.payloadThingDefName = interMapPayloadDefName;
		worldObject_GlobalAttackDevice.instigator = this;
		worldObject_GlobalAttackDevice.Tile = base.Map.Tile;
		Find.WorldObjects.Add(worldObject_GlobalAttackDevice);
		ResetInterMapState();
	}

	protected override void Tick()
	{
		base.Tick();
		if (isFiringInterMap)
		{
			if (base.Active && (mannableComp == null || mannableComp.MannedNow))
			{
				if (interMapTargetTile < 0 || interMapTargetTile >= Find.WorldGrid.TilesCount)
				{
					Log.ErrorOnce($"HNGT: Turret {def.defName} has an invalid interMapTargetTile ({interMapTargetTile}).", thingIDNumber);
					ResetInterMapState();
					return;
				}
				float headingFromTo = Find.World.grid.GetHeadingFromTo(base.Map.Tile, interMapTargetTile);
				float num = headingFromTo - curAngle;
				if (num > 180f)
				{
					num -= 360f;
				}
				if (num < -180f)
				{
					num += 360f;
				}
				curAngle += ((Mathf.Abs(num) - rotateSpeed > 0f) ? (Mathf.Sign(num) * rotateSpeed) : num);
				bool flag = Mathf.Abs(num) < rotateSpeed * 1.5f;
				bool flag2 = burstCooldownTicksLeft <= 0 && burstWarmupTicksLeft <= 0;
				bool flag3 = AttackVerb.state == VerbState.Idle;
				if (flag && flag2 && flag3)
				{
					if (interMapBurstCounter < interMapBurstRoundsTotal)
					{
						currentTargetInt = new LocalTargetInfo(base.Position + (turretOrientation * 500f).ToIntVec3());
						TryStartShootSomething(canBeginBurstImmediately: true);
						interMapBurstCounter++;
					}
					else
					{
						TryLaunchInterMapAttack();
					}
				}
			}
		}
		else if (base.Active && currentTargetInt != null)
		{
			if (burstWarmupTicksLeft == 1 && Mathf.Abs(deltaAngle) > rotateSpeed)
			{
				burstWarmupTicksLeft++;
			}
			curAngle += ((Mathf.Abs(deltaAngle) - rotateSpeed > 0f) ? (Mathf.Sign(deltaAngle) * rotateSpeed) : deltaAngle);
		}
		curAngle = Trim(curAngle);
		UpdateRecoil();
		UpdateMuzzleFlash();
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

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Graphic.Draw(drawLoc, base.Rotation, this);
		Vector3 vector = drawLoc + Altitudes.AltIncVect;
		vector.x += def.building.turretTopOffset.x;
		vector.z += def.building.turretTopOffset.y;
		Quaternion quaternion = curAngle.ToQuat();
		Quaternion q = (curAngle - 90f).ToQuat();
		float num = AltitudeLayer.BuildingOnTop.AltitudeFor();
		if (barrelMaterial != null && shootExt != null && shootExt.offsets != null && recoilStates != null)
		{
			Vector3 s = new Vector3(shootExt.barrelTextureSize.x, 1f, shootExt.barrelTextureSize.y);
			for (int i = 0; i < shootExt.offsets.Count; i++)
			{
				Vector3 vector2 = new Vector3(shootExt.offsets[i].x, 0f, shootExt.offsets[i].y);
				float num2 = ((recoilStates.Count > i) ? (recoilStates[i] * shootExt.recoilAmount) : 0f);
				vector2.z -= num2;
				Vector3 vector3 = quaternion * vector2;
				Vector3 vector4 = vector + vector3;
				vector4.y = num + 0.05f;
				Matrix4x4 matrix = default(Matrix4x4);
				matrix.SetTRS(vector4, q, s);
				Graphics.DrawMesh(MeshPool.plane10, matrix, barrelMaterial, 0);
				if (muzzleFlashMaterial != null && muzzleFlashTimers != null && muzzleFlashTimers.Count > i && muzzleFlashTimers[i] > 0 && muzzleFlashMoteDef != null)
				{
					float z = s.z / 2f;
					Vector3 vector5 = quaternion * new Vector3(0f, 0f, z);
					Vector3 pos = vector4 + vector5;
					pos.y = num + 0.07f;
					float num3 = muzzleFlashMoteDef.mote.fadeOutTime * 60f;
					float alpha = 1f;
					if ((float)muzzleFlashTimers[i] < num3)
					{
						alpha = (float)muzzleFlashTimers[i] / num3;
					}
					Material material = FadedMaterialPool.FadedVersionOf(muzzleFlashMaterial, alpha);
					Vector3 s2 = new Vector3(muzzleFlashMoteDef.graphicData.drawSize.x, 1f, muzzleFlashMoteDef.graphicData.drawSize.y);
					Matrix4x4 matrix2 = default(Matrix4x4);
					matrix2.SetTRS(pos, q, s2);
					Graphics.DrawMesh(MeshPool.plane10, matrix2, material, 0);
				}
			}
		}
		if (gunBaseGraphic != null)
		{
			float turretTopDrawSize = def.building.turretTopDrawSize;
			Vector3 s3 = new Vector3(turretTopDrawSize, 1f, turretTopDrawSize);
			Vector3 pos2 = vector;
			pos2.y = num + 0.1f;
			Matrix4x4 matrix3 = default(Matrix4x4);
			matrix3.SetTRS(pos2, q, s3);
			Graphics.DrawMesh(MeshPool.plane10, matrix3, gunBaseGraphic.MatAt(base.Rotation), 0);
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
		return (Thing)AttackTargetFinderAngle.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, turretOrientation, IsValidTarget);
	}
}
