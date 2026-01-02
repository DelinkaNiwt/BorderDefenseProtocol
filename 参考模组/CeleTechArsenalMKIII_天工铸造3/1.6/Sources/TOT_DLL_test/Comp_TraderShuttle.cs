using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
internal class Comp_TraderShuttle : ThingComp
{
	public Landed_CMCTS tradeShip;

	public bool mustCrash = false;

	private static readonly Texture2D SendAwayTexture = ContentFinder<Texture2D>.Get("UI/SendAway");

	private static Material LightTexture = MaterialPool.MatFrom("Things/Skyfaller/TradeShuttle_Light", ShaderDatabase.MoteGlow);

	private static Vector3 vec = new Vector3(5f, 0f, 5f);

	public CompProperties_TraderShuttle Props => props as CompProperties_TraderShuttle;

	public override void PostExposeData()
	{
		Scribe_Deep.Look(ref tradeShip, "ship");
	}

	public override void CompTick()
	{
		if (tradeShip.Departed)
		{
			if (parent.Spawned)
			{
				SendAway();
			}
		}
		else
		{
			tradeShip.PassingShipTick();
		}
	}

	public override string CompInspectStringExtra()
	{
		return tradeShip.def.LabelCap + "\n" + "UTSLeavingIn".Translate(tradeShip.ticksUntilDeparture.ToStringTicksToPeriod());
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (!respawningAfterLoad && !mustCrash && Props.soundThud != null)
		{
			Props.soundThud.PlayOneShot(parent);
		}
	}

	public override string TransformLabel(string label)
	{
		return tradeShip.name;
	}

	public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		bool flag = false;
		if (totalDamageDealt > 2000f)
		{
			flag = true;
		}
		else if ((double)parent.HitPoints <= (double)parent.MaxHitPoints * 0.5)
		{
			flag = true;
		}
		if (flag)
		{
			SendAway();
		}
	}

	public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn negotiator)
	{
		string label = "TradeWith".Translate(tradeShip.GetCallLabel());
		yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
		{
			Job job = JobMaker.MakeJob(CMC_Def.CMCTS_TradeWithShip, parent);
			negotiator.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}, MenuOptionPriority.InitiateSocial), negotiator, parent);
	}

	private Faction GetFaction(TraderKindDef trader)
	{
		return null;
	}

	public void GenerateInternalTradeShip(Map map, TraderKindDef traderKindDef = null)
	{
		if (traderKindDef == null)
		{
			traderKindDef = DefDatabase<TraderKindDef>.AllDefs.RandomElementByWeightWithFallback((TraderKindDef x) => x.CalculatedCommonality);
		}
		tradeShip = new Landed_CMCTS(map, traderKindDef, GetFaction(traderKindDef));
		tradeShip.passingShipManager = map.passingShipManager;
		tradeShip.name = "CMC_TradeShipName".Translate();
		tradeShip.GenerateThings();
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		yield return new Command_Action
		{
			defaultLabel = "CMC_TraderShipsSendAway".Translate(),
			defaultDesc = "CMC_TraderShipsSendAwayDesc".Translate(),
			action = SendAway,
			icon = SendAwayTexture
		};
	}

	private void SendAway()
	{
		if (!parent.Spawned)
		{
			string text = "Tried to send ";
			Log.Error(text + parent?.ToString() + " away, but it's unspawned.");
			return;
		}
		GameComponent_CeleTech.Instance.MoneySpent += Mathf.Abs(tradeShip.Silver - 100000);
		if (Mathf.Abs(tradeShip.Silver - 100000) != 0)
		{
			Messages.Message("Message_PointsAdded".Translate(Mathf.Abs(tradeShip.Silver - 100000), GameComponent_CeleTech.Instance.MoneySpent), MessageTypeDefOf.PositiveEvent);
		}
		Map map = parent.Map;
		IntVec3 position = parent.Position;
		parent.DeSpawn();
		Skyfaller skyfaller = ThingMaker.MakeThing(Props.takeoffAnimation) as Skyfaller;
		if (!skyfaller.innerContainer.TryAdd(parent))
		{
			Log.Error("Could not add " + ((Thing)parent).ToStringSafe() + " to a skyfaller.");
			parent.Destroy(DestroyMode.QuestLogic);
		}
		GenSpawn.Spawn(skyfaller, position, map);
	}

	public override void PostDraw()
	{
		base.PostDraw();
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 pos = parent.DrawPos + Altitudes.AltIncVect + parent.def.graphicData.drawOffset;
		pos.y = AltitudeLayer.Building.AltitudeFor();
		matrix.SetTRS(pos, Quaternion.identity, vec);
		Graphics.DrawMesh(MeshPool.plane10, matrix, LightTexture, 0);
	}
}
