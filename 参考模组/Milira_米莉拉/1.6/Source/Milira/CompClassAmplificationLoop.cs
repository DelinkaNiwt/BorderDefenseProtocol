using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace Milira;

[StaticConstructorOnStartup]
public class CompClassAmplificationLoop : ThingComp
{
	private int tickCounter = 0;

	private int num = 0;

	private int numDraw = 0;

	private bool flag = true;

	private List<Pawn> items = new List<Pawn>();

	private float floatOffset = 0f;

	private static readonly Material BishopGraphic = MaterialPool.MatFrom("Milira/Effect/Promotion/Promotion_Bishop", ShaderDatabase.MoteGlow, Color.white);

	private static readonly Material KnightGraphic = MaterialPool.MatFrom("Milira/Effect/Promotion/Promotion_Knight", ShaderDatabase.MoteGlow, Color.white);

	private static readonly Material RookGraphic = MaterialPool.MatFrom("Milira/Effect/Promotion/Promotion_Rook", ShaderDatabase.MoteGlow, Color.white);

	private static readonly Material PawnGraphic = MaterialPool.MatFrom("Milira/Effect/Promotion/Promotion_Pawn", ShaderDatabase.MoteGlow, Color.white);

	private static readonly Material KingGraphic = MaterialPool.MatFrom("Milira/Effect/Promotion/Promotion_King", ShaderDatabase.MoteGlow, Color.white);

	private GraphicData graphicData => Props.graphicData;

	public CompProperties_ClassAmplificationLoop Props => (CompProperties_ClassAmplificationLoop)props;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
	}

	private bool IsPawnAffected(Pawn target)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		if (target.Dead || target.health == null)
		{
			return false;
		}
		if (target.Faction != parent.Faction || parent.Faction != faction)
		{
			return false;
		}
		if (target.RaceProps.IsMechanoid)
		{
			return true;
		}
		return false;
	}

	public override void CompTick()
	{
		base.CompTick();
		floatOffset = Mathf.Sin((float)Find.TickManager.TicksGame * Props.floatSpeed) * Props.floatAmplitude;
		if (!parent.IsHashIntervalTick(Props.checkInterval) || !parent.Spawned)
		{
			return;
		}
		tickCounter += Props.checkInterval;
		if (tickCounter < Props.amplificationTickDuration)
		{
			return;
		}
		if (items.Count > 0)
		{
			foreach (Pawn item in items)
			{
				Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Pawn);
				Hediff firstHediffOfDef2 = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Knight);
				Hediff firstHediffOfDef3 = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Bishop);
				Hediff firstHediffOfDef4 = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Rook);
				if (firstHediffOfDef != null)
				{
					firstHediffOfDef.Severity = 0.1f;
				}
				if (firstHediffOfDef2 != null)
				{
					firstHediffOfDef2.Severity = 0.1f;
				}
				if (firstHediffOfDef3 != null)
				{
					firstHediffOfDef3.Severity = 0.1f;
				}
				if (firstHediffOfDef4 != null)
				{
					firstHediffOfDef4.Severity = 0.1f;
				}
				HealthUtility.AdjustSeverity(item, MiliraDefOf.Milian_KingTuningWeak, 1f);
			}
			if (flag)
			{
				CallBackPawn(items);
			}
			items.Clear();
			flag = false;
		}
		int num = Rand.Range(1, 3);
		if (this.num == 0)
		{
			this.num += num;
			this.num = ((this.num % 4 != 0) ? (this.num % 4) : 0);
			numDraw = 1;
			items.Clear();
			Method1_Pawn();
			FleckMaker.Static(parent.Position, parent.Map, MiliraDefOf.Milira_DropPodDistortion);
			flag = true;
			Pawn pawn = parent as Pawn;
			Hediff firstHediffOfDef5 = pawn.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_King);
			if (firstHediffOfDef5 != null)
			{
				firstHediffOfDef5.Severity = 0.25f;
			}
		}
		else if (this.num == 1)
		{
			this.num += num;
			this.num = ((this.num % 4 != 0) ? (this.num % 4) : 0);
			numDraw = 2;
			items.Clear();
			Method2_Knight();
			FleckMaker.Static(parent.Position, parent.Map, MiliraDefOf.Milira_DropPodDistortion);
			flag = true;
			Pawn pawn2 = parent as Pawn;
			Hediff firstHediffOfDef6 = pawn2.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_King);
			if (firstHediffOfDef6 != null)
			{
				firstHediffOfDef6.Severity = 0.5f;
			}
		}
		else if (this.num == 2)
		{
			this.num += num;
			this.num = ((this.num % 4 != 0) ? (this.num % 4) : 0);
			numDraw = 3;
			items.Clear();
			Method3_Bishop();
			FleckMaker.Static(parent.Position, parent.Map, MiliraDefOf.Milira_DropPodDistortion);
			flag = true;
			Pawn pawn3 = parent as Pawn;
			Hediff firstHediffOfDef7 = pawn3.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_King);
			if (firstHediffOfDef7 != null)
			{
				firstHediffOfDef7.Severity = 0.75f;
			}
		}
		else if (this.num == 3)
		{
			this.num += num;
			this.num = ((this.num % 4 != 0) ? (this.num % 4) : 0);
			numDraw = 4;
			items.Clear();
			Method4_Rook();
			FleckMaker.Static(parent.Position, parent.Map, MiliraDefOf.Milira_DropPodDistortion);
			flag = true;
			Pawn pawn4 = parent as Pawn;
			Hediff firstHediffOfDef8 = pawn4.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_King);
			if (firstHediffOfDef8 != null)
			{
				firstHediffOfDef8.Severity = 1f;
			}
		}
		tickCounter = 0;
	}

	private void CallBackPawn(List<Pawn> pawns)
	{
		flag = false;
		Log.Message("pawnsCount" + pawns.Count);
		foreach (Pawn pawn in pawns)
		{
			if (pawn != null && !pawn.Dead && pawn.health != null && !pawn.Downed && pawn.Spawned)
			{
				Log.Message("1");
				if (!(pawn.Position.DistanceTo(parent.Position) > 4f))
				{
					break;
				}
				FleckMaker.Static(pawn.Position, pawn.Map, MiliraDefOf.Milira_KingTuning);
				MiliraDefOf.Milira_Building_BeaconActivated.PlayOneShot(new TargetInfo(pawn.Position, parent.Map));
				pawn.Position = CellFinder.RandomClosewalkCellNear(parent.Position, parent.Map, 3);
				pawn.pather.StopDead();
				pawn.jobs.StopAll();
			}
		}
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (numDraw == 1)
		{
			Mesh mesh = graphicData.Graphic.MeshAt(parent.Rotation);
			Vector3 drawPos = parent.DrawPos;
			drawPos.z = parent.DrawPos.z + floatOffset;
			drawPos.y = Props.altitudeLayer.AltitudeFor();
			Graphics.DrawMesh(mesh, drawPos + graphicData.drawOffset, Quaternion.identity, PawnGraphic, 0);
		}
		else if (numDraw == 2)
		{
			Mesh mesh2 = graphicData.Graphic.MeshAt(parent.Rotation);
			Vector3 drawPos2 = parent.DrawPos;
			drawPos2.z = parent.DrawPos.z + floatOffset;
			drawPos2.y = Props.altitudeLayer.AltitudeFor();
			Graphics.DrawMesh(mesh2, drawPos2 + graphicData.drawOffset, Quaternion.identity, KnightGraphic, 0);
		}
		else if (numDraw == 3)
		{
			Mesh mesh3 = graphicData.Graphic.MeshAt(parent.Rotation);
			Vector3 drawPos3 = parent.DrawPos;
			drawPos3.z = parent.DrawPos.z + floatOffset;
			drawPos3.y = Props.altitudeLayer.AltitudeFor();
			Graphics.DrawMesh(mesh3, drawPos3 + graphicData.drawOffset, Quaternion.identity, BishopGraphic, 0);
		}
		else if (numDraw == 4)
		{
			Mesh mesh4 = graphicData.Graphic.MeshAt(parent.Rotation);
			Vector3 drawPos4 = parent.DrawPos;
			drawPos4.z = parent.DrawPos.z + floatOffset;
			drawPos4.y = Props.altitudeLayer.AltitudeFor();
			Graphics.DrawMesh(mesh4, drawPos4 + graphicData.drawOffset, Quaternion.identity, RookGraphic, 0);
		}
		else
		{
			Mesh mesh5 = graphicData.Graphic.MeshAt(parent.Rotation);
			Vector3 drawPos5 = parent.DrawPos;
			drawPos5.z = parent.DrawPos.z + floatOffset;
			drawPos5.y = Props.altitudeLayer.AltitudeFor();
			Graphics.DrawMesh(mesh5, drawPos5 + graphicData.drawOffset, Quaternion.identity, KingGraphic, 0);
		}
	}

	private void Method1_Pawn()
	{
		Log.Message("Pawn");
		foreach (Pawn item in parent.Map.mapPawns.AllPawnsSpawned)
		{
			if (!IsPawnAffected(item) || (!(item.def.defName == "Milian_Mechanoid_PawnI") && !(item.def.defName == "Milian_Mechanoid_PawnII") && !(item.def.defName == "Milian_Mechanoid_PawnIII") && !(item.def.defName == "Milian_Mechanoid_PawnIV")))
			{
				continue;
			}
			Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Pawn);
			if (firstHediffOfDef != null)
			{
				if (firstHediffOfDef.Severity < 1f)
				{
					firstHediffOfDef.Severity = 1f;
				}
				items.Add(item);
				Effecter effecter = Props.effecter.Spawn();
				effecter.Trigger(item, item);
				effecter.Cleanup();
			}
		}
		if (items.Count >= 8)
		{
			return;
		}
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < 8 - items.Count; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(MiliraDefOf.Milian_Mechanoid_PawnI, parent.Faction);
			list.Add(pawn);
			Pawn p = parent as Pawn;
			Lord lord = p.GetLord();
			if (lord == null)
			{
				Log.Message("lord == null");
			}
			lord.AddPawn(pawn);
		}
		DropPodUtility.DropThingsNear(parent.Position, parent.MapHeld, list, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, parent.Faction);
	}

	private void Method2_Knight()
	{
		Log.Message("Knight");
		foreach (Pawn item in parent.Map.mapPawns.AllPawnsSpawned)
		{
			if (!IsPawnAffected(item) || (!(item.def.defName == "Milian_Mechanoid_KnightI") && !(item.def.defName == "Milian_Mechanoid_KnightII") && !(item.def.defName == "Milian_Mechanoid_KnightIII") && !(item.def.defName == "Milian_Mechanoid_KnightIV")))
			{
				continue;
			}
			Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Knight);
			if (firstHediffOfDef != null)
			{
				if (firstHediffOfDef.Severity < 1f)
				{
					firstHediffOfDef.Severity = 1f;
				}
				items.Add(item);
				Effecter effecter = Props.effecter.Spawn();
				effecter.Trigger(item, item);
				effecter.Cleanup();
			}
		}
		if (items.Count >= 2)
		{
			return;
		}
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < 2 - items.Count; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(MiliraDefOf.Milian_Mechanoid_KnightI, parent.Faction);
			list.Add(pawn);
			Pawn p = parent as Pawn;
			Lord lord = p.GetLord();
			if (lord == null)
			{
				Log.Message("lord == null");
			}
			lord.AddPawn(pawn);
		}
		DropPodUtility.DropThingsNear(parent.Position, parent.MapHeld, list, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, parent.Faction);
	}

	private void Method3_Bishop()
	{
		Log.Message("Bishop");
		foreach (Pawn item in parent.Map.mapPawns.AllPawnsSpawned)
		{
			if (!IsPawnAffected(item) || (!(item.def.defName == "Milian_Mechanoid_BishopI") && !(item.def.defName == "Milian_Mechanoid_BishopII") && !(item.def.defName == "Milian_Mechanoid_BishopIII") && !(item.def.defName == "Milian_Mechanoid_BishopIV")))
			{
				continue;
			}
			Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Bishop);
			if (firstHediffOfDef != null)
			{
				if (firstHediffOfDef.Severity < 1f)
				{
					firstHediffOfDef.Severity = 1f;
				}
				items.Add(item);
				Effecter effecter = Props.effecter.Spawn();
				effecter.Trigger(item, item);
				effecter.Cleanup();
			}
		}
		if (items.Count >= 2)
		{
			return;
		}
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < 2 - items.Count; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(MiliraDefOf.Milian_Mechanoid_BishopI, parent.Faction);
			list.Add(pawn);
			Pawn p = parent as Pawn;
			Lord lord = p.GetLord();
			if (lord == null)
			{
				Log.Message("lord == null");
			}
			lord.AddPawn(pawn);
		}
		DropPodUtility.DropThingsNear(parent.Position, parent.MapHeld, list, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, parent.Faction);
	}

	private void Method4_Rook()
	{
		Log.Message("Rook");
		foreach (Pawn item in parent.Map.mapPawns.AllPawnsSpawned)
		{
			if (!IsPawnAffected(item) || (!(item.def.defName == "Milian_Mechanoid_RookI") && !(item.def.defName == "Milian_Mechanoid_RookII") && !(item.def.defName == "Milian_Mechanoid_RookIII") && !(item.def.defName == "Milian_Mechanoid_RookIV")))
			{
				continue;
			}
			Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Rook);
			if (firstHediffOfDef != null)
			{
				if (firstHediffOfDef.Severity < 1f)
				{
					firstHediffOfDef.Severity = 1f;
				}
				items.Add(item);
				Effecter effecter = Props.effecter.Spawn();
				effecter.Trigger(item, item);
				effecter.Cleanup();
			}
		}
		if (items.Count >= 2)
		{
			return;
		}
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < 2 - items.Count; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(MiliraDefOf.Milian_Mechanoid_RookI, parent.Faction);
			list.Add(pawn);
			Pawn p = parent as Pawn;
			Lord lord = p.GetLord();
			if (lord == null)
			{
				Log.Message("lord == null");
			}
			lord.AddPawn(pawn);
		}
		DropPodUtility.DropThingsNear(parent.Position, parent.MapHeld, list, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, parent.Faction);
	}
}
