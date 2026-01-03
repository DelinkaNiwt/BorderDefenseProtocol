using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_Bunker : Building_CMCTurretGun
{
	private CompPowerTrader powerTrader;

	private bool Isrepairing = false;

	private int LastrepTick = -1;

	public static Material repair = MaterialPool.MatFrom("Things/Buildings/CMC_ReinforcedBunker_Repair", ShaderDatabase.MoteGlow);

	public static Material TexLight = MaterialPool.MatFrom("Things/Buildings/CMC_ReinforcedBunker_Light", ShaderDatabase.MoteGlow);

	private Material CachedMat
	{
		get
		{
			string text = ((!holdFire) ? "Things/Buildings/CMC_ReinforcedBunker" : "Things/Buildings/CMC_ReinforcedBunker_Hidden");
			MaterialRequest req = new MaterialRequest(ContentFinder<Texture2D>.Get(text), ShaderDatabase.CutoutComplex, new Color(DrawColor.r, DrawColor.g, DrawColor.b, 1f));
			req.maskTex = ContentFinder<Texture2D>.Get(text + "m");
			return MaterialPool.MatFrom(req);
		}
	}

	private CompPowerTrader CompPowerTrader
	{
		get
		{
			if (CompPowerTrader != null)
			{
				return powerTrader;
			}
			powerTrader = this.TryGetComp<CompPowerTrader>();
			return powerTrader;
		}
	}

	public override Color DrawColor
	{
		get
		{
			return base.Stuff?.stuffProps.color ?? base.DrawColor;
		}
		set
		{
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		Matrix4x4 matrix = Matrix4x4.TRS(DrawPos + Altitudes.AltIncVect, Quaternion.identity, new Vector3(4.8f, 1f, 4.8f));
		Graphics.DrawMesh(MeshPool.plane10, matrix, CachedMat, 0);
		if (Isrepairing)
		{
			Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("Things/Buildings/CMC_ReinforcedBunker_Repair", ShaderDatabase.MoteGlow), 0);
		}
		if (powerTrader.PowerOn)
		{
			Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("Things/Buildings/CMC_ReinforcedBunker_Light", ShaderDatabase.MoteGlow), 0);
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		powerTrader = this.TryGetComp<CompPowerTrader>();
	}

	public override void ExposeData()
	{
		Scribe_Values.Look(ref Isrepairing, "repairing", defaultValue: false);
		Scribe_Values.Look(ref LastrepTick, "lastRepTick", -1);
		base.ExposeData();
	}

	protected override void Tick()
	{
		base.Tick();
		if (Find.TickManager.TicksGame % 600 != 0)
		{
			return;
		}
		if ((float)HitPoints < (float)base.MaxHitPoints * 0.25f && Find.TickManager.TicksGame - LastrepTick > 1800)
		{
			holdFire = true;
			LastrepTick = Find.TickManager.TicksGame;
		}
		if (HitPoints < base.MaxHitPoints)
		{
			if (holdFire && refuelableComp.Fuel >= 50f && powerTrader.PowerOn)
			{
				Isrepairing = true;
				HitPoints += Mathf.Min(base.MaxHitPoints - HitPoints, 100 + (int)((float)base.MaxHitPoints * 0.01f));
				refuelableComp.ConsumeFuel(50f);
				powerComp.PowerOutput = -800f;
			}
		}
		else
		{
			powerComp.powerOutputInt = -200f;
			Isrepairing = false;
		}
	}
}
