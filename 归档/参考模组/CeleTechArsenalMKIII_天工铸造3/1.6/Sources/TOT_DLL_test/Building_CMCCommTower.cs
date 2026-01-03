using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_CMCCommTower : Building
{
	public static readonly Texture2D CallAuxTex = ContentFinder<Texture2D>.Get("UI/UI_CallAuxTex");

	public static readonly Texture2D CallAuxTex_CD = ContentFinder<Texture2D>.Get("UI/UI_CallAuxTex_CD");

	public static readonly Texture2D CallArsTex = ContentFinder<Texture2D>.Get("UI/UI_CallArsTex");

	public static readonly Texture2D CallArsTex_CD = ContentFinder<Texture2D>.Get("UI/UI_CallArsTex_CD");

	public static readonly Texture2D CallSSTex = ContentFinder<Texture2D>.Get("UI/UI_CallArsTex");

	public static readonly Texture2D CallSSTex_CD = ContentFinder<Texture2D>.Get("UI/UI_CallArsTex_CD");

	public static readonly Material CommTower_Light = MaterialPool.MatFrom("Things/Buildings/CommTower_Light", ShaderDatabase.MoteGlow);

	private static readonly Vector3 vec = new Vector3(2f, 1f, 8f);

	private CompPowerTrader compPowerTrader;

	private static readonly int AuxCD = 48;

	private static readonly int ArsCD = 96;

	private static readonly int SSCD = 1200;

	private bool IsPowered()
	{
		if (compPowerTrader == null)
		{
			compPowerTrader = this.TryGetComp<CompPowerTrader>();
		}
		return compPowerTrader.PowerOn;
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		if (!IsPowered())
		{
			yield break;
		}
		if (GameComponent_CeleTech.Instance.LastAuxHr >= AuxCD)
		{
			yield return new Command_Action
			{
				defaultLabel = "CMC_CallAuxTrader".Translate(),
				icon = CallAuxTex,
				action = delegate
				{
					SpawnTradeShip_Supply spawnTradeShip_Supply = new SpawnTradeShip_Supply();
					spawnTradeShip_Supply.SpawnShip();
					GameComponent_CeleTech.Instance.LastAuxHr = 0;
				}
			};
		}
		else
		{
			yield return new Command_Action
			{
				defaultLabel = "CMC_CallAuxTrader".Translate(),
				icon = CallAuxTex_CD,
				action = delegate
				{
					Messages.Message("Message_CMC_TraderCD".Translate(AuxCD - GameComponent_CeleTech.Instance.LastAuxHr), MessageTypeDefOf.SilentInput);
				}
			};
		}
		if (GameComponent_CeleTech.Instance.LastArsHr >= ArsCD)
		{
			yield return new Command_Action
			{
				defaultLabel = "CMC_CallArsTrader".Translate(),
				icon = CallArsTex,
				action = delegate
				{
					SpawnTradeShip_Arsenal spawnTradeShip_Arsenal = new SpawnTradeShip_Arsenal();
					spawnTradeShip_Arsenal.SpawnShip();
					GameComponent_CeleTech.Instance.LastArsHr = 0;
				}
			};
		}
		else
		{
			yield return new Command_Action
			{
				defaultLabel = "CMC_CallArsTrader".Translate(),
				icon = CallArsTex_CD,
				action = delegate
				{
					Messages.Message("Message_CMC_TraderCD".Translate(ArsCD - GameComponent_CeleTech.Instance.LastArsHr), MessageTypeDefOf.SilentInput);
				}
			};
		}
		if (GameComponent_CeleTech.Instance.LastSSHr >= SSCD)
		{
			yield return new Command_Action
			{
				defaultLabel = "CMC_CallSSTrader".Translate(),
				icon = CallSSTex,
				action = delegate
				{
					SpawnTradeShip_Slave spawnTradeShip_Slave = new SpawnTradeShip_Slave();
					spawnTradeShip_Slave.SpawnShip();
					GameComponent_CeleTech.Instance.LastSSHr = 0;
				}
			};
		}
		else
		{
			yield return new Command_Action
			{
				defaultLabel = "CMC_CallSSTrader".Translate(),
				icon = CallSSTex_CD,
				action = delegate
				{
					int num = SSCD - GameComponent_CeleTech.Instance.LastArsHr;
					Messages.Message("Message_CMC_TraderCD".Translate(num / 24, num % 24), MessageTypeDefOf.SilentInput);
				}
			};
		}
		if (DebugSettings.godMode)
		{
			yield return new Command_Action
			{
				defaultLabel = "Debug resetCD".Translate(),
				action = delegate
				{
					GameComponent_CeleTech.Instance.LastAuxHr = AuxCD;
					GameComponent_CeleTech.Instance.LastArsHr = ArsCD;
					GameComponent_CeleTech.Instance.LastSSHr = SSCD;
				}
			};
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 pos = DrawPos + Altitudes.AltIncVect + def.graphicData.drawOffset;
		pos.y = AltitudeLayer.Building.AltitudeFor() + 1.1f;
		matrix.SetTRS(pos, Quaternion.identity, vec);
		Graphics.DrawMesh(MeshPool.plane10, matrix, CommTower_Light, 0);
	}
}
