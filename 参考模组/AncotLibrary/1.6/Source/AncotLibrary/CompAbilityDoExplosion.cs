using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityDoExplosion : CompAbilityEffect
{
	public new CompProperties_AbilityDoExplosion Props => (CompProperties_AbilityDoExplosion)props;

	public virtual float RadiusBase => Props.radius;

	public virtual int DamageAmount => Props.damageAmount;

	public virtual float ArmorPenetration => Props.armorPenetration;

	public Pawn Caster => parent.pawn;

	public override IEnumerable<PreCastAction> GetPreCastActions()
	{
		yield return new PreCastAction
		{
			action = delegate
			{
				if (Props.warmupEffect != null)
				{
					parent.AddEffecterToMaintain(Props.warmupEffect.Spawn(parent.pawn.Position, parent.pawn.Map), parent.pawn.Position, Props.warmupEffectMaintainTicks, parent.pawn.Map);
				}
			},
			ticksAwayFromCast = Props.warmupEffectMaintainTicks
		};
	}

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (Props.targetOnCaster)
		{
			target = Caster;
		}
		Map map = target.Thing.Map;
		if (Props.explosionEffect != null)
		{
			Effecter effecter = Props.explosionEffect.Spawn();
			effecter.Trigger(new TargetInfo(target.Thing.PositionHeld, map), new TargetInfo(target.Thing.PositionHeld, map));
			effecter.Cleanup();
		}
		GenExplosion.DoExplosion(target.Thing.PositionHeld, map, RadiusBase, Props.damageDef, Caster, DamageAmount, ArmorPenetration, Props.explosionSound, null, null, null, Props.postExplosionSpawnThingDef, Props.postExplosionSpawnChance, Props.postExplosionSpawnThingCount, Props.postExplosionGasType, null, 255, Props.applyDamageToExplosionCellsNeighbors, Props.preExplosionSpawnThingDef, Props.preExplosionSpawnChance, Props.preExplosionSpawnThingCount, Props.chanceToStartFire, Props.damageFalloff, null, null, null, Props.doVisualEffects, doSoundEffects: Props.doSoundEffects, propagationSpeed: Props.propagationSpeed);
	}
}
