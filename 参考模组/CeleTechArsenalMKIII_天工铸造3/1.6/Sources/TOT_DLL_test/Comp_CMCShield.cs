using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Comp_CMCShield : ThingComp
{
	protected float energy;

	protected int ticksToReset = -1;

	protected int lastKeepDisplayTick = -9999;

	private Vector3 impactAngleVect;

	private int lastAbsorbDamageTick = -9999;

	private readonly int KeepDisplayingTicks = 1000;

	private readonly float ApparelScorePerEnergyMax = 0.25f;

	public static readonly Material BubbleMat_CMC = MaterialPool.MatFrom("Things/CMC_ShieldBubble", ShaderDatabase.MoteGlow);

	public float currentangle = 0f;

	public CompProperties_CMCShield Props => (CompProperties_CMCShield)props;

	private float EnergyMax => parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax);

	private float EnergyGainPerTick => parent.GetStatValue(StatDefOf.EnergyShieldRechargeRate) / 60f;

	public float Energy => energy;

	public ShieldState ShieldState
	{
		get
		{
			if (parent is Pawn)
			{
				return ShieldState.Disabled;
			}
			if (ticksToReset <= 0)
			{
				return ShieldState.Active;
			}
			return ShieldState.Resetting;
		}
	}

	protected bool ShouldDisplay => PawnOwner.Spawned && !PawnOwner.DeadOrDowned && (PawnOwner.Drafted || (PawnOwner.Faction.HostileTo(Faction.OfPlayer) && !PawnOwner.IsPrisoner) || (ModsConfig.BiotechActive && PawnOwner.IsColonyMech && Find.Selector.SingleSelectedThing == PawnOwner));

	protected Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}

	public bool IsApparel => parent is Apparel;

	private bool IsBuiltIn => !IsApparel;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref energy, "energy", 0f);
		Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		if (IsApparel)
		{
			foreach (Gizmo gizmo in GetGizmos())
			{
				yield return gizmo;
			}
		}
		if (!DebugSettings.ShowDevGizmos)
		{
			yield break;
		}
		yield return new Command_Action
		{
			defaultLabel = "DEV: Break",
			action = Break
		};
		if (ticksToReset > 0)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Clear reset",
				action = delegate
				{
					ticksToReset = 0;
				}
			};
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (!IsBuiltIn)
		{
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	private IEnumerable<Gizmo> GetGizmos()
	{
		int num;
		if (PawnOwner.Faction != Faction.OfPlayer)
		{
			Pawn pawn2;
			Pawn pawn = (pawn2 = parent as Pawn);
			if (pawn2 == null || !pawn.RaceProps.IsMechanoid)
			{
				num = 0;
				goto IL_0081;
			}
		}
		num = ((Find.Selector.SingleSelectedThing == PawnOwner) ? 1 : 0);
		goto IL_0081;
		IL_0081:
		if (num != 0)
		{
			yield return new Gizmo_EnergyShieldStatus
			{
				shield = this
			};
		}
	}

	public override float CompGetSpecialApparelScoreOffset()
	{
		return EnergyMax * ApparelScorePerEnergyMax;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (PawnOwner == null)
		{
			energy = 0f;
		}
		else if (ShieldState == ShieldState.Resetting)
		{
			ticksToReset--;
			if (ticksToReset <= 0)
			{
				Reset();
			}
		}
		else if (ShieldState == ShieldState.Active)
		{
			energy += EnergyGainPerTick;
			if (energy > EnergyMax)
			{
				energy = EnergyMax;
			}
		}
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		absorbed = false;
		FloatRange floatRange = new FloatRange(0f, 1f);
		if (ShieldState == ShieldState.Active && PawnOwner != null && dinfo.Def.defName != DamageDefOf.SurgicalCut.defName)
		{
			float randomInRange = floatRange.RandomInRange;
			float num = ((!(randomInRange > 0.75f) || !(dinfo.Amount >= 70f)) ? dinfo.Amount : 10f);
			energy -= num * Props.energyLossPerDamage;
			if (energy < 0f)
			{
				Break();
			}
			else
			{
				AbsorbedDamage(dinfo);
			}
			absorbed = true;
		}
	}

	public void KeepDisplaying()
	{
		lastKeepDisplayTick = Find.TickManager.TicksGame;
	}

	private void AbsorbedDamage(DamageInfo dinfo)
	{
		SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(PawnOwner.Position, PawnOwner.Map));
		impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
		Vector3 loc = PawnOwner.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
		float num = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
		FleckMaker.Static(loc, PawnOwner.Map, FleckDefOf.ExplosionFlash, num);
		int num2 = (int)num;
		for (int i = 0; i < num2; i++)
		{
			FleckMaker.ThrowDustPuff(loc, PawnOwner.Map, Rand.Range(0.3f, 0.6f));
		}
		lastAbsorbDamageTick = Find.TickManager.TicksGame;
		KeepDisplaying();
	}

	private void Break()
	{
		float scale = Mathf.Lerp(Props.minDrawSize, Props.maxDrawSize, energy);
		RimWorld.EffecterDefOf.Shield_Break.SpawnAttached(parent, parent.MapHeld, scale);
		FleckMaker.Static(PawnOwner.TrueCenter(), PawnOwner.Map, FleckDefOf.ExplosionFlash, 12f);
		for (int i = 0; i < 6; i++)
		{
			FleckMaker.ThrowDustPuff(PawnOwner.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), PawnOwner.Map, Rand.Range(0.8f, 1.2f));
		}
		energy = 0f;
		if (IsApparel)
		{
			Apparel apparel = (Apparel)parent;
			Pawn wearer = apparel.Wearer;
			if (wearer != null)
			{
				HediffDef named = DefDatabase<HediffDef>.GetNamed("CMC_DMGAbsorb");
				Hediff hediff = HediffMaker.MakeHediff(named, wearer);
				hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 180;
				wearer.health.AddHediff(hediff);
			}
		}
		ticksToReset = Props.startingTicksToReset;
	}

	private void Reset()
	{
		if (PawnOwner.Spawned)
		{
			SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(PawnOwner.Position, PawnOwner.Map));
			FleckMaker.ThrowLightningGlow(PawnOwner.TrueCenter(), PawnOwner.Map, 3f);
		}
		ticksToReset = -1;
		energy = Props.energyOnReset;
	}

	public override void CompDrawWornExtras()
	{
		base.CompDrawWornExtras();
		if (IsApparel)
		{
			Draw();
		}
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (IsBuiltIn)
		{
			Draw();
		}
	}

	private void Draw()
	{
		if (ShieldState == ShieldState.Active && ShouldDisplay)
		{
			float num = Mathf.Lerp(Props.minDrawSize, Props.maxDrawSize, energy);
			Vector3 drawPos = PawnOwner.Drawer.DrawPos;
			drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			int num2 = Find.TickManager.TicksGame - lastAbsorbDamageTick;
			if (num2 < 8)
			{
				float num3 = (float)(8 - num2) / 8f * 0.05f;
				drawPos += impactAngleVect * num3;
				num -= num3;
			}
			float angle = currentangle;
			currentangle = (currentangle + 0.6f) % 360f;
			Vector3 s = new Vector3(num, 1f, num);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, BubbleMat_CMC, 0);
		}
	}

	public override bool CompAllowVerbCast(Verb verb)
	{
		return true;
	}
}
