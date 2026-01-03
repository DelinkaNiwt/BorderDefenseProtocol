using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompLegendaryWeapons : CompEquippable
{
	public int Killcount = 0;

	public int TickToCheck;

	protected bool biocoded;

	protected Pawn codedPawn;

	protected string codedPawnLabel;

	public CompProperties_LegendaryWeapons Props => props as CompProperties_LegendaryWeapons;

	public List<Ability> AbilitysForReading
	{
		get
		{
			List<Ability> list = new List<Ability>();
			foreach (AbilityDef abilitieDef in Props.AbilitieDefs)
			{
				list.Add(AbilityUtility.MakeAbility(abilitieDef, base.Holder));
			}
			Log.Message("ability" + list);
			return list;
		}
	}

	public virtual bool Biocodable => true;

	public Pawn CodedPawn => codedPawn;

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		if (base.Holder == null)
		{
			return;
		}
		foreach (Ability item in AbilitysForReading)
		{
			item.pawn = base.Holder;
			item.verb.caster = base.Holder;
		}
	}

	public override void Notify_Equipped(Pawn pawn)
	{
		foreach (Ability item in AbilitysForReading)
		{
			item.pawn = pawn;
			item.verb.caster = pawn;
			pawn.abilities.GainAbility(item.def);
		}
		CodeFor(pawn);
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		foreach (Ability item in AbilitysForReading)
		{
			pawn.abilities.RemoveAbility(item.def);
		}
	}

	public void CodeFor(Pawn pawn)
	{
		if (Biocodable)
		{
			biocoded = true;
			codedPawn = pawn;
			codedPawnLabel = pawn.Name.ToStringFull;
			OnCodedFor(pawn);
		}
	}

	public override void Notify_KilledPawn(Pawn pawn)
	{
		base.Notify_KilledPawn(pawn);
		Killcount++;
		Pawn_PsychicEntropyTracker psychicEntropy = pawn.psychicEntropy;
		if (psychicEntropy != null && Props.GivePE)
		{
			psychicEntropy.OffsetPsyfocusDirectly(Mathf.Max(0.5f, 0.07f * (float)pawn.GetPsylinkLevel()));
		}
	}

	public void UnCode()
	{
		biocoded = false;
		Pawn pawn = CodedPawn;
		codedPawn = null;
		codedPawnLabel = null;
		Killcount = 0;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref Killcount, "no. of kills", 0);
	}

	protected virtual void OnCodedFor(Pawn p)
	{
	}
}
