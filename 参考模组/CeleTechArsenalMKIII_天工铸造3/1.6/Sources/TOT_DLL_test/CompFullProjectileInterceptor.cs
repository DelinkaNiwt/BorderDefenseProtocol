using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class CompFullProjectileInterceptor : ThingComp
{
	public float energy;

	protected int ticksToReset = -1;

	private CompFlickable flick;

	private CompPowerTrader power;

	public bool debugInterceptNonHostileProjectiles;

	private float lastInterceptAngle;

	private int lastInterceptTicks = -999999;

	private float EnergyLossPerDamage = 1f;

	private int nextChargeTick = -1;

	private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

	private static readonly Material ShieldMat = MaterialPool.MatFrom("Things/FRShield_CMC", ShaderDatabase.MoteGlow);

	private bool drawInterceptCone;

	private float EnergyGainPerTick = 9f;

	public int EnergyMax = 1000;

	public static float RadiusMax;

	public float radius = 20f;

	public CompProperties_FullProjectileInterceptor Props => (CompProperties_FullProjectileInterceptor)props;

	public bool Active => ShieldState == ShieldState.Active;

	public ShieldState ShieldState
	{
		get
		{
			if (ticksToReset > 0 || !flick.SwitchIsOn || !power.PowerOn)
			{
				return ShieldState.Resetting;
			}
			return ShieldState.Active;
		}
	}

	public bool OnCooldown => Find.TickManager.TicksGame < lastInterceptTicks + Props.cooldownTicks;

	public bool Charging => nextChargeTick >= 0 && Find.TickManager.TicksGame > nextChargeTick;

	public bool BombardmentCanStartFireAt(Bombardment bombardment, IntVec3 cell)
	{
		return !Active || ((bombardment.instigator == null || !bombardment.instigator.HostileTo(parent)) && !debugInterceptNonHostileProjectiles && !Props.interceptNonHostileProjectiles) || !cell.InHorDistOf(parent.Position, radius);
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
		if ((bombardment.instigator == null || !bombardment.instigator.HostileTo(parent)) && !debugInterceptNonHostileProjectiles && !Props.interceptNonHostileProjectiles)
		{
			return false;
		}
		lastInterceptTicks = Find.TickManager.TicksGame;
		drawInterceptCone = false;
		TriggerEffecter(projectile.targetCell);
		return true;
	}

	private void TriggerEffecter(IntVec3 pos)
	{
		Effecter effecter = new Effecter(Props.interceptEffect ?? RimWorld.EffecterDefOf.Interceptor_BlockedProjectile);
		effecter.Trigger(new TargetInfo(pos, parent.Map), TargetInfo.Invalid);
		effecter.Cleanup();
	}

	public bool CheckIntercept(Projectile projectile, Vector3 lastExactPos, Vector3 newExactPos)
	{
		Vector3 vector = parent.Position.ToVector3Shifted();
		float num = radius + projectile.def.projectile.SpeedTilesPerTick + 0.1f;
		if ((newExactPos.x - vector.x) * (newExactPos.x - vector.x) + (newExactPos.z - vector.z) * (newExactPos.z - vector.z) > num * num)
		{
			return false;
		}
		if (!Active || ShieldState == ShieldState.Resetting)
		{
			return false;
		}
		if ((projectile.Launcher == null || !projectile.Launcher.HostileTo(parent)) && !debugInterceptNonHostileProjectiles && !Props.interceptNonHostileProjectiles)
		{
			return false;
		}
		if (!GenGeo.IntersectLineCircleOutline(new Vector2(vector.x, vector.z), radius, new Vector2(lastExactPos.x, lastExactPos.z), new Vector2(newExactPos.x, newExactPos.z)))
		{
			return false;
		}
		DamageInfo damageInfo = new DamageInfo(projectile.def.projectile.damageDef, projectile.DamageAmount);
		lastInterceptAngle = lastExactPos.AngleToFlat(parent.TrueCenter());
		lastInterceptTicks = Find.TickManager.TicksGame;
		Effecter effecter = new Effecter(Props.interceptEffect ?? RimWorld.EffecterDefOf.Interceptor_BlockedProjectile);
		effecter.Trigger(new TargetInfo(newExactPos.ToIntVec3(), parent.Map), TargetInfo.Invalid);
		effecter.Cleanup();
		energy -= damageInfo.Amount * EnergyLossPerDamage;
		if (energy < 0f)
		{
			Break();
		}
		return true;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (power.PowerOn)
		{
			energy += EnergyGainPerTick;
			if (energy > (float)EnergyMax)
			{
				energy = EnergyMax;
			}
		}
		else
		{
			energy = 0f;
		}
		if (ShieldState == ShieldState.Resetting)
		{
			ticksToReset--;
			if (energy >= (float)EnergyMax)
			{
				Reset();
			}
		}
	}

	public void Break()
	{
		SoundDefOf.MechSelfShutdown.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
		FleckMaker.Static(parent.TrueCenter(), parent.Map, FleckDefOf.ExplosionFlash, 52f);
		for (int i = 0; i < 6; i++)
		{
			FleckMaker.ThrowDustPuff(parent.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), parent.Map, Rand.Range(0.8f, 1.2f));
		}
		energy = 0f;
		ticksToReset = Props.startingTicksToReset;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref energy, "energy", 0f);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		flick = parent.GetComp<CompFlickable>();
		power = parent.GetComp<CompPowerTrader>();
	}

	private void Reset()
	{
		if (parent.Spawned)
		{
			SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
		}
		ticksToReset = -1;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Find.Selector.SingleSelectedThing == parent)
		{
			yield return new Gizmo_PIStatus
			{
				shield = this
			};
		}
		if (!Prefs.DevMode)
		{
			yield break;
		}
		if (ShieldState == ShieldState.Resetting)
		{
			yield return new Command_Action
			{
				defaultLabel = "Dev: Reset cooldown ",
				action = delegate
				{
					lastInterceptTicks = Find.TickManager.TicksGame - Props.cooldownTicks;
				}
			};
		}
		yield return new Command_Toggle
		{
			defaultLabel = "Dev: Intercept non-hostile",
			isActive = () => debugInterceptNonHostileProjectiles,
			toggleAction = delegate
			{
				debugInterceptNonHostileProjectiles = !debugInterceptNonHostileProjectiles;
			}
		};
	}

	private float GetCurrentAlpha()
	{
		float num = Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(GetCurrentAlpha_Idle(), GetCurrentAlpha_RecentlyIntercepted()), GetCurrentAlpha_RecentlyActivated()), 0.11f));
		if (parent.Map.dangerWatcher.DangerRating == StoryDanger.High)
		{
			num = Mathf.Clamp01(num * 1.5f);
		}
		return num;
	}

	private float GetCurrentAlpha_Idle()
	{
		float num = 0.7f;
		float a = -1.7f;
		if (!Active)
		{
			return 0f;
		}
		if (Find.Selector.IsSelected(parent))
		{
			return 0f;
		}
		return Mathf.Lerp(a, 0.11f, (Mathf.Sin((float)(Gen.HashCombineInt(parent.thingIDNumber, 96804938) % 100) + Time.realtimeSinceStartup * num) + 1f) / 2f);
	}

	private float GetCurrentAlpha_RecentlyActivated()
	{
		if (!Active)
		{
			return 0f;
		}
		int num = Find.TickManager.TicksGame - (lastInterceptTicks + Props.cooldownTicks);
		return Mathf.Clamp01(1f - (float)num / 50f) * 0.09f;
	}

	private float GetCurrentAlpha_RecentlyIntercepted()
	{
		int num = Find.TickManager.TicksGame - lastInterceptTicks;
		return Mathf.Clamp01(1f - (float)num / 40f) * 0.09f;
	}

	public override void PostDraw()
	{
		base.PostDraw();
		Vector3 drawPos = parent.DrawPos;
		drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
		if (Active && (parent.Map.attackTargetsCache.TargetsHostileToColony.Count > 0 || Find.TickManager.TicksGame - lastInterceptTicks < 15))
		{
			Color cyan = Color.cyan;
			cyan.a = GetCurrentAlpha();
			MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, cyan);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(radius * 2f * 1.1601562f, 1f, radius * 2f * 1.1601562f));
			Graphics.DrawMesh(MeshPool.plane10, matrix, ShieldMat, 0, null, 0, MatPropertyBlock);
		}
	}

	public override void PostDrawExtraSelectionOverlays()
	{
		base.PostDrawExtraSelectionOverlays();
		Vector3 drawPos = parent.DrawPos;
		drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
		if (Active && (parent.Map.attackTargetsCache.TargetsHostileToColony.Count > 0 || Find.TickManager.TicksGame - lastInterceptTicks < 15))
		{
			Color cyan = Color.cyan;
			cyan.a = GetCurrentAlpha();
			MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, cyan);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(radius * 2f * 1.1601562f, 1f, radius * 2f * 1.1601562f));
			Graphics.DrawMesh(MeshPool.plane10, matrix, ShieldMat, 0, null, 0, MatPropertyBlock);
		}
		if (radius < 54f)
		{
			GenDraw.DrawRadiusRing(parent.DrawPos.ToIntVec3(), radius, Color.white);
		}
	}
}
