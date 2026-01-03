using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class Building_TurretGunFortress : Building_SpinTurretGun
{
	public static readonly Material Turret = MaterialPool.MatFrom("Milira/Building/Security/MilianHeavyTurretPlasma_TopII", ShaderDatabase.Cutout, Color.white);

	private static readonly SimpleCurve ArmorToDamageReductionCurve = new SimpleCurve
	{
		new CurvePoint(0f, 0f),
		new CurvePoint(2f, 0.6f)
	};

	protected override bool CanSetForcedTarget => ((Building_SpinTurretGun)this).PlayerControlled;

	protected CompThingContainer_Milian compThingContainer => ((Thing)(object)this).TryGetComp<CompThingContainer_Milian>();

	public float damageReductionFactor
	{
		get
		{
			if (compThingContainer != null && compThingContainer.innerPawn != null)
			{
				return ArmorToDamageReductionCurve.Evaluate(TryGetOverallArmor(compThingContainer.innerPawn, StatDefOf.ArmorRating_Sharp));
			}
			return 1f;
		}
	}

	protected override void TickInterval(int delta)
	{
		((ThingWithComps)this).TickInterval(delta);
		if (((Thing)(object)this).IsHashIntervalTick(60, delta))
		{
			Modification_AutoRepair();
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Vector3 vector = new Vector3(0f, 0f, 0f);
		float num = 0f;
		EquipmentUtility.Recoil(((Thing)this).def.building.turretGunDef, (Verb_LaunchProjectile)((Building_Turret)(object)this).AttackVerb, ref vector, ref num, ((Building_SpinTurretGun)this).CurRotation);
		((Building_SpinTurretGun)this).Top.DrawTurret(drawLoc, vector, num);
		DrawTurretTop();
	}

	public virtual void DrawTurretTop()
	{
		Vector3 vector = new Vector3(((Thing)this).def.building.turretTopOffset.x, 0f, ((Thing)this).def.building.turretTopOffset.y).RotatedBy(((Building_SpinTurretGun)this).CurRotation);
		float turretTopDrawSize = ((Thing)this).def.building.turretTopDrawSize;
		float num = ((Building_Turret)this).CurrentEffectiveVerb?.AimAngleOverride ?? ((Building_SpinTurretGun)this).CurRotation;
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 drawPos = ((Thing)(object)this).DrawPos;
		drawPos.y += 0.2f;
		matrix.SetTRS(drawPos + Altitudes.AltIncVect + vector, (-90f + num).ToQuat(), new Vector3(turretTopDrawSize, 1f, turretTopDrawSize));
		Graphics.DrawMesh(MeshPool.plane10, matrix, Turret, 0);
	}

	public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		if (!dinfo.Def.isExplosive)
		{
			dinfo.SetAmount(dinfo.Amount * (1f - damageReductionFactor));
		}
		dinfo.SetAmount(dinfo.Amount * compThingContainer.innerPawn.GetStatValue(StatDefOf.IncomingDamageFactor));
		((Building_Turret)this).PreApplyDamage(ref dinfo, out absorbed);
		compThingContainer.innerPawn.PostApplyDamage(dinfo, 0f);
	}

	private float TryGetOverallArmor(Pawn pawn, StatDef stat)
	{
		float num = 0f;
		float num2 = Mathf.Clamp01(pawn.GetStatValue(stat) / 2f);
		List<BodyPartRecord> allParts = pawn.RaceProps.body.AllParts;
		List<Apparel> list = ((pawn.apparel != null) ? pawn.apparel.WornApparel : null);
		for (int i = 0; i < allParts.Count; i++)
		{
			float num3 = 1f - num2;
			if (list != null)
			{
				for (int j = 0; j < list.Count; j++)
				{
					if (list[j].def.apparel.CoversBodyPart(allParts[i]))
					{
						float num4 = Mathf.Clamp01(list[j].GetStatValue(stat) / 2f);
						num3 *= 1f - num4;
					}
				}
			}
			num += allParts[i].coverageAbs * (1f - num3);
		}
		return Mathf.Clamp(num * 2f, 0f, 2f);
	}

	public void Modification_AutoRepair()
	{
		if (ModsConfig.IsActive("Ancot.MilianModification") && compThingContainer.innerPawn.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_FortressDamageControl) != null)
		{
			((Thing)(object)this).HitPoints += 6;
			((Thing)(object)this).HitPoints = Mathf.Min(((Thing)(object)this).HitPoints, ((Thing)this).MaxHitPoints);
		}
	}
}
