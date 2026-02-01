using System;
using HarmonyLib;
using Verse;
using RimWorld;
using Verse.AI.Group;
using System.Collections.Generic;
using Verse.Sound;
using System.Linq;
using System.Reflection;
using Verse.AI;

namespace GD3
{
	[HarmonyPatch(typeof(Trigger_UrgentlyHungry), "ActivateOn")]
	public static class Lord_Patch
	{
		public static bool Prefix(ref bool __result, Lord lord, TriggerSignal signal)
		{
			if (lord.faction == Faction.OfMechanoids)
			{
				__result = false;
				return false;
			}
			else
			{
				return true;
			}
		}
	}

	[HarmonyPatch(typeof(CompPawnSpawnOnWakeup), "Spawn")]
	public static class CapsuleLord_Patch
	{
		public static bool Prefix(CompPawnSpawnOnWakeup __instance)
		{
            Building building = __instance.parent as Building;
			if (building != null && !building.Map.IsPlayerHome)
			{
                Spawn(building, (CompProperties_PawnSpawnOnWakeup)__instance.props, __instance.points, __instance.spawnedPawns);
                __instance.points = 0;
                return false;
			}
			return true;
		}

        private static void Spawn(Building parent, CompProperties_PawnSpawnOnWakeup Props, float points, List<Pawn> spawnedPawns)
        {
            /*Lord lord = CompSpawnerPawn.FindLordToJoin(parent, typeof(LordJob_DefendBase), Props.shouldJoinParentLord, (Thing spawner) => spawner.TryGetComp<CompPawnSpawnOnWakeup>()?.spawnedPawns);
            if (lord == null)
            {
                lord = CompSpawnerPawn.CreateNewLord(parent, Props.aggressive, Props.defendRadius, typeof(LordJob_DefendBase));
            }*/

            //Lord lord = CompSpawnerPawn.FindLordToJoin(parent, typeof(LordJob_DefendCluster), Props.shouldJoinParentLord, (Thing spawner) => spawner.TryGetComp<CompPawnSpawnOnWakeup>()?.spawnedPawns);
            Lord lord = LordMaker.MakeNewLord(parent.Faction, new LordJob_MechanoidsDefend(new List<Thing>() { parent }, parent.Faction, 12f, parent.Position, false,false), parent.Map);
            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                lord.AddPawn(spawnedPawns[i]);
            }

            //Log.Warning(lord.LordJob.GetType().ToString());

            IntVec3 spawnPosition = GetSpawnPosition(parent, Props);
            if (!spawnPosition.IsValid)
            {
                return;
            }

            List<Thing> list = GeneratePawns(parent, Props, points);
            if (Props.dropInPods)
            {
                DropPodUtility.DropThingsNear(spawnPosition, parent.MapHeld, list, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: true, forbid: true, allowFogged: true, parent.Faction);
            }

            List<IntVec3> occupiedCells = new List<IntVec3>();
            foreach (Thing item in list)
            {
                if (!Props.dropInPods)
                {
                    IntVec3 intVec = CellFinder.RandomClosewalkCellNear(spawnPosition, parent.Map, Props.pawnSpawnRadius.RandomInRange, (IntVec3 c) => !occupiedCells.Contains(c));
                    if (!intVec.IsValid)
                    {
                        intVec = CellFinder.RandomClosewalkCellNear(spawnPosition, parent.Map, Props.pawnSpawnRadius.RandomInRange);
                    }

                    GenSpawn.Spawn(item, intVec, parent.Map);
                    occupiedCells.Add(intVec);
                }

                lord.AddPawn((Pawn)item);
                spawnedPawns.Add((Pawn)item);
                item.TryGetComp<CompCanBeDormant>()?.WakeUp();
                if (Props.mentalState != null)
                {
                    ((Pawn)item).mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.CocoonDisturbed);
                }
            }

            if (Props.spawnEffecter != null)
            {
                Effecter effecter = new Effecter(Props.spawnEffecter);
                effecter.Trigger(parent, TargetInfo.Invalid);
                effecter.Cleanup();
            }

            if (Props.spawnSound != null)
            {
                Props.spawnSound.PlayOneShot(parent);
            }

            if (Props.activatedMessageKey != null)
            {
                Messages.Message(Props.activatedMessageKey.Translate(), spawnedPawns, MessageTypeDefOf.ThreatBig);
            }

            if (Props.destroyAfterSpawn && !parent.Destroyed)
            {
                parent.Destroy();
            }
        }

        private static IntVec3 GetSpawnPosition(Building parent, CompProperties_PawnSpawnOnWakeup Props)
        {
            if (!Props.dropInPods)
            {
                return parent.Position;
            }

            Predicate<IntVec3> validator = delegate (IntVec3 c)
            {
                if (!DropCellFinder.IsGoodDropSpot(c, parent.MapHeld, allowFogged: false, canRoofPunch: true))
                {
                    return false;
                }

                float num = c.DistanceTo(parent.Position);
                return num >= (float)Props.pawnSpawnRadius.min && num <= (float)Props.pawnSpawnRadius.max;
            };
            if (CellFinder.TryFindRandomCellNear(parent.Position, parent.MapHeld, Props.pawnSpawnRadius.max, validator, out IntVec3 result))
            {
                return result;
            }

            return IntVec3.Invalid;
        }

        private static List<Thing> GeneratePawns(Building parent, CompProperties_PawnSpawnOnWakeup Props, float points)
        {
            List<Thing> list = new List<Thing>();
            float pointsLeft;
            PawnKindDef result;
            for (pointsLeft = points; pointsLeft > 0f && Props.spawnablePawnKinds.Where((PawnKindDef p) => p.combatPower <= pointsLeft).TryRandomElement(out result); pointsLeft -= result.combatPower)
            {
                int index = result.lifeStages.Count - 1;
                list.Add(PawnGenerator.GeneratePawn(new PawnGenerationRequest(result, parent.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, result.race.race.lifeStageAges[index].minAge)));
            }

            points = 0f;
            return list;
        }
    }
}