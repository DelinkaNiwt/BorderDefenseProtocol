using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace Milira;

public class HediffComp_Promotion : HediffComp
{
	private HediffCompProperties_Promotion Props => (HediffCompProperties_Promotion)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		CompPromotionGraphic comp = base.Pawn.GetComp<CompPromotionGraphic>();
		CompMilianHairSwitch comp2 = base.Pawn.GetComp<CompMilianHairSwitch>();
		if (parent.Severity == 1f)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(Props.promotionPawnkind, base.Pawn.Faction);
			pawn.ageTracker.AgeBiologicalTicks = base.Pawn.ageTracker.AgeBiologicalTicks;
			pawn.ageTracker.AgeChronologicalTicks = base.Pawn.ageTracker.AgeChronologicalTicks;
			GenSpawn.Spawn(pawn, base.Pawn.Position, base.Pawn.MapHeld);
			pawn.GetComp<CompMilianHairSwitch>().num = comp2.num;
			pawn.GetComp<CompMilianHairSwitch>().ChangeGraphic(comp2.num);
			BodyPartRecord part = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def.defName == "Milian_Brain");
			Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffAddon);
			if (firstHediffOfDef == null)
			{
				firstHediffOfDef = pawn.health.AddHediff(Props.hediffAddon, part);
				firstHediffOfDef.Severity = 0.1f;
			}
			else
			{
				firstHediffOfDef.Severity = 0.1f;
			}
			List<Pawn> list = new List<Pawn>();
			Lord lord = base.Pawn.GetLord();
			if (lord == null)
			{
				lord = CreateNewLord(base.Pawn, aggressive: true, 15f, Props.lordJob);
			}
			if (lord == null)
			{
			}
			lord.AddPawn(pawn);
			FleckMaker.Static(base.Pawn.Position, base.Pawn.Map, Props.fleck);
			Props.promotionSound.PlayOneShot(new TargetInfo(base.Pawn.Position, base.Pawn.Map));
			base.Pawn.DeSpawn();
		}
	}

	public static Lord CreateNewLord(Thing byThing, bool aggressive, float defendRadius, Type lordJobType)
	{
		if (!CellFinder.TryFindRandomCellNear(byThing.Position, byThing.Map, 5, (IntVec3 c) => c.Standable(byThing.Map) && byThing.Map.reachability.CanReach(c, byThing, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors)), out var result))
		{
			Log.Error("Found no place for mechanoids to defend " + byThing);
			result = IntVec3.Invalid;
		}
		return LordMaker.MakeNewLord(byThing.Faction, Activator.CreateInstance(lordJobType, new SpawnedPawnParams
		{
			aggressive = aggressive,
			defendRadius = defendRadius,
			defSpot = result,
			spawnerThing = byThing
		}) as LordJob, byThing.Map);
	}
}
