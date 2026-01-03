using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_CMCTurretGun_AAAS : Building_CMCTurretGun
{
	public bool AAMode = false;

	public override LocalTargetInfo CurrentTarget => currentTargetInt;

	public override bool CanSetForcedTarget => false;

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Vector3 zero = Vector3.zero;
		turrettop.DrawTurret(drawLoc, zero);
		base.DrawAt(drawLoc, flip);
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		IEnumerable<StatDrawEntry> enumerable = base.SpecialDisplayStats();
		if (enumerable != null)
		{
			foreach (StatDrawEntry item in enumerable)
			{
				yield return item;
			}
		}
		List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
		for (int i = 0; i < allVerbs.Count; i++)
		{
			Verb verb = allVerbs[i];
			if (verb is Verb_ShootMultiTarget)
			{
				Verb_ShootMultiTarget verb_ShootMultiTarget = (Verb_ShootMultiTarget)verb;
				yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatShootNum_Label".Translate(), "StatShootNum_Desc".Translate(verb_ShootMultiTarget.ShootNum * verb_ShootMultiTarget.verbProps.burstShotCount), "StatShootNum_Text".Translate(verb_ShootMultiTarget.ShootNum * verb_ShootMultiTarget.verbProps.burstShotCount), 25);
			}
		}
	}

	public Building_CMCTurretGun_AAAS()
	{
		turrettop = new CMCTurretTop(this);
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (base.CanExtractShell)
		{
			CompChangeableProjectile compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
			yield return new Command_Action
			{
				defaultLabel = "CommandExtractShell".Translate(),
				defaultDesc = "CommandExtractShellDesc".Translate(),
				icon = compChangeableProjectile.LoadedShell.uiIcon,
				iconAngle = compChangeableProjectile.LoadedShell.uiIconAngle,
				iconOffset = compChangeableProjectile.LoadedShell.uiIconOffset,
				iconDrawScale = GenUI.IconDrawScale(compChangeableProjectile.LoadedShell),
				action = delegate
				{
					ExtractShell();
				}
			};
		}
		CompChangeableProjectile compChangeableProjectile2 = gun.TryGetComp<CompChangeableProjectile>();
		if (compChangeableProjectile2 != null)
		{
			StorageSettings storeSettings = compChangeableProjectile2.GetStoreSettings();
			foreach (Gizmo item in StorageSettingsClipboard.CopyPasteGizmosFor(storeSettings))
			{
				yield return item;
			}
		}
		if (!base.CanToggleHoldFire)
		{
			yield break;
		}
		yield return new Command_Toggle
		{
			defaultLabel = "CommandHoldFire".Translate(),
			defaultDesc = "CommandHoldFireDesc".Translate(),
			icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire"),
			hotKey = KeyBindingDefOf.Misc6,
			toggleAction = delegate
			{
				holdFire = !holdFire;
				if (holdFire)
				{
					ResetForcedTarget();
				}
			},
			isActive = () => holdFire
		};
	}

	public override void PostMake()
	{
		base.PostMake();
		burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();
		MakeGun();
	}

	protected override void BurstComplete()
	{
		currentTargetInt = null;
		base.BurstComplete();
	}

	protected override void Tick()
	{
		if (base.Active && !base.IsStunned && base.Spawned)
		{
			base.GunCompEq.verbTracker.VerbsTick();
			if (AttackVerb.state != VerbState.Bursting)
			{
				turrettop.TurretTopTick();
				if (currentTargetInt != null && currentTargetInt.Thing != null && turrettop.CurRotation == turrettop.DestRotation)
				{
					if (burstCooldownTicksLeft > 0)
					{
						burstCooldownTicksLeft--;
					}
					else
					{
						BeginBurst();
					}
				}
				else if (burstCooldownTicksLeft > 0)
				{
					burstCooldownTicksLeft--;
				}
			}
			if (currentTargetInt.Thing.DestroyedOrNull())
			{
				currentTargetInt = null;
			}
		}
		else
		{
			currentTargetInt = null;
		}
	}
}
