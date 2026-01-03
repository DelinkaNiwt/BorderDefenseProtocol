using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class Building_Aerocraft : Building_PassengerShuttle, IAttackTarget, ILoadReferenceable
{
	private Vector2 realPosition;

	private IntVec3 targetPosition;

	private Rot4 landRotation;

	private float targetDirection;

	private float cachedCurDirection;

	private float deltaDirection;

	private static MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();

	private static readonly IntRange DurationTicks = new IntRange(2700, 10080);

	private static readonly Material ShadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent, new Color(1f, 1f, 1f, 1f));

	private static Material MoveLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, Color.white);

	private static MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();

	private bool allowAutoRefuelCache = false;

	private int lerpTick;

	private int turningTick;

	private static readonly SimpleCurve TakeoffCurve = new SimpleCurve
	{
		new CurvePoint(0f, 0f),
		new CurvePoint(0.5f, 0.6f),
		new CurvePoint(1f, 1f)
	};

	private static readonly SimpleCurve LandingCurve = new SimpleCurve
	{
		new CurvePoint(0f, 1f),
		new CurvePoint(0.5f, 0.4f),
		new CurvePoint(1f, 0f)
	};

	public AerocraftState FlightState = AerocraftState.Grounded;

	Thing IAttackTarget.Thing => this;

	public LocalTargetInfo TargetCurrentlyAimingAt => null;

	public float TargetPriorityFactor => 1f;

	private float TargetDirection => (targetPosition.ToVector3Shifted() - realPosition.ToVector3()).AngleFlat();

	public float CurDirection
	{
		get
		{
			return cachedCurDirection;
		}
		set
		{
			if (cachedCurDirection != value)
			{
				if (value < 0f)
				{
					value += 360f;
				}
				if (value >= 360f)
				{
					value -= 360f;
				}
				cachedCurDirection = value;
			}
		}
	}

	private float DeltaDirection
	{
		get
		{
			float num = CurDirection - TargetDirection;
			if (num < -180f)
			{
				num += 360f;
			}
			if (num > 180f)
			{
				num -= 360f;
			}
			return num;
		}
	}

	private float MinTurningRadius => this.GetStatValue(AncotDefOf.Ancot_Aerocraft_MinTurnRadius, applyPostProcess: true, 120);

	private float MaxFlightSpeed => this.GetStatValue(AncotDefOf.Ancot_Aerocraft_MaxSpeed, applyPostProcess: true, 120);

	private float AngularVelocityPerTick => FlightSpeed * 180f / (MinTurningRadius * (float)Math.PI);

	private CompAerocraft CompAerocraft => this.TryGetComp<CompAerocraft>();

	public AltitudeLayer AltitudeLayer
	{
		get
		{
			if (FlightState == AerocraftState.Grounded)
			{
				return def.altitudeLayer;
			}
			return AltitudeLayer.Skyfaller;
		}
	}

	public override Vector2 DrawSize => new Vector2(1f, 1f);

	public override Vector3 DrawPos => new Vector3(realPosition.x, AltitudeLayer.AltitudeFor(), realPosition.y + PositionOffsetFactor * 1.2f);

	public float FlightSpeed => PositionOffsetFactor * MaxFlightSpeed / 60f;

	public float PositionOffsetFactor => FlightState switch
	{
		AerocraftState.Flying => 1f, 
		AerocraftState.TakingOff => TakeoffCurve.Evaluate((float)lerpTick / 50f), 
		AerocraftState.Landing => LandingCurve.Evaluate((float)lerpTick / 50f), 
		_ => 0f, 
	};

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			Vector3 vector = base.Position.ToVector3Shifted();
			realPosition = new Vector2(vector.x, vector.z);
			CurDirection = base.Rotation.AsAngle;
		}
	}

	protected override void Tick()
	{
		if (!base.Spawned)
		{
			return;
		}
		switch (FlightState)
		{
		case AerocraftState.TakingOff:
			lerpTick++;
			if (lerpTick >= 50)
			{
				FlightState = AerocraftState.Flying;
				lerpTick = 0;
			}
			if (base.Position != targetPosition)
			{
				realPosition = realPosition.Moved(CurDirection - 90f, FlightSpeed);
				IntVec3 intVec = new Vector3(realPosition.x, 0f, realPosition.y).ToIntVec3();
				if (intVec.InBounds(base.Map))
				{
					base.Position = intVec;
				}
			}
			break;
		case AerocraftState.Landing:
			if ((float)turningTick >= 360f / AngularVelocityPerTick)
			{
				FlightState = AerocraftState.Flying;
			}
			if (base.Position != targetPosition)
			{
				MoveToTarget();
				break;
			}
			lerpTick++;
			if (lerpTick >= 80)
			{
				FlightState = AerocraftState.Grounded;
				lerpTick = 0;
				base.Rotation = landRotation;
				base.RefuelableComp.allowAutoRefuel = allowAutoRefuelCache;
			}
			if (CurDirection != landRotation.AsAngle)
			{
				CurDirection = Mathf.LerpAngle(CurDirection, landRotation.AsAngle, (float)lerpTick / 80f);
				Vector3 vector = targetPosition.ToVector3Shifted();
				realPosition = Vector2.Lerp(b: new Vector2(vector.x, vector.z), a: realPosition, t: (float)lerpTick / 80f);
			}
			break;
		case AerocraftState.Flying:
			if (base.Position != targetPosition)
			{
				MoveToTarget();
			}
			break;
		}
		base.Tick();
	}

	private void MoveToTarget()
	{
		if (CurDirection != TargetDirection)
		{
			if (DeltaDirection >= 0f - AngularVelocityPerTick && DeltaDirection <= AngularVelocityPerTick)
			{
				CurDirection = TargetDirection;
				turningTick = 0;
			}
			else if (deltaDirection < 0f)
			{
				CurDirection += AngularVelocityPerTick;
			}
			else if (deltaDirection > 0f)
			{
				CurDirection -= AngularVelocityPerTick;
			}
			turningTick++;
		}
		realPosition = realPosition.Moved(CurDirection - 90f, FlightSpeed);
		IntVec3 intVec = new Vector3(realPosition.x, 0f, realPosition.y).ToIntVec3();
		if (intVec.InBounds(base.Map))
		{
			base.Position = intVec;
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		if (FlightState == AerocraftState.Grounded)
		{
			base.DrawAt(drawLoc, flip);
			return;
		}
		Effect(drawLoc, CurDirection);
		DrawShadow(new Vector3(realPosition.x, 0f, realPosition.y), Rot4.North, ShadowMaterial, new Vector3(3f, 3f), PositionOffsetFactor);
		Comps_DrawAt(drawLoc, flip);
		Comps_PostDraw();
	}

	public void DrawShadow(Vector3 center, Rot4 rot, Material material, Vector2 shadowSize, float altitude)
	{
		altitude = Mathf.Max(altitude, 0f);
		Vector3 pos = center;
		pos.y = AltitudeLayer.Shadows.AltitudeFor();
		float num = 1f + altitude / 100f;
		Vector3 s = new Vector3(num * shadowSize.x, 1f, num * shadowSize.y);
		Color white = Color.white;
		white.a = Mathf.InverseLerp(200f, 150f, altitude);
		shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, white);
		Matrix4x4 matrix = default(Matrix4x4);
		matrix.SetTRS(pos, rot.AsQuat, s);
		Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, shadowPropertyBlock);
	}

	public virtual void Effect(Vector3 drawLoc, float aimAngle)
	{
		Mesh plane = MeshPool.plane10;
		aimAngle %= 360f;
		drawLoc.y = AltitudeLayer.AltitudeFor();
		Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(CompAerocraft.InflightGraphicData.drawSize.x, 0f, CompAerocraft.InflightGraphicData.drawSize.y), pos: drawLoc, q: Quaternion.AngleAxis(aimAngle, Vector3.up));
		Graphics.DrawMesh(plane, matrix, CompAerocraft.InflightGraphicData.Graphic.MatNorth, 0);
	}

	public override void DrawExtraSelectionOverlays()
	{
		if (FlightState == AerocraftState.Grounded)
		{
			base.DrawExtraSelectionOverlays();
		}
		if (FlightState == AerocraftState.Grounded || !Find.Selector.SelectedObjects.Contains(this))
		{
			return;
		}
		if (targetPosition != base.Position)
		{
			GenDraw.DrawLineBetween(DrawPos, targetPosition.ToVector3Shifted(), MoveLineMat);
		}
		if (base.AllComps != null)
		{
			for (int i = 0; i < base.AllComps.Count; i++)
			{
				base.AllComps[i].PostDrawExtraSelectionOverlays();
			}
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (FlightState != AerocraftState.Grounded && FlightState != AerocraftState.Landing)
		{
			yield return new Command_Land
			{
				defaultLabel = "Ancot.AerocraftLanding".Translate(),
				defaultDesc = "Ancot.AerocraftLandingDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get(CompAerocraft.LandingIconPath),
				thing = this,
				thingDef = def,
				action = delegate(IntVec3 pos, Rot4 rot)
				{
					FlightState = AerocraftState.Landing;
					landRotation = rot;
					SetTargetDestination(pos);
				},
				action_RightClickMap = delegate(IntVec3 pos)
				{
					if (!pos.Fogged(base.Map) && pos.InBounds(base.Map))
					{
						SetTargetDestination(pos);
					}
				}
			};
		}
		if (FlightState != AerocraftState.Grounded)
		{
			yield break;
		}
		Command_Action command_Action = new Command_Action
		{
			defaultLabel = "Ancot.AerocraftTakeOff".Translate(),
			defaultDesc = "Ancot.AerocraftTakeOffDesc".Translate(CompAerocraft.TakeOffFuelCost.ToString(), base.RefuelableComp.Props.FuelGizmoLabel),
			icon = ContentFinder<Texture2D>.Get(CompAerocraft.TakeOffIconPath),
			action = delegate
			{
				FlightState = AerocraftState.TakingOff;
				targetPosition = base.Position;
				turningTick = 0;
				if (CompAerocraft.TakeOffEffect != null)
				{
					Effecter effecter = new Effecter(CompAerocraft.TakeOffEffect);
					effecter.Trigger(new TargetInfo(this.TrueCenter().ToIntVec3(), base.Map), TargetInfo.Invalid);
					effecter.Cleanup();
				}
				if (CompAerocraft.TakeOffFuelCost != 0f)
				{
					base.RefuelableComp.ConsumeFuel(CompAerocraft.TakeOffFuelCost);
				}
				allowAutoRefuelCache = base.RefuelableComp.allowAutoRefuel;
				base.RefuelableComp.allowAutoRefuel = false;
			}
		};
		yield return command_Action;
		if (CompAerocraft.RequirePilot && !base.ShuttleComp.HasPilot)
		{
			command_Action.Disable("Ancot.AerocraftDisabled_RequirePilot".Translate());
		}
		if (base.RefuelableComp.Fuel < CompAerocraft.TakeOffFuelCost)
		{
			command_Action.Disable("Ancot.AerocraftDisabled_RequireFuel".Translate());
		}
	}

	public void SetTargetDestination(IntVec3 pos)
	{
		targetPosition = pos;
		targetDirection = (targetPosition.ToVector3() - base.Position.ToVector3()).AngleFlat();
		deltaDirection = DeltaDirection;
		turningTick = 0;
		FleckMaker.Static(pos, base.Map, FleckDefOf.FeedbackGoto);
	}

	public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
	{
		if (disabledFor?.Thing is Pawn pawn)
		{
			Pawn_EquipmentTracker equipment = pawn.equipment;
			if ((equipment == null || equipment.Primary?.def.IsMeleeWeapon != false) && !pawn.flight.CanFlyNow)
			{
				return true;
			}
		}
		return false;
	}

	public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		float statValue = this.GetStatValue(AncotDefOf.Ancot_Aerocraft_ArmorRating, applyPostProcess: true, 120);
		if (dinfo.ArmorPenetrationInt < statValue)
		{
			dinfo.SetAmount(dinfo.Amount * (1f - (statValue - dinfo.ArmorPenetrationInt)));
		}
		base.PreApplyDamage(ref dinfo, out absorbed);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref realPosition, "realPosition");
		Scribe_Values.Look(ref targetPosition, "targetPosition");
		Scribe_Values.Look(ref cachedCurDirection, "cachedCurDirection", 0f);
		Scribe_Values.Look(ref targetDirection, "targetDirection", 0f);
		Scribe_Values.Look(ref deltaDirection, "deltaDirection", 0f);
		Scribe_Values.Look(ref FlightState, "FlightState", AerocraftState.Grounded);
		Scribe_Values.Look(ref landRotation, "landRotation");
		Scribe_Values.Look(ref lerpTick, "lerpRick", 0);
		Scribe_Values.Look(ref turningTick, "turningTick", 0);
		Scribe_Values.Look(ref allowAutoRefuelCache, "allowAutoRefuelCache", defaultValue: false);
	}
}
