using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AlienRace.ExtendedGraphics;

public class DummyExtendedGraphicsPawnWrapper : ExtendedGraphicsPawnWrapper
{
	public PawnDrawParms drawParms;

	public IEnumerable<Apparel> wornApparel = Array.Empty<Apparel>();

	public IEnumerable<ApparelProperties> wornApparelProps = Array.Empty<ApparelProperties>();

	public bool visibleInBed;

	public Gender gender;

	public PawnPosture posture;

	public RotStage? rotStage;

	public RotDrawMode rotDrawMode;

	public List<Hediff> hediffList = new List<Hediff>();

	public List<Trait> traitList = new List<Trait>();

	public HediffSet hediffSet;

	public IEnumerable<BackstoryDef> backstories;

	public TraitSet traits;

	public bool drafted;

	public Job curJob;

	public bool moving;

	public LifeStageDef currentLifeStage;

	public BodyTypeDef bodyType;

	public HeadTypeDef headType;

	public List<GeneDef> genes = new List<GeneDef>();

	public ThingDef race;

	public MutantDef mutant;

	public CreepJoinerFormKindDef creepJoiner;

	public BodyDef body;

	public bool isStatue;

	public List<StyleCategoryDef> styles = new List<StyleCategoryDef>();

	public override PawnDrawParms DrawParms => drawParms;

	public override IEnumerable<Apparel> GetWornApparel => wornApparel;

	public override bool Drafted => drafted;

	public override Job CurJob => curJob;

	public override bool Moving => moving;

	public override bool IsStatue => isStatue;

	public override bool CurrentLifeStageDefMatches(LifeStageDef lifeStageDef)
	{
		return lifeStageDef == currentLifeStage;
	}

	public override bool VisibleInBed(bool noBed = true)
	{
		return visibleInBed;
	}

	public override BodyPartRecord GetAnyBodyPart(BodyPartDef part, string partLabel)
	{
		return body.AllParts.Find((BodyPartRecord bpr) => IsBodyPart(bpr, part, partLabel));
	}

	public override float GetNeed(NeedDef needDef, bool percentage)
	{
		return 0f;
	}

	public override Gender GetGender()
	{
		return gender;
	}

	public override PawnPosture GetPosture()
	{
		return posture;
	}

	public override bool HasBodyType(BodyTypeDef bodyType)
	{
		return bodyType == this.bodyType;
	}

	public override bool HasHeadTypeNamed(HeadTypeDef headType)
	{
		return headType == this.headType;
	}

	public override RotStage? GetRotStage()
	{
		return rotStage;
	}

	public override RotDrawMode GetRotDrawMode()
	{
		return rotDrawMode;
	}

	public override List<Hediff> GetHediffList()
	{
		return hediffList;
	}

	public override List<Trait> GetTraitList()
	{
		return traitList;
	}

	public override HediffSet GetHediffSet()
	{
		return hediffSet;
	}

	public override IEnumerable<BackstoryDef> GetBackstories()
	{
		return backstories;
	}

	public override TraitSet GetTraits()
	{
		return traits;
	}

	public override bool HasGene(GeneDef gene)
	{
		return genes.Contains(gene);
	}

	public override bool IsRace(ThingDef race)
	{
		return race == this.race;
	}

	public override bool IsMutant(MutantDef def)
	{
		return def == mutant;
	}

	public override bool IsCreepJoiner(CreepJoinerFormKindDef def)
	{
		return def == creepJoiner;
	}

	public override bool HasStyle(StyleCategoryDef style)
	{
		return styles.Contains(style);
	}
}
