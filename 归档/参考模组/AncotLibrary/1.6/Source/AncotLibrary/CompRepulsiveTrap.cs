using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class CompRepulsiveTrap : ThingComp
{
	public string signalTag;

	public bool wickStarted;

	public int wickTicksLeft;

	private Thing instigator;

	private int countdownTicksLeft = -1;

	public bool destroyedThroughDetonation;

	private List<Thing> thingsIgnoredByExplosion;

	public float? customExplosiveRadius;

	protected Sustainer wickSoundSustainer;

	private OverlayHandle? overlayBurningWick;

	public CompProperties_RepulsiveTrap Props => (CompProperties_RepulsiveTrap)props;

	protected int StartWickThreshold => Mathf.RoundToInt(Props.startWickHitPointsPercent * (float)parent.MaxHitPoints);

	private bool CanEverExplodeFromDamage
	{
		get
		{
			if (Props.chanceNeverExplodeFromDamage < 1E-05f)
			{
				return true;
			}
			Rand.PushState();
			Rand.Seed = parent.thingIDNumber.GetHashCode();
			bool result = Rand.Value > Props.chanceNeverExplodeFromDamage;
			Rand.PopState();
			return result;
		}
	}

	public void AddThingsIgnoredByExplosion(List<Thing> things)
	{
		if (thingsIgnoredByExplosion == null)
		{
			thingsIgnoredByExplosion = new List<Thing>();
		}
		thingsIgnoredByExplosion.AddRange(things);
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_References.Look(ref instigator, "instigator");
		Scribe_Collections.Look(ref thingsIgnoredByExplosion, "thingsIgnoredByExplosion", LookMode.Reference);
		Scribe_Values.Look(ref wickStarted, "wickStarted", defaultValue: false);
		Scribe_Values.Look(ref wickTicksLeft, "wickTicksLeft", 0);
		Scribe_Values.Look(ref destroyedThroughDetonation, "destroyedThroughDetonation", defaultValue: false);
		Scribe_Values.Look(ref countdownTicksLeft, "countdownTicksLeft", 0);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (Props.countdownTicks.HasValue)
		{
			countdownTicksLeft = Props.countdownTicks.Value.RandomInRange;
		}
		UpdateOverlays();
	}

	public override void CompTick()
	{
		if (countdownTicksLeft > 0)
		{
			countdownTicksLeft--;
			if (countdownTicksLeft == 0)
			{
				StartWick();
				countdownTicksLeft = -1;
			}
		}
		if (!wickStarted)
		{
			return;
		}
		if (wickSoundSustainer == null)
		{
			StartWickSustainer();
		}
		else
		{
			wickSoundSustainer.Maintain();
		}
		if (Props.wickMessages != null)
		{
			foreach (WickMessage wickMessage in Props.wickMessages)
			{
				if (wickMessage.ticksLeft == wickTicksLeft && wickMessage.wickMessagekey != null)
				{
					Messages.Message(wickMessage.wickMessagekey.Translate(parent.GetCustomLabelNoCount(includeHp: false), wickTicksLeft.ToStringSecondsFromTicks()), parent, wickMessage.messageType ?? MessageTypeDefOf.NeutralEvent, historical: false);
				}
			}
		}
		wickTicksLeft--;
		if (wickTicksLeft <= 0)
		{
			Detonate(parent.MapHeld);
		}
	}

	private void StartWickSustainer()
	{
		SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(parent.PositionHeld, parent.MapHeld));
		SoundInfo info = SoundInfo.InMap(parent, MaintenanceType.PerTick);
		wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
	}

	private void EndWickSustainer()
	{
		if (wickSoundSustainer != null)
		{
			wickSoundSustainer.End();
			wickSoundSustainer = null;
		}
	}

	private void UpdateOverlays()
	{
		if (parent.Spawned && Props.drawWick)
		{
			parent.Map.overlayDrawer.Disable(parent, ref overlayBurningWick);
			if (wickStarted)
			{
				overlayBurningWick = parent.Map.overlayDrawer.Enable(parent, OverlayTypes.BurningWick);
			}
		}
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		if (mode == DestroyMode.KillFinalize && Props.explodeOnKilled)
		{
			Detonate(previousMap, ignoreUnspawned: true);
		}
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		absorbed = false;
		if (!CanEverExplodeFromDamage)
		{
			return;
		}
		if (dinfo.Def.ExternalViolenceFor(parent) && dinfo.Amount >= (float)parent.HitPoints && CanExplodeFromDamageType(dinfo.Def))
		{
			if (parent.MapHeld != null)
			{
				instigator = dinfo.Instigator;
				Detonate(parent.MapHeld);
				if (parent.Destroyed)
				{
					absorbed = true;
				}
			}
		}
		else if (!wickStarted && Props.startWickOnDamageTaken != null && Props.startWickOnDamageTaken.Contains(dinfo.Def))
		{
			StartWick(dinfo.Instigator);
		}
	}

	public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		if (CanEverExplodeFromDamage && CanExplodeFromDamageType(dinfo.Def) && !parent.Destroyed)
		{
			if (wickStarted && dinfo.Def == DamageDefOf.Stun)
			{
				StopWick();
			}
			else if (!wickStarted && parent.HitPoints <= StartWickThreshold && (dinfo.Def.ExternalViolenceFor(parent) || (!Props.startWickOnInternalDamageTaken.NullOrEmpty() && Props.startWickOnInternalDamageTaken.Contains(dinfo.Def))))
			{
				StartWick(dinfo.Instigator);
			}
		}
	}

	public void StartWick(Thing instigator = null)
	{
		if (!wickStarted)
		{
			this.instigator = instigator;
			wickStarted = true;
			wickTicksLeft = Props.wickTicks.RandomInRange;
			StartWickSustainer();
			UpdateOverlays();
		}
	}

	public void StopWick()
	{
		wickStarted = false;
		instigator = null;
		UpdateOverlays();
	}

	private bool IsPawnAffected(Pawn target)
	{
		if (target.Dead || target.health == null || target.Downed || !target.Spawned)
		{
			return false;
		}
		if (target.Position.DistanceTo(parent.Position) <= Props.range)
		{
			if (Props.onlyTargetHostile)
			{
				return target.HostileTo(parent.Faction);
			}
			return true;
		}
		return false;
	}

	protected void Detonate(Map map, bool ignoreUnspawned = false)
	{
		foreach (Pawn item in parent.Map.mapPawns.AllPawnsSpawned)
		{
			if (IsPawnAffected(item))
			{
				ForceMovementUtility.ApplyRepulsiveForce(parent.PositionHeld, item, Props.distance, Props.removeHediffsAffected, ignoreResistance: false, Props.fieldStrength);
			}
		}
		if (!ignoreUnspawned && !parent.SpawnedOrAnyParentSpawned)
		{
			return;
		}
		if (!parent.Destroyed)
		{
			destroyedThroughDetonation = true;
			if (Props.signalTag != null)
			{
				Find.SignalManager.SendSignal(new Signal(Props.signalTag, parent.Named("SUBJECT")));
			}
			parent.Kill();
		}
		CompProperties_RepulsiveTrap compProperties_RepulsiveTrap = Props;
		EndWickSustainer();
		wickStarted = false;
		if (map == null)
		{
			Log.Warning("Tried to detonate CompRepulsiveTrap in a null map.");
		}
		else if (compProperties_RepulsiveTrap.explosionEffect != null)
		{
			Effecter effecter = compProperties_RepulsiveTrap.explosionEffect.Spawn();
			effecter.Trigger(new TargetInfo(parent.PositionHeld, map), new TargetInfo(parent.PositionHeld, map));
			effecter.Cleanup();
		}
	}

	private bool CanExplodeFromDamageType(DamageDef damage)
	{
		if (Props.requiredDamageTypeToExplode != null)
		{
			return Props.requiredDamageTypeToExplode == damage;
		}
		return true;
	}

	public override string CompInspectStringExtra()
	{
		string text = "";
		if (countdownTicksLeft != -1)
		{
			text += "DetonationCountdown".Translate(countdownTicksLeft.TicksToDays().ToString("0.0"));
		}
		if (Props.extraInspectStringKey != null)
		{
			text += ((text != "") ? "\n" : "") + Props.extraInspectStringKey.Translate();
		}
		return text;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (countdownTicksLeft > 0)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Trigger countdown",
				action = delegate
				{
					countdownTicksLeft = 1;
				}
			};
		}
	}
}
