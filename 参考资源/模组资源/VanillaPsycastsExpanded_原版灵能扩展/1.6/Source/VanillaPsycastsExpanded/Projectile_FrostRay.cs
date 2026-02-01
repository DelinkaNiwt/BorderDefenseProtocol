using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using VanillaPsycastsExpanded.Graphics;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded;

[StaticConstructorOnStartup]
public class Projectile_FrostRay : Projectile
{
	private static readonly Material shadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);

	public static Func<Projectile, float> ArcHeightFactor = (Func<Projectile, float>)Delegate.CreateDelegate(typeof(Func<Projectile, float>), null, AccessTools.Method(typeof(Projectile), "get_ArcHeightFactor"));

	private Sustainer sustainer;

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		float num = ArcHeightFactor(this) * GenMath.InverseParabola(base.DistanceCoveredFraction);
		float y = Vector3.Distance(origin.Yto0(), drawLoc.Yto0());
		Vector3 vector = Vector3.Lerp(origin, drawLoc, 0.5f);
		vector.y += 5f;
		Vector3 position = vector + new Vector3(0f, 0f, 1f) * num;
		if (def.projectile.shadowSize > 0f)
		{
			DrawShadow(vector, num);
		}
		Comps_PostDraw();
		UnityEngine.Graphics.DrawMesh(MeshPool.GridPlane(new Vector2(5f, y)), position, ExactRotation, (Graphic as Graphic_Animated).MatSingle, 0);
	}

	protected override void Tick()
	{
		base.Tick();
		if (sustainer == null || sustainer.Ended)
		{
			sustainer = VPE_DefOf.VPE_FrostRay_Sustainer.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
		}
		sustainer.Maintain();
		if (launcher is Pawn pawn && (pawn.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_FrostRay) == null || pawn.Downed || pawn.Dead))
		{
			Destroy();
		}
		else
		{
			if (!this.IsHashIntervalTick(10))
			{
				return;
			}
			ShootLine resultingLine = new ShootLine(origin.ToIntVec3(), DrawPos.ToIntVec3());
			IEnumerable<IntVec3> enumerable = from x in resultingLine.Points()
				where x != resultingLine.Source
				select x;
			HashSet<Pawn> hashSet = new HashSet<Pawn>();
			foreach (IntVec3 item in enumerable)
			{
				foreach (Pawn item2 in item.GetThingList(base.Map).OfType<Pawn>())
				{
					hashSet.Add(item2);
				}
			}
			foreach (Pawn item3 in hashSet)
			{
				BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, item3, intendedTarget.Thing, launcher.def, def, targetCoverDef);
				Find.BattleLog.Add(battleLogEntry_RangedImpact);
				DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
				item3.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
				if (item3.CanReceiveHypothermia(out var hypothermiaHediff))
				{
					HealthUtility.AdjustSeverity(item3, hypothermiaHediff, 0.013333333f);
				}
				HealthUtility.AdjustSeverity(item3, VPE_DefOf.VFEP_HypothermicSlowdown, 0.013333333f);
			}
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
	}

	private void DrawShadow(Vector3 drawLoc, float height)
	{
		if (!(shadowMaterial == null))
		{
			float num = def.projectile.shadowSize * Mathf.Lerp(1f, 0.6f, height);
			Vector3 s = new Vector3(num, 1f, num);
			Vector3 vector = new Vector3(0f, -0.01f, 0f);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawLoc + vector, Quaternion.identity, s);
			UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
		}
	}
}
