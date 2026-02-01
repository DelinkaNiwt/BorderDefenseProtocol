using RimWorld;
using UnityEngine;
using VEF.Hediffs;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Staticlord;

[StaticConstructorOnStartup]
public class HediffComp_Recharge : HediffComp_Draw
{
	private const float ChargePerTickMech = 0.00083333335f;

	private const float ChargePerTickBattery = 3.3333333f;

	private CompPowerBattery compPower;

	private Building_MechCharger fakeCharger;

	private Need_MechEnergy needPower;

	private Sustainer sustainer;

	private Thing target;

	public void Init(Thing t)
	{
		target = t;
		compPower = t.TryGetComp<CompPowerBattery>();
		needPower = (t as Pawn)?.needs?.energy;
		Need_MechEnergy need_MechEnergy = needPower;
		if (need_MechEnergy != null && need_MechEnergy.currentCharger == null)
		{
			needPower.currentCharger = fakeCharger ?? (fakeCharger = new Building_MechCharger());
		}
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		((HediffComp)this).CompPostTick(ref severityAdjustment);
		if (sustainer == null)
		{
			sustainer = VPE_DefOf.VPE_Recharge_Sustainer.TrySpawnSustainer(((HediffComp)this).Pawn);
		}
		sustainer?.Maintain();
		compPower?.AddEnergy(3.3333333f);
		if (needPower != null)
		{
			needPower.CurLevel += 0.00083333335f;
		}
		Need_MechEnergy need_MechEnergy = needPower;
		if (need_MechEnergy != null && need_MechEnergy.currentCharger == null)
		{
			needPower.currentCharger = fakeCharger ?? (fakeCharger = new Building_MechCharger());
		}
	}

	public override void CompPostPostRemoved()
	{
		sustainer.End();
		Need_MechEnergy need_MechEnergy = needPower;
		if (need_MechEnergy != null)
		{
			Building_MechCharger currentCharger = need_MechEnergy.currentCharger;
			if (currentCharger == fakeCharger)
			{
				needPower.currentCharger = null;
			}
		}
		fakeCharger = null;
		((HediffComp_Draw)this).CompPostPostRemoved();
	}

	public override void DrawAt(Vector3 drawPos)
	{
		Vector3 vector = target.TrueCenter();
		Vector3 s = new Vector3(((HediffComp_Draw)this).Graphic.drawSize.x, 1f, (vector - drawPos).magnitude);
		Matrix4x4 matrix = Matrix4x4.TRS(drawPos + (vector - drawPos) / 2f, Quaternion.LookRotation(vector - drawPos), s);
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, ((HediffComp_Draw)this).Graphic.MatSingle, 0);
	}

	public override void CompExposeData()
	{
		((HediffComp)this).CompExposeData();
		Scribe_References.Look(ref target, "target");
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			compPower = target.TryGetComp<CompPowerBattery>();
			needPower = (target as Pawn)?.needs?.energy;
			Need_MechEnergy need_MechEnergy = needPower;
			if (need_MechEnergy != null && need_MechEnergy.currentCharger == null)
			{
				needPower.currentCharger = fakeCharger ?? (fakeCharger = new Building_MechCharger());
			}
		}
	}
}
