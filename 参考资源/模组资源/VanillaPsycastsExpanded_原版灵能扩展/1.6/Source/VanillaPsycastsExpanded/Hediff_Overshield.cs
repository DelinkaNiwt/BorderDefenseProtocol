using System.Linq;
using RimWorld;
using UnityEngine;
using VEF;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

[StaticConstructorOnStartup]
public class Hediff_Overshield : Hediff_Overlay
{
	private int lastInterceptTicks = -999999;

	private float lastInterceptAngle;

	private bool drawInterceptCone;

	public static float idlePulseSpeed = 3f;

	public static float minIdleAlpha = 0.05f;

	public static float minAlpha = 0.2f;

	private static Material ForceFieldConeMat = MaterialPool.MatFrom("Other/ForceFieldCone", ShaderDatabase.MoteGlow);

	public override string OverlayPath => "Other/ForceField";

	public virtual Color OverlayColor => Color.yellow;

	public override void Tick()
	{
		((Hediff)(object)this).Tick();
		if (((Hediff)(object)this).pawn.Map == null)
		{
			return;
		}
		foreach (Thing item in GenRadial.RadialDistinctThingsAround(((Hediff)(object)this).pawn.Position, ((Hediff)(object)this).pawn.Map, OverlaySize + 1f, useCenter: true))
		{
			if (item is Projectile projectile && CanDestroyProjectile(projectile))
			{
				DestroyProjectile(projectile);
			}
		}
	}

	protected virtual void DestroyProjectile(Projectile projectile)
	{
		Effecter effecter = new Effecter(VPE_DefOf.Interceptor_BlockedProjectilePsychic);
		effecter.Trigger(new TargetInfo(projectile.Position, ((Hediff)(object)this).pawn.Map), TargetInfo.Invalid);
		effecter.Cleanup();
		lastInterceptAngle = projectile.ExactPosition.AngleToFlat(((Hediff)(object)this).pawn.TrueCenter());
		lastInterceptTicks = Find.TickManager.TicksGame;
		drawInterceptCone = true;
		projectile.Destroy();
	}

	public virtual bool CanDestroyProjectile(Projectile projectile)
	{
		IntVec3 item = ((Vector3)NonPublicFields.Projectile_origin.GetValue(projectile)).Yto0().ToIntVec3();
		if (Vector3.Distance(projectile.ExactPosition.Yto0(), ((Hediff)(object)this).pawn.DrawPos.Yto0()) <= OverlaySize)
		{
			return !GenRadial.RadialCellsAround(((Hediff)(object)this).pawn.Position, OverlaySize, useCenter: true).ToList().Contains(item);
		}
		return false;
	}

	public override void Draw()
	{
		Vector3 drawPos = ((Hediff)(object)this).pawn.DrawPos;
		drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
		float currentAlpha = GetCurrentAlpha();
		if (currentAlpha > 0f)
		{
			Color overlayColor = OverlayColor;
			overlayColor.a *= currentAlpha;
			MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, overlayColor);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(OverlaySize * 2f * 1.1601562f, 1f, OverlaySize * 2f * 1.1601562f));
			UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, base.OverlayMat, 0, null, 0, MatPropertyBlock);
		}
		float currentConeAlpha_RecentlyIntercepted = GetCurrentConeAlpha_RecentlyIntercepted();
		if (currentConeAlpha_RecentlyIntercepted > 0f)
		{
			Color overlayColor2 = OverlayColor;
			overlayColor2.a *= currentConeAlpha_RecentlyIntercepted;
			MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, overlayColor2);
			Matrix4x4 matrix2 = default(Matrix4x4);
			matrix2.SetTRS(drawPos, Quaternion.Euler(0f, lastInterceptAngle - 90f, 0f), new Vector3(OverlaySize * 2f * 1.1601562f, 1f, OverlaySize * 2f * 1.1601562f));
			UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix2, ForceFieldConeMat, 0, null, 0, MatPropertyBlock);
		}
	}

	private float GetCurrentAlpha()
	{
		return Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(GetCurrentAlpha_Idle(), GetCurrentAlpha_Selected()), GetCurrentAlpha_RecentlyIntercepted()), GetCurrentAlpha_RecentlyActivated()), minAlpha);
	}

	private float GetCurrentAlpha_Idle()
	{
		if (Find.Selector.IsSelected(((Hediff)(object)this).pawn))
		{
			return 0f;
		}
		return Mathf.Lerp(minIdleAlpha, 0.11f, (Mathf.Sin((float)(Gen.HashCombineInt(((Hediff)(object)this).pawn.thingIDNumber, 96804938) % 100) + Time.realtimeSinceStartup * idlePulseSpeed) + 1f) / 2f);
	}

	private float GetCurrentAlpha_Selected()
	{
		float num = Mathf.Max(2f, idlePulseSpeed);
		if (!Find.Selector.IsSelected(((Hediff)(object)this).pawn))
		{
			return 0f;
		}
		return Mathf.Lerp(0.2f, 0.62f, (Mathf.Sin((float)(Gen.HashCombineInt(((Hediff)(object)this).pawn.thingIDNumber, 35990913) % 100) + Time.realtimeSinceStartup * num) + 1f) / 2f);
	}

	private float GetCurrentAlpha_RecentlyIntercepted()
	{
		int num = Find.TickManager.TicksGame - lastInterceptTicks;
		return Mathf.Clamp01(1f - (float)num / 40f) * 0.09f;
	}

	private float GetCurrentAlpha_RecentlyActivated()
	{
		int num = Find.TickManager.TicksGame - lastInterceptTicks;
		return Mathf.Clamp01(1f - (float)num / 50f) * 0.09f;
	}

	private float GetCurrentConeAlpha_RecentlyIntercepted()
	{
		if (!drawInterceptCone)
		{
			return 0f;
		}
		int num = Find.TickManager.TicksGame - lastInterceptTicks;
		return Mathf.Clamp01(1f - (float)num / 40f) * 0.82f;
	}

	public override void ExposeData()
	{
		((Hediff_Ability)this).ExposeData();
		Scribe_Values.Look(ref lastInterceptTicks, "lastInterceptTicks", 0);
		Scribe_Values.Look(ref lastInterceptAngle, "lastInterceptTicks", 0f);
		Scribe_Values.Look(ref drawInterceptCone, "drawInterceptCone", defaultValue: false);
	}
}
