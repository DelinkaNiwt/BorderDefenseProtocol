using System.Collections.Generic;
using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalk.Data;

public class Hediff_Persona : Hediff
{
	private const string RimtalkHediff = "RimTalk_PersonaData";

	private Dictionary<string, int> _spokenThoughtTicks = new Dictionary<string, int>();

	public string Personality;

	public float TalkInitiationWeight = 1f;

	public override bool Visible => false;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref Personality, "Personality");
		Scribe_Values.Look(ref TalkInitiationWeight, "TalkInitiationWeight", 1f);
		Scribe_Collections.Look(ref _spokenThoughtTicks, "SpokenThoughtTicks", LookMode.Value, LookMode.Value);
		if (_spokenThoughtTicks == null)
		{
			_spokenThoughtTicks = new Dictionary<string, int>();
		}
	}

	public static Hediff_Persona GetOrAddNew(Pawn pawn)
	{
		HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("RimTalk_PersonaData");
		if (pawn?.health?.hediffSet == null || def == null)
		{
			return null;
		}
		Hediff_Persona hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def) as Hediff_Persona;
		if (hediff == null)
		{
			hediff = (Hediff_Persona)HediffMaker.MakeHediff(def, pawn);
			PersonalityData randomPersonalityData = (pawn.RaceProps.Humanlike ? Constant.Personalities.RandomElement() : (pawn.RaceProps.Animal ? Constant.PersonaAnimal : (pawn.RaceProps.IsMechanoid ? Constant.PersonaMech : Constant.PersonaNonHuman)));
			hediff.Personality = randomPersonalityData.Persona;
			if (pawn.IsSlave || pawn.IsPrisoner || pawn.IsVisitor() || pawn.IsEnemy())
			{
				hediff.TalkInitiationWeight = 0.2f;
			}
			else
			{
				hediff.TalkInitiationWeight = randomPersonalityData.Chattiness;
			}
			pawn.health.AddHediff(hediff);
		}
		Hediff_Persona hediff_Persona = hediff;
		if (hediff_Persona._spokenThoughtTicks == null)
		{
			hediff_Persona._spokenThoughtTicks = new Dictionary<string, int>();
		}
		return hediff;
	}

	public bool TryMarkAsSpoken(Thought thought)
	{
		string key = $"{thought.def.defName}_{thought.CurStageIndex}";
		int currentTick = Find.TickManager.TicksGame;
		int randomInterval = Random.Range(60000, 150000);
		if (_spokenThoughtTicks.TryGetValue(key, out var lastTick) && currentTick - lastTick < randomInterval)
		{
			return false;
		}
		_spokenThoughtTicks[key] = currentTick;
		List<Pawn> nearbyPawns = PawnSelector.GetAllNearByPawns(thought.pawn);
		foreach (Pawn p in nearbyPawns)
		{
			if (p != thought.pawn)
			{
				Hediff_Persona hediff = GetOrAddNew(p);
				if (hediff != null)
				{
					hediff._spokenThoughtTicks[key] = currentTick;
				}
			}
		}
		return true;
	}
}
