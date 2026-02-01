using RimWorld;
using UnityEngine;
using VEF.Hediffs;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

[StaticConstructorOnStartup]
public class HediffComp_InfinitePower : HediffComp_Draw
{
	private static readonly Material OVERLAY = MaterialPool.MatFrom("Effects/Technomancer/Power/InfinitePowerOverlay", ShaderDatabase.MetaOverlay);

	private CompPowerTrader compPower;

	private Building_MechCharger fakeCharger;

	private Need_MechEnergy needPower;

	private Thing target;

	public override bool CompShouldRemove
	{
		get
		{
			bool flag = ((HediffComp)this).CompShouldRemove;
			if (!flag)
			{
				Thing thing = target;
				bool flag2 = ((thing == null || !thing.Spawned) ? true : false);
				flag = flag2;
			}
			return flag;
		}
	}

	public void Begin(Thing t)
	{
		target = t;
		compPower = t.TryGetComp<CompPowerTrader>();
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
		if (compPower != null)
		{
			compPower.PowerOn = true;
			compPower.PowerOutput = 0f;
		}
		Need_MechEnergy need_MechEnergy = needPower;
		if (need_MechEnergy != null && need_MechEnergy.currentCharger == null)
		{
			needPower.currentCharger = fakeCharger ?? (fakeCharger = new Building_MechCharger());
		}
	}

	public override void DrawAt(Vector3 drawPos)
	{
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(target.DrawPos.Yto0() + Vector3.up * AltitudeLayer.MetaOverlays.AltitudeFor(), Quaternion.AngleAxis(0f, Vector3.up), Vector3.one), OVERLAY, 0);
	}

	public override void CompPostPostRemoved()
	{
		((HediffComp_Draw)this).CompPostPostRemoved();
		compPower?.SetUpPowerVars();
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
	}

	public override void CompExposeData()
	{
		((HediffComp)this).CompExposeData();
		Scribe_References.Look(ref target, "target");
		Scribe_References.Look(ref fakeCharger, "fakeCharger");
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			compPower = target.TryGetComp<CompPowerTrader>();
			needPower = (target as Pawn)?.needs?.energy;
			Need_MechEnergy need_MechEnergy = needPower;
			if (need_MechEnergy != null && need_MechEnergy.currentCharger == null)
			{
				needPower.currentCharger = fakeCharger ?? (fakeCharger = new Building_MechCharger());
			}
		}
	}
}
