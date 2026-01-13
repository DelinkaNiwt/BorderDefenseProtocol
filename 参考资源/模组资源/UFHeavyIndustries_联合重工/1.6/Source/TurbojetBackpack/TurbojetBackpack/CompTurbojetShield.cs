using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TurbojetBackpack;

[StaticConstructorOnStartup]
public class CompTurbojetShield : ThingComp
{
	private float energy;

	private int ticksToReset = -1;

	private ShieldState shieldState = ShieldState.Disabled;

	private Material shieldMat;

	public CompProperties_TurbojetShield Props => (CompProperties_TurbojetShield)props;

	public float Energy => energy;

	public float MaxEnergy => Props.maxEnergy;

	public ShieldState State => shieldState;

	public Material ShieldMat
	{
		get
		{
			if (shieldMat == null)
			{
				shieldMat = MaterialPool.MatFrom(Props.shieldTexPath, ShaderDatabase.Transparent, Props.shieldColor);
			}
			return shieldMat;
		}
	}

	public override void CompTick()
	{
		base.CompTick();
		Pawn wearer = ((Apparel)parent).Wearer;
		if (wearer == null || wearer.Dead)
		{
			return;
		}
		if (wearer.Drafted)
		{
			if (shieldState == ShieldState.Disabled)
			{
				shieldState = ShieldState.Active;
				energy = Props.maxEnergy;
				ticksToReset = -1;
				SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(wearer.Position, wearer.Map));
			}
			else if (shieldState == ShieldState.Resetting)
			{
				ticksToReset--;
				if (ticksToReset <= 0)
				{
					ResetShield();
				}
			}
			else if (shieldState == ShieldState.Active && energy < Props.maxEnergy)
			{
				energy += Props.energyRegenRate;
			}
		}
		else if (shieldState != ShieldState.Disabled)
		{
			shieldState = ShieldState.Disabled;
			energy = 0f;
		}
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		absorbed = false;
		if (shieldState != ShieldState.Active || (dinfo.Def != null && !dinfo.Def.harmsHealth) || dinfo.Def == DamageDefOf.SurgicalCut || dinfo.IgnoreArmor)
		{
			return;
		}
		Pawn wearer = ((Apparel)parent).Wearer;
		if (wearer == null)
		{
			return;
		}
		float num = dinfo.Amount;
		if (num < Props.minDamageThreshold)
		{
			absorbed = true;
			FleckMaker.ThrowMicroSparks(wearer.DrawPos, wearer.Map);
			return;
		}
		if (num > Props.maxDamageCap)
		{
			num = Props.maxDamageCap;
		}
		if (dinfo.Def == DamageDefOf.EMP)
		{
			num *= 1.5f;
		}
		float num2 = num * Props.energyLossPerDamage;
		if (energy >= num2)
		{
			energy -= num2;
			absorbed = true;
			SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(wearer.Position, wearer.Map));
			FleckMaker.Static(wearer.Position, wearer.Map, FleckDefOf.ExplosionFlash, 12f);
		}
		else
		{
			absorbed = true;
			energy = 0f;
			BreakShield();
		}
	}

	private void BreakShield()
	{
		Pawn wearer = ((Apparel)parent).Wearer;
		SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail("EnergyShield_Broken") ?? SoundDefOf.EnergyShield_Reset;
		soundDef.PlayOneShot(new TargetInfo(wearer.Position, wearer.Map));
		FleckMaker.Static(wearer.Position, wearer.Map, FleckDefOf.ExplosionFlash, 12f);
		for (int i = 0; i < 6; i++)
		{
			FleckMaker.ThrowMicroSparks(wearer.DrawPos + Vector3.up * 0.5f, wearer.Map);
		}
		shieldState = ShieldState.Resetting;
		ticksToReset = Props.resetDelayTicks;
	}

	private void ResetShield()
	{
		shieldState = ShieldState.Active;
		energy = Props.maxEnergy * 0.2f;
		Pawn wearer = ((Apparel)parent).Wearer;
		SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(wearer.Position, wearer.Map));
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		if (((Apparel)parent).Wearer.IsColonistPlayerControlled && shieldState != ShieldState.Disabled)
		{
			yield return new Gizmo_TurbojetShieldStatus
			{
				shield = this
			};
		}
	}

	public void DrawWornExtras()
	{
		if (shieldState == ShieldState.Active && ShouldDisplayShield())
		{
			Pawn wearer = ((Apparel)parent).Wearer;
			Vector3 drawPos = wearer.Drawer.DrawPos;
			CompTurbojetFlight flightComp = TurbojetGlobal.GetFlightComp(wearer);
			if (flightComp != null && flightComp.CurrentHeight > 0.01f)
			{
				float num = flightComp.CurrentHeight + flightComp.GetBreathingOffset();
				drawPos.z += num;
				drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			}
			else
			{
				drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			}
			float t = energy / Props.maxEnergy;
			float num2 = Mathf.Lerp(Props.minDrawSize, Props.maxDrawSize, t);
			float num3 = Mathf.Sin((float)GenTicks.TicksGame / 20f) * 0.05f;
			float num4 = num2 + num3;
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(num4, 1f, num4));
			Graphics.DrawMesh(MeshPool.plane10, matrix, ShieldMat, 0);
		}
	}

	private bool ShouldDisplayShield()
	{
		Pawn wearer = ((Apparel)parent).Wearer;
		return wearer.Spawned && !wearer.Dead && !wearer.Downed;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref energy, "energy", 0f);
		Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
		Scribe_Values.Look(ref shieldState, "shieldState", ShieldState.Disabled);
	}
}
