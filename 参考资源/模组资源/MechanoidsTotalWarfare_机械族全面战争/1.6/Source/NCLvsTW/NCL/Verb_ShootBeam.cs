using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class Verb_ShootBeam : Verb
{
	private List<Vector3> path = new List<Vector3>();

	private List<Vector3> tmpPath = new List<Vector3>();

	private int ticksToNextPathStep;

	private Vector3 initialTargetPosition;

	private Vector3 LasttargetPosition;

	private MoteDualAttached mote;

	private Effecter endEffecter;

	private Sustainer sustainer;

	private HashSet<IntVec3> pathCells = new HashSet<IntVec3>();

	private HashSet<IntVec3> tmpPathCells = new HashSet<IntVec3>();

	private HashSet<IntVec3> tmpHighlightCells = new HashSet<IntVec3>();

	private HashSet<IntVec3> tmpSecondaryHighlightCells = new HashSet<IntVec3>();

	private HashSet<IntVec3> hitCells = new HashSet<IntVec3>();

	private const int NumSubdivisionsPerUnitLength = 1;

	public float ShotProgress => (float)ticksToNextPathStep / (float)base.TicksBetweenBurstShots;

	private int min => Mathf.RoundToInt(verbProps.minRange);

	private DamageExtension modExtention => base.EquipmentSource?.def.GetModExtension<DamageExtension>();

	public Vector3 InterpolatedPosition
	{
		get
		{
			Vector3 b;
			if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
			{
				b = LasttargetPosition - initialTargetPosition;
			}
			else
			{
				LasttargetPosition = base.CurrentTarget.CenterVector3;
				b = LasttargetPosition - initialTargetPosition;
			}
			return Vector3.Lerp(path[Mathf.Max(burstShotsLeft - min, 0)], path[Mathf.Min(Mathf.Max(burstShotsLeft + 1 - min, 1), path.Count - 1 - min)], ShotProgress) + b;
		}
	}

	public override float? AimAngleOverride => (state == VerbState.Bursting) ? new float?((InterpolatedPosition - caster.DrawPos).AngleFlat()) : ((float?)null);

	public override void DrawHighlight(LocalTargetInfo target)
	{
		base.DrawHighlight(target);
		if (target == null)
		{
			return;
		}
		CalculatePath(target.CenterVector3, tmpPath, tmpPathCells, addRandomOffset: false);
		foreach (IntVec3 targetCell in tmpPathCells)
		{
			ShootLine shootLine;
			bool flag2 = TryFindShootLineFromTo(caster.Position, target, out shootLine);
			IntVec3 intVec = default(IntVec3);
			if ((verbProps.stopBurstWithoutLos && !flag2) || !TryGetHitCell(shootLine.Source, targetCell, out intVec))
			{
				continue;
			}
			foreach (IntVec3 item in GenRadial.RadialCellsAround(intVec, 2f, useCenter: true).InRandomOrder())
			{
				tmpHighlightCells.Add(item);
			}
			if (!verbProps.beamHitsNeighborCells)
			{
				continue;
			}
			foreach (IntVec3 intVec2 in GetBeamHitNeighbourCells(shootLine.Source, intVec))
			{
				if (tmpHighlightCells.Contains(intVec2))
				{
					continue;
				}
				foreach (IntVec3 item2 in GenRadial.RadialCellsAround(intVec2, 2f, useCenter: true).InRandomOrder())
				{
					tmpSecondaryHighlightCells.Add(item2);
				}
			}
		}
		tmpSecondaryHighlightCells.RemoveWhere((IntVec3 x) => tmpHighlightCells.Contains(x));
		if (tmpHighlightCells.Any())
		{
			GenDraw.DrawFieldEdges(tmpHighlightCells.ToList(), verbProps.highlightColor ?? Color.white);
		}
		if (tmpSecondaryHighlightCells.Any())
		{
			GenDraw.DrawFieldEdges(tmpSecondaryHighlightCells.ToList(), verbProps.secondaryHighlightColor ?? Color.white);
		}
		tmpHighlightCells.Clear();
		tmpSecondaryHighlightCells.Clear();
	}

	protected override bool TryCastShot()
	{
		ShootLine shootLine;
		bool flag = TryFindShootLineFromTo(caster.Position, currentTarget, out shootLine);
		if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
		{
			shootLine = new ShootLine(caster.Position, LasttargetPosition.ToIntVec3());
		}
		if (verbProps.stopBurstWithoutLos && !flag)
		{
			return false;
		}
		if (base.EquipmentSource != null)
		{
			base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
			base.EquipmentSource.GetComp<CompApparelReloadable>()?.UsedOnce();
		}
		lastShotTick = Find.TickManager.TicksGame;
		ticksToNextPathStep = base.TicksBetweenBurstShots;
		IntVec3 targetCell = InterpolatedPosition.Yto0().ToIntVec3();
		if (!TryGetHitCell(shootLine.Source, targetCell, out var intVec))
		{
			return true;
		}
		HitCell(intVec, shootLine.Source);
		if (verbProps.beamHitsNeighborCells)
		{
			hitCells.Add(intVec);
			foreach (IntVec3 intVec2 in GetBeamHitNeighbourCells(shootLine.Source, intVec))
			{
				if (!hitCells.Contains(intVec2))
				{
					float damageFactor = (pathCells.Contains(intVec2) ? 1f : 0.5f);
					HitCell(intVec2, shootLine.Source, damageFactor);
					hitCells.Add(intVec2);
				}
			}
		}
		return true;
	}

	protected bool TryGetHitCell(IntVec3 source, IntVec3 targetCell, out IntVec3 hitCell)
	{
		IntVec3 intVec = GenSight.LastPointOnLineOfSight(source, targetCell, (IntVec3 c) => c.InBounds(caster.Map) && c.CanBeSeenOverFast(caster.Map), skipFirstCell: true);
		if (verbProps.beamCantHitWithinMinRange && intVec.DistanceTo(source) < verbProps.minRange)
		{
			hitCell = default(IntVec3);
			return false;
		}
		hitCell = (intVec.IsValid ? intVec : targetCell);
		return intVec.IsValid;
	}

	protected IntVec3 GetHitCell(IntVec3 source, IntVec3 targetCell)
	{
		TryGetHitCell(source, targetCell, out var result);
		return result;
	}

	protected IEnumerable<IntVec3> GetBeamHitNeighbourCells(IntVec3 source, IntVec3 pos)
	{
		if (!verbProps.beamHitsNeighborCells)
		{
			yield break;
		}
		for (int i = 0; i < 4; i++)
		{
			IntVec3 intVec = pos + GenAdj.CardinalDirections[i];
			if (intVec.InBounds(Caster.Map) && (!verbProps.beamHitsNeighborCellsRequiresLOS || GenSight.LineOfSight(source, intVec, caster.Map)))
			{
				yield return intVec;
			}
		}
	}

	public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
	{
		return base.TryStartCastOn(verbProps.beamTargetsGround ? ((LocalTargetInfo)castTarg.Cell) : castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
	}

	public override void BurstingTick()
	{
		ticksToNextPathStep--;
		Vector3 vector = InterpolatedPosition;
		IntVec3 intVec = vector.ToIntVec3();
		Vector3 vector2 = InterpolatedPosition - caster.Position.ToVector3Shifted();
		float num = vector2.MagnitudeHorizontal();
		Vector3 normalized = vector2.Yto0().normalized;
		IntVec3 b = GenSight.LastPointOnLineOfSight(caster.Position, intVec, (IntVec3 c) => c.CanBeSeenOverFast(caster.Map), skipFirstCell: true);
		if (b.IsValid)
		{
			num -= (intVec - b).LengthHorizontal;
			vector = caster.Position.ToVector3Shifted() + normalized * num;
			intVec = vector.ToIntVec3();
		}
		Vector3 offsetA = normalized * verbProps.beamStartOffset;
		Vector3 vector3 = vector - intVec.ToVector3Shifted();
		if (mote != null)
		{
			mote.UpdateTargets(new TargetInfo(caster.Position, caster.Map), new TargetInfo(intVec, caster.Map), offsetA, vector3);
			mote.Maintain();
		}
		if (caster.IsHashIntervalTick(base.TicksBetweenBurstShots) && base.EquipmentSource != null && modExtention != null && modExtention.circleMote != null)
		{
			float rotation = normalized.AngleFlat();
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, caster.Map, modExtention.circleMote);
			dataStatic.rotation = rotation;
			caster.Map.flecks.CreateFleck(dataStatic);
		}
		if (verbProps.beamGroundFleckDef != null && Rand.Chance(verbProps.beamFleckChancePerTick))
		{
			FleckMaker.Static(vector, caster.Map, verbProps.beamGroundFleckDef);
		}
		if (endEffecter == null && verbProps.beamEndEffecterDef != null)
		{
			endEffecter = verbProps.beamEndEffecterDef.Spawn(intVec, caster.Map, vector3);
		}
		if (endEffecter != null)
		{
			endEffecter.offset = vector3;
			endEffecter.EffectTick(new TargetInfo(intVec, caster.Map), TargetInfo.Invalid);
			endEffecter.ticksLeft--;
		}
		if (verbProps.beamLineFleckDef != null)
		{
			float num2 = 1f * num;
			for (int num3 = 0; (float)num3 < num2; num3++)
			{
				if (Rand.Chance(verbProps.beamLineFleckChanceCurve.Evaluate((float)num3 / num2)))
				{
					Vector3 b2 = num3 * normalized - normalized * Rand.Value + normalized / 2f;
					FleckMaker.Static(caster.Position.ToVector3Shifted() + b2, caster.Map, verbProps.beamLineFleckDef);
				}
			}
		}
		this.sustainer?.Maintain();
	}

	public override void WarmupComplete()
	{
		state = VerbState.Bursting;
		initialTargetPosition = currentTarget.CenterVector3;
		CalculatePath(currentTarget.CenterVector3, path, pathCells);
		burstShotsLeft = path.Count - 1;
		hitCells.Clear();
		if (verbProps.beamMoteDef != null)
		{
			mote = MoteMaker.MakeInteractionOverlay(verbProps.beamMoteDef, caster, new TargetInfo(path[0].ToIntVec3(), caster.Map));
		}
		TryCastNextBurstShot();
		ticksToNextPathStep = base.TicksBetweenBurstShots;
		endEffecter?.Cleanup();
		if (verbProps.soundCastBeam != null)
		{
			sustainer = verbProps.soundCastBeam.TrySpawnSustainer(SoundInfo.InMap(caster, MaintenanceType.PerTick));
		}
	}

	private void CalculatePath(Vector3 target, List<Vector3> pathList, HashSet<IntVec3> pathCellsList, bool addRandomOffset = true)
	{
		pathList.Clear();
		IntVec3 intVec = target.ToIntVec3();
		float lengthHorizontal = (intVec - caster.Position).LengthHorizontal;
		float num = (float)(intVec.x - caster.Position.x) / lengthHorizontal;
		float num2 = (float)(intVec.z - caster.Position.z) / lengthHorizontal;
		intVec.x = Mathf.RoundToInt((float)caster.Position.x + num * verbProps.range);
		intVec.z = Mathf.RoundToInt((float)caster.Position.z + num2 * verbProps.range);
		List<IntVec3> list = GenSight.BresenhamCellsBetween(caster.Position, intVec);
		for (int i = 0; i < list.Count; i++)
		{
			IntVec3 c = list[i];
			if (c.InBounds(Caster.Map))
			{
				pathList.Add(c.ToVector3Shifted());
			}
		}
		pathCellsList.Clear();
		foreach (Vector3 vect in pathList)
		{
			pathCellsList.Add(vect.ToIntVec3());
		}
		pathList.Reverse();
		pathCellsList.Reverse();
	}

	private bool CanHit(Thing thing)
	{
		return thing.Spawned && !CoverUtility.ThingCovered(thing, caster.Map);
	}

	private void HitCell(IntVec3 cell, IntVec3 sourceCell, float damageFactor = 1f)
	{
		if (!cell.InBounds(caster.Map))
		{
			return;
		}
		foreach (IntVec3 intVec in GenRadial.RadialCellsAround(cell, 2f, useCenter: true).InRandomOrder())
		{
			if (intVec.InBounds(caster.Map))
			{
				ApplyDamage(VerbUtility.ThingsToHit(intVec, caster.Map, CanHit).RandomElementWithFallback(), sourceCell, damageFactor);
			}
		}
		if (verbProps.beamSetsGroundOnFire && Rand.Chance(verbProps.beamChanceToStartFire))
		{
			FireUtility.TryStartFireIn(cell, caster.Map, 1f, caster);
		}
	}

	private void ApplyDamage(Thing thing, IntVec3 sourceCell, float damageFactor = 1f)
	{
		IntVec3 intVec = InterpolatedPosition.Yto0().ToIntVec3();
		IntVec3 intVec2 = GenSight.LastPointOnLineOfSight(sourceCell, intVec, (IntVec3 c) => c.InBounds(caster.Map) && c.CanBeSeenOverFast(caster.Map), skipFirstCell: true);
		if (intVec2.IsValid)
		{
			intVec = intVec2;
		}
		Map map = caster.Map;
		if (thing == null || verbProps.beamDamageDef == null)
		{
			return;
		}
		float angleFlat = (currentTarget.Cell - caster.Position).AngleFlat;
		BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(caster, thing, currentTarget.Thing, base.EquipmentSource.def, null, null);
		DamageInfo dinfo;
		if (verbProps.beamTotalDamage > 0f)
		{
			float num = verbProps.beamTotalDamage / (float)pathCells.Count;
			num *= damageFactor;
			dinfo = new DamageInfo(verbProps.beamDamageDef, num, verbProps.beamDamageDef.defaultArmorPenetration, angleFlat, caster, null, base.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, currentTarget.Thing);
		}
		else
		{
			float amount = (float)verbProps.beamDamageDef.defaultDamage * damageFactor;
			dinfo = new DamageInfo(verbProps.beamDamageDef, amount, verbProps.beamDamageDef.defaultArmorPenetration, angleFlat, caster, null, base.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, currentTarget.Thing);
		}
		thing.TakeDamage(dinfo).AssociateWithLog(log);
		if (thing.CanEverAttachFire())
		{
			float chance = ((verbProps.flammabilityAttachFireChanceCurve == null) ? verbProps.beamChanceToAttachFire : verbProps.flammabilityAttachFireChanceCurve.Evaluate(thing.GetStatValue(StatDefOf.Flammability)));
			if (Rand.Chance(chance))
			{
				thing.TryAttachFire(verbProps.beamFireSizeRange.RandomInRange, caster);
			}
		}
		else if (Rand.Chance(verbProps.beamChanceToStartFire))
		{
			FireUtility.TryStartFireIn(intVec, map, verbProps.beamFireSizeRange.RandomInRange, caster, verbProps.flammabilityAttachFireChanceCurve);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref path, "path", LookMode.Value);
		Scribe_Values.Look(ref ticksToNextPathStep, "ticksToNextPathStep", 0);
		Scribe_Values.Look(ref initialTargetPosition, "initialTargetPosition");
		if (Scribe.mode == LoadSaveMode.PostLoadInit && path == null)
		{
			path = new List<Vector3>();
		}
	}
}
