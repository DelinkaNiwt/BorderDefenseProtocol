using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CharacterEditor;

public class CharacterEditorGrave : Building_CryptosleepCasket
{
	private Pawn innerPawn;

	private Graphic cachedGraphicFull;

	public override Graphic Graphic
	{
		get
		{
			if (innerPawn == null && innerContainer.Count == 0)
			{
				return base.Graphic;
			}
			cachedGraphicFull = def.building.fullGraveGraphicData.GraphicColoredFor(this);
			if (def.building.fullGraveGraphicData == null)
			{
				return base.Graphic;
			}
			if (cachedGraphicFull == null)
			{
				cachedGraphicFull = def.building.fullGraveGraphicData.GraphicColoredFor(this);
			}
			return cachedGraphicFull;
		}
	}

	public CharacterEditorGrave()
	{
		innerPawn = null;
		cachedGraphicFull = null;
	}

	public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
	{
		if (innerContainer.Count != 0)
		{
			yield break;
		}
		if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
		{
			yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
			yield break;
		}
		string jobStr = CharacterEditor.Label.ENTER_ZOMBGRELLA;
		Action jobAction = delegate
		{
			JobDef jobDef = DefTool.JobDef("EnterZGrave");
			if (jobDef != null)
			{
				Job job = new Job(jobDef, this);
				myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			}
		};
		yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(jobStr, jobAction), myPawn, this);
	}

	public override void EjectContents()
	{
		ThingDef filth_Slime = ThingDefOf.Filth_Slime;
		foreach (Thing item in (IEnumerable<Thing>)innerContainer)
		{
			if (item is Pawn pawn)
			{
				PawnComponentsUtility.AddComponentsForSpawn(pawn);
				pawn.filth.GainFilth(filth_Slime);
				innerPawn = null;
			}
		}
		if (!base.Destroyed)
		{
			DefTool.SoundDef("Bridge_CollapseWater")?.PlayOneShot(SoundInfo.InMap(new TargetInfo(base.Position, base.Map)));
		}
		innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
		contentsKnown = true;
	}

	public bool TryToEnter(Thing thing)
	{
		if (!Accepts(thing))
		{
			return false;
		}
		bool flag;
		if (thing.holdingOwner != null)
		{
			thing.holdingOwner.TryTransferToContainer(thing, innerContainer, thing.stackCount);
			flag = true;
		}
		else
		{
			flag = innerContainer.TryAdd(thing);
		}
		if (flag)
		{
			if (thing.Faction != null && thing.Faction.IsPlayer)
			{
				contentsKnown = true;
			}
			return true;
		}
		return false;
	}

	public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
	{
		if (TryToEnter(thing))
		{
			SoundDefOf.Corpse_Drop.PlayOneShot(new TargetInfo(base.Position, base.Map));
			cachedGraphicFull = def.building.fullGraveGraphicData.GraphicColoredFor(this);
			innerPawn = thing as Pawn;
			CEditor.API.Get<Dictionary<int, Building_CryptosleepCasket>>(EType.UIContainers)[0] = this;
			CEditor.API.StartEditor(innerPawn);
			return true;
		}
		return false;
	}
}
