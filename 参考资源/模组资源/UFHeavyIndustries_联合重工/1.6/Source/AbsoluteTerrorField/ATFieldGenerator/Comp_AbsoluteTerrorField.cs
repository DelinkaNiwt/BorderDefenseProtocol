using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ATFieldGenerator;

[StaticConstructorOnStartup]
public class Comp_AbsoluteTerrorField : ThingComp
{
	public float energy;

	public int ticksToReset = -1;

	private CompFlickable flick;

	private CompPowerTrader power;

	public float radius;

	public float maxRadius;

	public float minRadius;

	public bool reflectMode = true;

	public bool suppressExplosions = true;

	public bool redirectSkyfallers = true;

	public bool antiTeleport = true;

	private bool initialized = false;

	private Material shieldMatInstance;

	private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

	public Material ShieldMat
	{
		get
		{
			if (shieldMatInstance == null)
			{
				string texPath = (string.IsNullOrEmpty(Props.shieldTexturePath) ? "Other/ForceField" : Props.shieldTexturePath);
				shieldMatInstance = MaterialPool.MatFrom(texPath, ShaderDatabase.MoteGlow);
			}
			return shieldMatInstance;
		}
	}

	public CompProperties_AbsoluteTerrorField Props => (CompProperties_AbsoluteTerrorField)props;

	public ThingDef InterceptMoteDef => Props.interceptMoteDef ?? DefDatabase<ThingDef>.GetNamed("Mote_ATFieldOctagon", errorOnFail: false);

	public ShieldState State
	{
		get
		{
			if (ticksToReset > 0 || (flick != null && !flick.SwitchIsOn) || (power != null && !power.PowerOn))
			{
				return ShieldState.Resetting;
			}
			return ShieldState.Active;
		}
	}

	public bool Active => State == ShieldState.Active;

	public int EnergyMax => Props.energyMax;

	public float EnergyGainPerTick => Props.energyGainPerTick;

	public float EnergyLossPerDamage => Props.energyLossPerDamage;

	public bool BlockSolarFlare => Props.blockSolarFlare;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		flick = parent.GetComp<CompFlickable>();
		power = parent.GetComp<CompPowerTrader>();
		if (!respawningAfterLoad && !initialized)
		{
			radius = Props.radiusDefault;
			suppressExplosions = Props.suppressExplosions;
			redirectSkyfallers = Props.redirectSkyfallers;
			antiTeleport = Props.antiTeleport;
			initialized = true;
		}
		maxRadius = Props.radiusMax;
		minRadius = Props.radiusMin;
		ATFieldManager.Get(parent.Map).Register(this);
	}

	public override void PostDeSpawn(Map map, DestroyMode mode)
	{
		base.PostDeSpawn(map, mode);
		if (map != null)
		{
			ATFieldManager.Get(map).Deregister(this);
		}
	}

	public void Break()
	{
		FleckMaker.Static(parent.TrueCenter(), parent.Map, FleckDefOf.ExplosionFlash, 12f);
		energy = 0f;
		ticksToReset = Props.cooldownTicks;
		if (parent.Map != null)
		{
			Messages.Message("ATField_Break_Warning".Translate(), new TargetInfo(parent.Position, parent.Map), MessageTypeDefOf.NegativeEvent);
		}
	}

	private void Reset()
	{
		ticksToReset = -1;
		energy = (float)EnergyMax * Props.restartEnergyPct;
	}

	public bool TryConsumeEnergy(float amount)
	{
		if (State == ShieldState.Resetting)
		{
			return false;
		}
		if (amount <= 0f)
		{
			return true;
		}
		energy -= amount;
		if (energy <= 0f)
		{
			Break();
		}
		return true;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (State == ShieldState.Active)
		{
			if (power != null && power.PowerOn)
			{
				energy += EnergyGainPerTick;
				if (energy > (float)EnergyMax)
				{
					energy = EnergyMax;
				}
			}
			return;
		}
		energy = 0f;
		if (flick.SwitchIsOn && power.PowerOn && ticksToReset > 0)
		{
			ticksToReset--;
			if (ticksToReset <= 0)
			{
				Reset();
			}
		}
	}

	public void SpawnInterceptEffect(Vector3 hitPos, float damage)
	{
		if (parent.Map == null)
		{
			return;
		}
		ThingDef interceptMoteDef = InterceptMoteDef;
		if (interceptMoteDef != null)
		{
			Vector3 vector = parent.Position.ToVector3Shifted();
			float num = (hitPos - vector).AngleFlat();
			Mote_ATFieldOctagon mote_ATFieldOctagon = (Mote_ATFieldOctagon)ThingMaker.MakeThing(interceptMoteDef);
			if (mote_ATFieldOctagon != null)
			{
				mote_ATFieldOctagon.exactPosition = hitPos;
				mote_ATFieldOctagon.exactRotation = num;
				mote_ATFieldOctagon.customRotation = num;
				float num2 = ((Props.damageScaleFactor > 0f) ? Props.damageScaleFactor : 1f);
				float value = Props.damageScaleBase + damage / num2;
				mote_ATFieldOctagon.damageScale = Mathf.Clamp(value, Props.damageScaleBase, Props.damageScaleMax);
				GenSpawn.Spawn(mote_ATFieldOctagon, hitPos.ToIntVec3(), parent.Map);
			}
		}
	}

	private float GetAdjustedDamage(float incomingDamage, DamageDef damType)
	{
		if (damType != null && !damType.harmsHealth)
		{
			return 0f;
		}
		if (Props.maxDamagePerHit > 0f && incomingDamage > Props.maxDamagePerHit)
		{
			return Props.maxDamagePerHit;
		}
		return incomingDamage;
	}

	public bool CheckIntercept(Projectile projectile, Vector3 lastExactPos, Vector3 newExactPos)
	{
		Vector3 vector = parent.Position.ToVector3Shifted();
		float num = radius + projectile.def.projectile.SpeedTilesPerTick + 0.1f;
		if ((newExactPos.x - vector.x) * (newExactPos.x - vector.x) + (newExactPos.z - vector.z) * (newExactPos.z - vector.z) > num * num)
		{
			return false;
		}
		if (!Active)
		{
			return false;
		}
		if ((new Vector2(vector.x, vector.z) - new Vector2(lastExactPos.x, lastExactPos.z)).sqrMagnitude <= radius * radius)
		{
			return false;
		}
		if (projectile.def.projectile.alwaysFreeIntercept)
		{
			return false;
		}
		if (!GenGeo.IntersectLineCircleOutline(new Vector2(vector.x, vector.z), radius, new Vector2(lastExactPos.x, lastExactPos.z), new Vector2(newExactPos.x, newExactPos.z)))
		{
			return false;
		}
		float incomingDamage = projectile.DamageAmount;
		float adjustedDamage = GetAdjustedDamage(incomingDamage, projectile.def.projectile.damageDef);
		float num2 = adjustedDamage * EnergyLossPerDamage;
		if (reflectMode)
		{
			num2 *= Props.reflectEnergyCostFactor;
		}
		if (!TryConsumeEnergy(num2))
		{
			return false;
		}
		SpawnInterceptEffect(newExactPos, adjustedDamage);
		return true;
	}

	public bool CheckBombardmentIntercept(Bombardment bombardment, Bombardment.BombardmentProjectile projectile)
	{
		if (!Active)
		{
			return false;
		}
		if (!projectile.targetCell.InHorDistOf(parent.Position, radius))
		{
			return false;
		}
		float incomingDamage = 100f;
		float adjustedDamage = GetAdjustedDamage(incomingDamage, DamageDefOf.Bomb);
		float amount = adjustedDamage * EnergyLossPerDamage * 2f;
		if (!TryConsumeEnergy(amount))
		{
			return false;
		}
		SpawnInterceptEffect(projectile.targetCell.ToVector3Shifted(), adjustedDamage);
		return true;
	}

	public bool CheckBeamIntercept(Beam beam, Thing launcher, LocalTargetInfo target)
	{
		if (!Active)
		{
			return false;
		}
		if (launcher.Position.InHorDistOf(parent.Position, radius))
		{
			return false;
		}
		Vector3 vector = parent.Position.ToVector3Shifted();
		Vector2 lineA = launcher.Position.ToVector2();
		Vector2 lineB = target.Cell.ToVector2();
		if (!GenGeo.IntersectLineCircleOutline(new Vector2(vector.x, vector.z), radius, lineA, lineB))
		{
			return false;
		}
		float incomingDamage = beam.DamageAmount;
		float adjustedDamage = GetAdjustedDamage(incomingDamage, beam.def.projectile.damageDef);
		float amount = adjustedDamage * EnergyLossPerDamage;
		if (!TryConsumeEnergy(amount))
		{
			return false;
		}
		Vector3 normalized = (launcher.Position.ToVector3Shifted() - vector).normalized;
		Vector3 vector2 = vector + normalized * radius;
		SpawnInterceptEffect(vector2, adjustedDamage);
		if (beam.def.projectile.beamMoteDef != null)
		{
			Vector3 offsetA = (vector2 - launcher.Position.ToVector3Shifted()).Yto0().normalized * beam.def.projectile.beamStartOffset;
			MoteMaker.MakeInteractionOverlay(beam.def.projectile.beamMoteDef, launcher, new TargetInfo(vector2.ToIntVec3(), parent.Map), offsetA, Vector3.zero);
		}
		return true;
	}

	public bool CheckVerbShootBeamIntercept(Verb_ShootBeam verb, IntVec3 hitCell, IntVec3 sourceCell)
	{
		if (!Active)
		{
			return false;
		}
		if (sourceCell.InHorDistOf(parent.Position, radius))
		{
			return false;
		}
		Vector3 vector = parent.Position.ToVector3Shifted();
		Vector2 lineA = sourceCell.ToVector3Shifted().ToVector2();
		Vector2 lineB = hitCell.ToVector3Shifted().ToVector2();
		if (!GenGeo.IntersectLineCircleOutline(new Vector2(vector.x, vector.z), radius, lineA, lineB))
		{
			return false;
		}
		float incomingDamage = verb.verbProps.beamDamageDef.defaultDamage;
		float adjustedDamage = GetAdjustedDamage(incomingDamage, verb.verbProps.beamDamageDef);
		float amount = adjustedDamage * EnergyLossPerDamage;
		if (!TryConsumeEnergy(amount))
		{
			return false;
		}
		Vector3 normalized = (sourceCell.ToVector3Shifted() - vector).normalized;
		Vector3 hitPos = vector + normalized * radius;
		SpawnInterceptEffect(hitPos, adjustedDamage);
		return true;
	}

	public bool CheckExplosionIntercept(IntVec3 center, DamageDef damType)
	{
		if (!Active || !suppressExplosions)
		{
			return false;
		}
		if (!center.InHorDistOf(parent.Position, radius))
		{
			return false;
		}
		float incomingDamage = damType.defaultDamage;
		float adjustedDamage = GetAdjustedDamage(incomingDamage, damType);
		float amount = adjustedDamage * EnergyLossPerDamage * 2f;
		if (TryConsumeEnergy(amount))
		{
			FleckMaker.ThrowMicroSparks(center.ToVector3Shifted(), parent.Map);
			FleckMaker.ThrowLightningGlow(center.ToVector3Shifted(), parent.Map, 1.5f);
			SpawnInterceptEffect(center.ToVector3Shifted(), (!(adjustedDamage > 0.1f)) ? 20f : ((adjustedDamage > 100f) ? 100f : adjustedDamage));
			return true;
		}
		return false;
	}

	public bool CheckExplosionAffectCellIntercept(IntVec3 cell)
	{
		if (!Active || !suppressExplosions)
		{
			return false;
		}
		return cell.InHorDistOf(parent.Position, radius);
	}

	public bool CheckBurstingTickIntercept(Verb_ShootBeam verb, Vector3 start, Vector3 end)
	{
		if (!Active)
		{
			return false;
		}
		if (start.ToIntVec3().InHorDistOf(parent.Position, radius))
		{
			return false;
		}
		Vector3 vector = parent.Position.ToVector3Shifted();
		return GenGeo.IntersectLineCircleOutline(new Vector2(vector.x, vector.z), radius, new Vector2(start.x, start.z), new Vector2(end.x, end.z));
	}

	public void DrawShield()
	{
		World world = Find.World;
		if (world != null && world.renderer?.wantedMode == WorldRenderMode.None && Find.CurrentMap == parent.Map && Active)
		{
			Vector3 drawPos = parent.DrawPos;
			drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			float num = radius * 2f * 1.1601562f;
			Color value = (reflectMode ? Props.shieldColorReflect : Props.shieldColor);
			MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(num, 1f, num));
			Graphics.DrawMesh(MeshPool.plane10, matrix, ShieldMat, 0, null, 0, MatPropertyBlock);
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref initialized, "initialized", defaultValue: false);
		Scribe_Values.Look(ref energy, "energy", 0f);
		Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
		Scribe_Values.Look(ref radius, "radius", Props.radiusDefault);
		Scribe_Values.Look(ref reflectMode, "reflectMode", defaultValue: true);
		Scribe_Values.Look(ref suppressExplosions, "suppressExplosions", Props.suppressExplosions);
		Scribe_Values.Look(ref redirectSkyfallers, "redirectSkyfallers", Props.redirectSkyfallers);
		Scribe_Values.Look(ref antiTeleport, "antiTeleport", Props.antiTeleport);
	}

	public override void PostDrawExtraSelectionOverlays()
	{
		base.PostDrawExtraSelectionOverlays();
		if (radius <= 70f)
		{
			GenDraw.DrawRadiusRing(parent.Position, radius);
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		yield return new Gizmo_EnergyStatus(this);
		yield return new Gizmo_RadiusSlider(this);
		yield return new Command_Toggle
		{
			defaultLabel = "ATField_ReflectMode_Label".Translate(),
			defaultDesc = "ATField_ReflectMode_Desc".Translate(),
			icon = ContentFinder<Texture2D>.Get(Props.iconPathReflect),
			isActive = () => reflectMode,
			toggleAction = delegate
			{
				reflectMode = !reflectMode;
			}
		};
		yield return new Command_Toggle
		{
			defaultLabel = "ATField_SuppressExplosions_Label".Translate(),
			defaultDesc = "ATField_SuppressExplosions_Desc".Translate(),
			icon = ContentFinder<Texture2D>.Get(Props.iconPathExplosion),
			isActive = () => suppressExplosions,
			toggleAction = delegate
			{
				suppressExplosions = !suppressExplosions;
			}
		};
		yield return new Command_Toggle
		{
			defaultLabel = "ATField_RedirectSkyfall_Label".Translate(),
			defaultDesc = "ATField_RedirectSkyfall_Desc".Translate(),
			icon = ContentFinder<Texture2D>.Get(Props.iconPathSkyfall),
			isActive = () => redirectSkyfallers,
			toggleAction = delegate
			{
				redirectSkyfallers = !redirectSkyfallers;
			}
		};
		yield return new Command_Toggle
		{
			defaultLabel = "ATField_AntiTeleport_Label".Translate(),
			defaultDesc = "ATField_AntiTeleport_Desc".Translate(),
			icon = ContentFinder<Texture2D>.Get(Props.iconPathTeleport),
			isActive = () => antiTeleport,
			toggleAction = delegate
			{
				antiTeleport = !antiTeleport;
			}
		};
	}
}
