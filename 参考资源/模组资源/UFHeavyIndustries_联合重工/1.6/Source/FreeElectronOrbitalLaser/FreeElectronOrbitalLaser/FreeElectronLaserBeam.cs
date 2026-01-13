using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FreeElectronOrbitalLaser;

public class FreeElectronLaserBeam : OrbitalStrike
{
	private static List<Thing> tmpThings = new List<Thing>();

	private List<IntVec3> affectedCellsCache = new List<IntVec3>();

	private IntVec3 lastPosition = IntVec3.Invalid;

	public IntVec3 from;

	public IntVec3 to;

	public MoltenFlowProcess lavaManager;

	private ThingDef_FreeElectronLaserBeam _myDef;

	private ThingDef_FreeElectronLaserBeam MyDef
	{
		get
		{
			if (_myDef == null)
			{
				_myDef = def as ThingDef_FreeElectronLaserBeam;
			}
			return _myDef;
		}
	}

	public override Vector3 DrawPos
	{
		get
		{
			if (duration <= 0)
			{
				return from.ToVector3();
			}
			return Vector3.Lerp(from.ToVector3(), to.ToVector3(), Mathf.InverseLerp(0f, duration, base.TicksPassed));
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref from, "from");
		Scribe_Values.Look(ref to, "to");
		Scribe_References.Look(ref lavaManager, "lavaManager");
	}

	public override void StartStrike()
	{
		base.StartStrike();
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		if (MyDef != null)
		{
			drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			Material matSingle = ThingDefOf.Mote_PowerBeam.graphic.MatSingle;
			float num = (float)this.HashOffsetTicks() * (1f / 60f) * 1.2f;
			Quaternion q = Quaternion.AngleAxis(num, Vector3.up);
			if (MyDef.haloScaleInner > 0f)
			{
				Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(MyDef.haloScaleInner, 1f, MyDef.haloScaleInner), pos: drawLoc, q: q);
				Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0);
			}
			if (MyDef.haloScaleOuter > 0f)
			{
				Matrix4x4 matrix2 = Matrix4x4.TRS(s: new Vector3(MyDef.haloScaleOuter, 1f, MyDef.haloScaleOuter), pos: drawLoc, q: q);
				Graphics.DrawMesh(MeshPool.plane10, matrix2, matSingle, 0);
			}
		}
	}

	protected override void Tick()
	{
		if (base.Destroyed)
		{
			return;
		}
		IntVec3 intVec = (base.Position = DrawPos.ToIntVec3());
		if (MyDef == null)
		{
			base.Tick();
			return;
		}
		int strikesPerTick = MyDef.strikesPerTick;
		if (intVec != lastPosition)
		{
			affectedCellsCache.Clear();
			affectedCellsCache.AddRange(from x in GenRadial.RadialCellsAround(intVec, MyDef.effectRadius, useCenter: true)
				where x.InBounds(base.Map)
				select x);
			lastPosition = intVec;
		}
		if (affectedCellsCache.Count > 0)
		{
			for (int num = 0; num < strikesPerTick; num++)
			{
				StartRandomFireAndDoFlameDamage(affectedCellsCache);
			}
		}
		if (lavaManager != null && !lavaManager.Destroyed)
		{
			float radius = 1f;
			foreach (IntVec3 item in GenRadial.RadialCellsAround(base.Position, radius, useCenter: true))
			{
				if (item.InBounds(base.Map))
				{
					lavaManager.AddCellDirectly(item);
				}
			}
		}
		base.Tick();
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		if (lavaManager != null && !lavaManager.Destroyed)
		{
			lavaManager.isLinkedToBeam = false;
		}
		base.Destroy(mode);
	}

	private void StartRandomFireAndDoFlameDamage(List<IntVec3> validCells)
	{
		if (MyDef == null || MyDef.damageDef == null)
		{
			return;
		}
		IntVec3 c = validCells.RandomElementByWeight((IntVec3 x) => 1f - Mathf.Min(x.DistanceTo(base.Position) / MyDef.effectRadius, 1f) + 0.05f);
		Thing thing = instigator ?? this;
		FireUtility.TryStartFireIn(c, base.Map, Rand.Range(0.1f, 0.925f), thing);
		tmpThings.Clear();
		tmpThings.AddRange(c.GetThingList(base.Map));
		for (int num = 0; num < tmpThings.Count; num++)
		{
			int num2 = ((tmpThings[num] is Corpse) ? MyDef.corpseFlameDamageAmountRange.RandomInRange : MyDef.flameDamageAmountRange.RandomInRange);
			Pawn pawn = tmpThings[num] as Pawn;
			BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = null;
			if (pawn != null && instigator != null)
			{
				battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_PowerBeam, instigator as Pawn);
				Find.BattleLog.Add(battleLogEntry_DamageTaken);
			}
			DamageInfo dinfo = new DamageInfo(MyDef.damageDef, num2, MyDef.armorPenetration, -1f, instigator, null, weaponDef);
			tmpThings[num].TakeDamage(dinfo).AssociateWithLog(battleLogEntry_DamageTaken);
		}
		tmpThings.Clear();
	}
}
