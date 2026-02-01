using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

[StaticConstructorOnStartup]
public class Hediff_BlizzardSource : Hediff_Overlay
{
	private List<Faction> affectedFactions;

	private float curAngle;

	public override string OverlayPath => "Effects/Frostshaper/Blizzard/Blizzard";

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		((Hediff)(object)this).pawn.Map.GetComponent<MapComponent_PsycastsManager>().blizzardSources.Add(this);
	}

	public override void PostRemoved()
	{
		((HediffWithComps)(object)this).PostRemoved();
		((Hediff)(object)this).pawn.Map.GetComponent<MapComponent_PsycastsManager>().blizzardSources.Remove(this);
		((Def)(object)((Hediff_Ability)this).ability?.def).GetModExtension<AbilityExtension_PsychicComa>()?.ApplyComa(((Hediff_Ability)this).ability);
	}

	public override void Tick()
	{
		((Hediff)(object)this).Tick();
		Find.CameraDriver.shaker.DoShake(2f);
		curAngle += 0.07f;
		if (curAngle > 360f)
		{
			curAngle = 0f;
		}
		if (affectedFactions == null)
		{
			affectedFactions = new List<Faction>();
		}
		foreach (IntVec3 item in (from x in GenRadial.RadialCellsAround(((Hediff)(object)this).pawn.Position, ((Hediff_Ability)this).ability.GetAdditionalRadius(), ((Hediff_Ability)this).ability.GetRadiusForPawn())
			where x.InBounds(((Hediff)(object)this).pawn.Map)
			select x).InRandomOrder().Take(Rand.RangeInclusive(9, 12)).ToList())
		{
			((Hediff)(object)this).pawn.Map.snowGrid.AddDepth(item, 0.5f);
		}
		foreach (Pawn item2 in ((Hediff_Ability)this).ability.pawn.Map.mapPawns.AllPawnsSpawned.ToList())
		{
			if (InAffectedArea(item2.Position))
			{
				Hediff firstHediffOfDef = item2.health.hediffSet.GetFirstHediffOfDef(VPE_DefOf.VPE_Blizzard);
				if (firstHediffOfDef != null)
				{
					firstHediffOfDef.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 60;
				}
				else
				{
					firstHediffOfDef = HediffMaker.MakeHediff(VPE_DefOf.VPE_Blizzard, item2);
					item2.health.AddHediff(firstHediffOfDef);
				}
				if (item2.IsHashIntervalTick(60) && item2.CanReceiveHypothermia(out var hypothermiaHediff))
				{
					HealthUtility.AdjustSeverity(item2, hypothermiaHediff, 0.02f);
					DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, Rand.RangeInclusive(1, 3));
					item2.TakeDamage(dinfo);
				}
				if (((Hediff_Ability)this).ability.pawn.Faction == Faction.OfPlayer)
				{
					AffectGoodwill(item2.HomeFaction, item2);
				}
			}
		}
	}

	public bool InAffectedArea(IntVec3 cell)
	{
		if (!cell.InHorDistOf(((Hediff_Ability)this).ability.pawn.Position, ((Hediff_Ability)this).ability.GetAdditionalRadius()))
		{
			return cell.InHorDistOf(((Hediff_Ability)this).ability.pawn.Position, ((Hediff_Ability)this).ability.GetRadiusForPawn());
		}
		return false;
	}

	private void AffectGoodwill(Faction faction, Pawn p)
	{
		if (faction != null && !faction.IsPlayer && !faction.HostileTo(Faction.OfPlayer) && (p == null || !p.IsSlaveOfColony) && !affectedFactions.Contains(faction))
		{
			Faction.OfPlayer.TryAffectGoodwillWith(faction, ((Hediff_Ability)this).ability.def.goodwillImpact, canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.UsedHarmfulAbility);
		}
	}

	public override void Draw()
	{
		Vector3 drawPos = ((Hediff)(object)this).pawn.DrawPos;
		drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
		Matrix4x4 matrix = default(Matrix4x4);
		float num = ((Hediff_Ability)this).ability.GetRadiusForPawn() * 2f;
		matrix.SetTRS(drawPos, Quaternion.AngleAxis(curAngle, Vector3.up), new Vector3(num, 1f, num));
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, base.OverlayMat, 0, null, 0, MatPropertyBlock);
	}

	public override void ExposeData()
	{
		((Hediff_Ability)this).ExposeData();
		Scribe_Values.Look(ref curAngle, "curAngle", 0f);
	}
}
