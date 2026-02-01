using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded.Nightstalker;

[HarmonyPatch]
public class Decoy : ThingWithComps, IAttackTarget, ILoadReferenceable
{
	private static readonly HashSet<Map> mapsWithDecoys = new HashSet<Map>();

	private Pawn pawn;

	public Thing Thing => this;

	public LocalTargetInfo TargetCurrentlyAimingAt => LocalTargetInfo.Invalid;

	public float TargetPriorityFactor => float.MaxValue;

	public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
	{
		return false;
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		this.pawn = base.GetComp<CompAbilitySpawn>().pawn;
		mapsWithDecoys.Add(map);
		base.SpawnSetup(map, respawningAfterLoad);
		foreach (IAttackTarget item in this.pawn.Map.attackTargetsCache.TargetsHostileToFaction(this.pawn.Faction))
		{
			if (item.Thing is Pawn { CurJobDef: var curJobDef } pawn && (curJobDef == JobDefOf.Wait_Combat || curJobDef == JobDefOf.Goto))
			{
				pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		}
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		if (!base.Map.listerThings.AllThings.OfType<Decoy>().Except(this).Any())
		{
			mapsWithDecoys.Remove(base.Map);
		}
		base.DeSpawn(mode);
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		DecoyOverlayUtility.DrawOverlay = true;
		pawn.Drawer.renderer.RenderPawnAt(drawLoc, Rot4.South);
		DecoyOverlayUtility.DrawOverlay = false;
	}

	[HarmonyPatch(typeof(DamageFlasher), "GetDamagedMat")]
	[HarmonyPrefix]
	private static void GetDuplicateMat(ref Material baseMat)
	{
		if (DecoyOverlayUtility.DrawOverlay)
		{
			baseMat = DecoyOverlayUtility.GetDuplicateMat(baseMat);
		}
	}

	[HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget")]
	[HarmonyPrefix]
	public static bool BestAttackTarget_Prefix(IAttackTargetSearcher searcher, ref IAttackTarget __result)
	{
		if (!mapsWithDecoys.Contains(searcher.Thing.MapHeld))
		{
			return true;
		}
		List<Decoy> list = searcher.Thing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher).OfType<Decoy>().ToList();
		if (list.NullOrEmpty())
		{
			return true;
		}
		__result = list.RandomElement();
		return false;
	}

	[HarmonyPatch(typeof(JobGiver_AIFightEnemy), "UpdateEnemyTarget")]
	[HarmonyPrefix]
	public static void UpdateEnemyTarget_Prefix(Pawn pawn)
	{
		Thing enemyTarget = pawn.mindState.enemyTarget;
		if (enemyTarget != null && !(enemyTarget is Decoy) && mapsWithDecoys.Contains(pawn.MapHeld))
		{
			pawn.mindState.enemyTarget = null;
		}
	}
}
