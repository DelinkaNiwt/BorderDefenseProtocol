using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded;

public class Ability_ReunionFarskip : Ability
{
	private List<Mote> maintainedMotes = new List<Mote>();

	public override void PreWarmupAction()
	{
		((Ability)this).PreWarmupAction();
		Map map = base.pawn.Map;
		Mote item = SpawnMote(map, VPE_DefOf.VPE_Mote_GreenMist, base.pawn.Position.ToVector3Shifted(), 10f, 20f);
		maintainedMotes = new List<Mote>();
		maintainedMotes.Add(item);
		List<IntVec3> list = (from x in GenRadial.RadialCellsAround(base.pawn.Position, 3f, useCenter: true)
			where x.InBounds(map)
			select x).ToList();
		for (int num = 0; num < 5; num++)
		{
			if (list.Any())
			{
				IntVec3 item2 = list.RandomElement();
				list.Remove(item2);
				Mote item3 = SpawnMote(map, ThingDef.Named("VPE_Mote_Ghost" + "ABCDEFG".RandomElement()), item2.ToVector3Shifted(), 1f, 0f);
				maintainedMotes.Add(item3);
			}
		}
	}

	public override void WarmupToil(Toil toil)
	{
		((Ability)this).WarmupToil(toil);
		toil.AddPreTickAction(delegate
		{
			foreach (Mote maintainedMote in maintainedMotes)
			{
				maintainedMote.Maintain();
			}
		});
	}

	public List<Pawn> GetLivingFamilyMembers(Pawn pawn)
	{
		return pawn.relations.FamilyByBlood.Where((Pawn x) => !x.Dead).ToList();
	}

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		Pawn pawn = targets[0].Thing as Pawn;
		List<Pawn> livingFamilyMembers = GetLivingFamilyMembers(pawn);
		List<IntVec3> source = (from x in GenRadial.RadialCellsAround(base.pawn.Position, 3f, useCenter: true)
			where x.InBounds(base.pawn.Map) && x.Walkable(base.pawn.Map)
			select x).ToList();
		foreach (Pawn item in livingFamilyMembers)
		{
			GenSpawn.Spawn(item, source.RandomElement(), base.pawn.Map);
			Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, item);
			BodyPartRecord result = null;
			item.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).TryRandomElement(out result);
			item.health.AddHediff(hediff, result);
		}
		foreach (Faction item2 in (from x in livingFamilyMembers
			select x.Faction into x
			where x != null
			select x).Distinct())
		{
			item2.TryAffectGoodwillWith(base.pawn.Faction, -10);
		}
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (target.Thing is Pawn { relations: not null } pawn && !GetLivingFamilyMembers(pawn).Any())
		{
			if (showMessages)
			{
				Messages.Message("VPE.MustHaveLivingFamilyMembers".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return ((Ability)this).ValidateTarget(target, showMessages);
	}

	public Mote SpawnMote(Map map, ThingDef moteDef, Vector3 loc, float scale, float rotationRate)
	{
		Mote mote = MoteMaker.MakeStaticMote(loc, map, moteDef, scale);
		mote.rotationRate = rotationRate;
		if (mote.def.mote.needsMaintenance)
		{
			mote.Maintain();
		}
		return mote;
	}

	public override void ExposeData()
	{
		((Ability)this).ExposeData();
		Scribe_Collections.Look(ref maintainedMotes, "maintainedMotes", LookMode.Reference);
	}
}
