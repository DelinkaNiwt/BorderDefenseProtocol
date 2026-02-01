using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class Building_ManualAimTurret : Building_TurretGun
{
	private static readonly FieldInfo holdFireField = typeof(Building_TurretGun).GetField("holdFire", BindingFlags.Instance | BindingFlags.NonPublic);

	private bool manualAimMode = false;

	private float manualRotation;

	private const float MouseFollowSpeed = 0.2f;

	private int manualShootCooldown = 0;

	private const int MaxShootCooldown = 60;

	private ThingDef bulletDef = DefDatabase<ThingDef>.GetNamed("Bullet_AA");

	private void SetHoldFire(bool value)
	{
		holdFireField.SetValue(this, value);
	}

	private void SetTurretRotationDirectly(float rotation)
	{
		top.CurRotation = rotation;
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		SetHoldFire(value: true);
		manualRotation = base.Rotation.AsAngle;
		SetTurretRotationDirectly(manualRotation);
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		SetTurretRotationDirectly(manualRotation);
		base.DrawAt(drawLoc, flip);
	}

	protected override void Tick()
	{
		base.Tick();
		if (manualShootCooldown > 0)
		{
			manualShootCooldown--;
		}
		if (manualAimMode && base.Map != null && Find.Selector.SelectedObjects.Contains(this))
		{
			Vector3 currentMousePos = UI.MouseMapPosition();
			Vector3 turretPos = DrawPos;
			Vector2 relativePos = new Vector2(currentMousePos.x - turretPos.x, turretPos.z - currentMousePos.z);
			manualRotation = Mathf.Atan2(relativePos.y, relativePos.x) * 57.29578f + 90f;
			if (manualRotation < 0f)
			{
				manualRotation += 360f;
			}
			if (manualRotation > 360f)
			{
				manualRotation -= 360f;
			}
			SetTurretRotationDirectly(manualRotation);
			if (KeyBindingDefOf.Misc1.IsDown && manualShootCooldown <= 0)
			{
				ManualShoot();
			}
		}
		SetHoldFire(value: true);
	}

	public void ManualShoot()
	{
		if (bulletDef == null || manualShootCooldown > 0 || !base.Active || base.IsStunned)
		{
			return;
		}
		Vector3 shootDirection = Quaternion.AngleAxis(manualRotation, Vector3.up) * Vector3.forward;
		Vector3 startPos = DrawPos + new Vector3(0f, 1f, 0f);
		IntVec3 startCell = startPos.ToIntVec3();
		IntVec3 endCell = FindMapEdgeTarget(startCell, shootDirection);
		if (!startCell.InBounds(base.Map) || !endCell.InBounds(base.Map))
		{
			return;
		}
		Projectile projectile = (Projectile)ThingMaker.MakeThing(bulletDef);
		if (projectile != null)
		{
			GenSpawn.Spawn(projectile, startCell, base.Map);
			projectile.Launch(this, startPos, new LocalTargetInfo(endCell), new LocalTargetInfo(endCell), ProjectileHitFlags.All);
			manualShootCooldown = 60;
			if (base.Spawned)
			{
				FleckMaker.ThrowDustPuff(startPos, base.Map, 1f);
				FleckMaker.Static(startPos, base.Map, FleckDefOf.ShotFlash);
			}
		}
	}

	private IntVec3 FindMapEdgeTarget(IntVec3 startCell, Vector3 direction)
	{
		Map map = base.Map;
		if (map == null)
		{
			return startCell;
		}
		direction.Normalize();
		float maxDistance = Mathf.Sqrt(map.Size.x * map.Size.x + map.Size.z * map.Size.z);
		IntVec3 currentCell = startCell;
		IntVec3 lastValidCell = startCell;
		for (float distance = 1f; distance <= maxDistance; distance += 1f)
		{
			Vector3 currentPos = startCell.ToVector3() + direction * distance;
			IntVec3 testCell = currentPos.ToIntVec3();
			if (!testCell.InBounds(map))
			{
				return lastValidCell;
			}
			lastValidCell = testCell;
		}
		return lastValidCell;
	}

	public void ToggleManualAimMode()
	{
		manualAimMode = !manualAimMode;
		if (manualAimMode)
		{
			Messages.Message("ManualAimTurret_AimEnabled".Translate(), MessageTypeDefOf.NeutralEvent);
		}
		else
		{
			Messages.Message("ManualAimTurret_AimDisabled".Translate(), MessageTypeDefOf.NeutralEvent);
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		List<Gizmo> baseGizmos = new List<Gizmo>();
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			baseGizmos.Add(gizmo);
		}
		for (int i = baseGizmos.Count - 1; i >= 0; i--)
		{
			if (baseGizmos[i] is Command_Toggle toggle && toggle.defaultLabel == "CommandHoldFire".Translate())
			{
				baseGizmos.RemoveAt(i);
			}
		}
		foreach (Gizmo item in baseGizmos)
		{
			yield return item;
		}
		yield return new Command_Toggle
		{
			defaultLabel = (manualAimMode ? "ManualAimTurret_DisableAim".Translate() : "ManualAimTurret_EnableAim".Translate()),
			defaultDesc = "ManualAimTurret_AimDesc".Translate(),
			icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
			isActive = () => manualAimMode,
			toggleAction = ToggleManualAimMode
		};
		if (manualAimMode)
		{
			Command_Action shootCommand = new Command_Action
			{
				defaultLabel = "ManualAimTurret_Shoot".Translate(),
				defaultDesc = "ManualAimTurret_ShootDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/Shoot"),
				action = ManualShoot
			};
			if (manualShootCooldown > 0)
			{
				shootCommand.Disable("ManualAimTurret_Cooldown".Translate(manualShootCooldown.ToStringSecondsFromTicks()));
			}
			yield return shootCommand;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref manualAimMode, "manualAimMode", defaultValue: false);
		Scribe_Values.Look(ref manualRotation, "manualRotation", 0f);
		Scribe_Values.Look(ref manualShootCooldown, "manualShootCooldown", 0);
	}
}
