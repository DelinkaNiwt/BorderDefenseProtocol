using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCLProjectiles;

public class ProjectileEffectTracker : IExposable
{
	public Thing parent;

	public int ticksSinceLaunch;

	public int parentDuration;

	public Vector3 origin = Vector3.zero;

	public Vector3 destination = Vector3.zero;

	public Vector3 fullVector = Vector3.zero;

	public Quaternion destinationRotation = Quaternion.identity;

	public Vector3 previousExactPosition = Vector3.zero;

	public Vector3 previousVisualPosition = Vector3.zero;

	public float previousVisualHeight;

	public Vector3 currentExactPosition = Vector3.zero;

	public Vector3 currentVisualPosition = Vector3.zero;

	public float currentVisualHeight;

	public float currentVisualAngle;

	public Quaternion currentVisualRotation = Quaternion.identity;

	public ModExtension_ProjectileEffects effectExtension;

	public Func<float, float> progress;

	public Func<float, float> height;

	public float arcFactor;

	public Func<float, float> lateralOffset;

	public float lateralOffsetMagnitude;

	public Projectile Projectile => parent as Projectile;

	public ProjectileEffectTracker(Thing parent)
	{
		this.parent = parent;
	}

	public void PostSpawnSetup(Map map, bool respawningAfterLoad)
	{
		arcFactor = parent.def.projectile?.arcHeightFactor ?? 0f;
		effectExtension = parent.def.GetModExtension<ModExtension_ProjectileEffects>();
		if (effectExtension == null)
		{
			if (arcFactor > 0f)
			{
				height = AnimationUtility.Sine;
			}
			return;
		}
		progress = effectExtension.progress;
		height = effectExtension.height;
		lateralOffset = effectExtension.lateralOffset;
		if (!respawningAfterLoad)
		{
			lateralOffsetMagnitude = effectExtension.lateralOffsetMagnitude.RandomInRange;
			if (lateralOffsetMagnitude != 0f && Rand.Chance(effectExtension.lateralOffsetMirrorChance))
			{
				lateralOffsetMagnitude *= -1f;
			}
		}
	}

	public void PreLaunch(Thing equipment, ref Vector3 origin, Vector3 destination)
	{
		if (origin == destination)
		{
			return;
		}
		if (equipment is WeaponWithAttachments { AttachmentExtension: { } attachmentExtension } weaponWithAttachments)
		{
			if (effectExtension.originAttachment != null)
			{
				origin = weaponWithAttachments.GetAttachmentPosition(effectExtension.originAttachment, effectExtension.originAttachmentIndex, effectExtension.randomizeOriginAttachment);
			}
			ProjectileUtility.ModifyOriginVector(ref origin, destination, attachmentExtension.GetOriginOffsetFor(equipment), attachmentExtension.alignOriginOffsetWithDirection, attachmentExtension.originDistance, weaponWithAttachments.pawnScaleFactor);
		}
		if (effectExtension == null)
		{
			return;
		}
		if (effectExtension.alignOriginWithDrawPos)
		{
			Vector3? drawPosHeld = equipment.DrawPosHeld;
			if (drawPosHeld.HasValue)
			{
				Vector3 valueOrDefault = drawPosHeld.GetValueOrDefault();
				origin = valueOrDefault;
			}
		}
		ProjectileUtility.ModifyOriginVector(ref origin, destination, effectExtension.GetOriginOffsetFor(equipment), effectExtension.alignOriginOffsetWithDirection, effectExtension.originDistance);
	}

	public void PostLaunch(Vector3 origin, Vector3 destination, bool calculatePositionImmediately = true)
	{
		this.origin = origin;
		this.destination = destination;
		fullVector = (destination - origin).Yto0();
		destinationRotation = ((fullVector == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(fullVector));
		if (calculatePositionImmediately)
		{
			ProjectileUtility.CalculateExactPosition(parent, this, origin, destination, 0f);
			ProjectileUtility.CalculateExactRotation(parent, this, effectExtension, origin, destination, 0f);
		}
		if (effectExtension != null)
		{
			if (effectExtension.useVariableHeightFactor)
			{
				arcFactor = (origin - destination).Yto0().MagnitudeHorizontal() * effectExtension.heightFactorMagnitude.RandomInRange;
			}
			Map map = parent.Map;
			if (map != null && !effectExtension.launchEffects.NullOrEmpty())
			{
				EffectMapComponent effectMapComponent = map.EccentricProjectilesEffectComp();
				if (effectMapComponent != null)
				{
					foreach (EffectDef launchEffect in effectExtension.launchEffects)
					{
						effectMapComponent.CreateEffect(new EffectContext(map, launchEffect)
						{
							anchor = parent,
							position = origin,
							origin = origin,
							destination = destination,
							rotation = destinationRotation,
							angle = destinationRotation.eulerAngles.y,
							parentDuration = parentDuration
						});
					}
				}
			}
		}
		if (arcFactor > 0f && height == null)
		{
			height = AnimationUtility.Sine;
		}
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref ticksSinceLaunch, "ticksSinceLaunch", 0);
		Scribe_Values.Look(ref arcFactor, "arcFactor", 0f);
		Scribe_Values.Look(ref lateralOffsetMagnitude, "lateralOffsetMagnitude", 0f);
		Scribe_Values.Look(ref previousExactPosition, "previousExactPosition");
		Scribe_Values.Look(ref previousVisualPosition, "previousVisualPosition");
		Scribe_Values.Look(ref previousVisualHeight, "previousVisualHeight", 0f);
		Scribe_Values.Look(ref currentExactPosition, "currentExactPosition");
		Scribe_Values.Look(ref currentVisualPosition, "currentVisualPosition");
		Scribe_Values.Look(ref currentVisualHeight, "currentVisualHeight", 0f);
	}

	public void PreTick(Vector3 origin, Vector3 destination)
	{
		this.origin = origin;
		this.destination = destination;
		fullVector = (destination - origin).Yto0();
		destinationRotation = ((fullVector == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(fullVector));
		previousExactPosition = currentExactPosition;
		previousVisualPosition = currentVisualPosition;
		previousVisualHeight = currentVisualHeight;
	}

	public void Tick(Map map, ModExtension_ProjectileEffects extension)
	{
		if (map == null || extension == null || extension.effects.NullOrEmpty())
		{
			return;
		}
		EffectMapComponent effectMapComponent = map.EccentricProjectilesEffectComp();
		if (effectMapComponent == null)
		{
			return;
		}
		foreach (EffectDef effect in extension.effects)
		{
			if (effect.ShouldBeActive(ticksSinceLaunch))
			{
				effectMapComponent.CreateEffect(new EffectContext(map, effect)
				{
					anchor = parent,
					destinationAnchor = null,
					position = previousVisualPosition,
					origin = origin,
					destination = currentVisualPosition,
					rotation = currentVisualRotation,
					angle = currentVisualAngle,
					parentDuration = parentDuration,
					parentTicksElapsed = ticksSinceLaunch
				});
			}
		}
	}

	public void PostTick(int interval = 1)
	{
		ticksSinceLaunch += interval;
	}

	public void Impact(Map map, Thing hitThing, bool blockedByShield = false)
	{
		ModExtension_ProjectileEffects modExtension_ProjectileEffects = effectExtension;
		if (modExtension_ProjectileEffects == null || !modExtension_ProjectileEffects.hasImpactEffects)
		{
			return;
		}
		EffectMapComponent effectMapComponent = map?.EccentricProjectilesEffectComp();
		if (effectMapComponent == null)
		{
			return;
		}
		if (!effectExtension.impactEffects.NullOrEmpty())
		{
			float y = currentVisualAngle;
			Quaternion rotation = currentVisualRotation;
			Vector3 vector = currentExactPosition - previousVisualPosition;
			if (vector != Vector3.zero)
			{
				rotation = Quaternion.LookRotation(vector);
				y = rotation.eulerAngles.y;
			}
			foreach (EffectDef impactEffect in effectExtension.impactEffects)
			{
				if (impactEffect != null && (!blockedByShield || impactEffect.drawIfIntercepted))
				{
					effectMapComponent.CreateEffect(new EffectContext(map, impactEffect)
					{
						anchor = null,
						destinationAnchor = hitThing,
						position = currentVisualPosition,
						origin = destination,
						destination = destination,
						rotation = rotation,
						angle = y
					});
				}
			}
		}
		Projectile?.def?.projectile?.soundImpact?.PlayOneShot(new TargetInfo(hitThing?.Position ?? destination.ToIntVec3(), map));
		if (effectExtension.returnEffects.NullOrEmpty())
		{
			return;
		}
		Quaternion rotation2 = Quaternion.LookRotation(origin - destination);
		float y2 = rotation2.eulerAngles.y;
		foreach (EffectDef returnEffect in effectExtension.returnEffects)
		{
			if (returnEffect != null && (!blockedByShield || returnEffect.drawIfIntercepted))
			{
				effectMapComponent.CreateEffect(new EffectContext(map, returnEffect)
				{
					anchor = null,
					position = currentVisualPosition,
					origin = currentVisualPosition,
					destination = origin,
					rotation = rotation2,
					angle = y2
				});
			}
		}
	}
}
