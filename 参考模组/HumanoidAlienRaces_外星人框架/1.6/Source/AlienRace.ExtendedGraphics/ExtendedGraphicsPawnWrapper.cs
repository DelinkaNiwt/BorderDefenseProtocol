using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace AlienRace.ExtendedGraphics;

public class ExtendedGraphicsPawnWrapper
{
	public Pawn WrappedPawn { get; private set; }

	public virtual PawnDrawParms DrawParms => CachedData.oldDrawParms(WrappedPawn.Drawer.renderer.renderTree);

	public virtual IEnumerable<Apparel> GetWornApparel
	{
		get
		{
			IEnumerable<Apparel> enumerable = WrappedPawn.apparel?.WornApparel;
			return enumerable ?? Enumerable.Empty<Apparel>();
		}
	}

	public virtual bool Drafted => WrappedPawn.Drafted;

	public virtual Job CurJob => WrappedPawn.CurJob;

	public virtual bool Moving => WrappedPawn.pather.MovingNow;

	public virtual bool IsStatue => AlienRenderTreePatches.IsStatuePawn(WrappedPawn);

	public ExtendedGraphicsPawnWrapper(Pawn pawn)
	{
		WrappedPawn = pawn;
	}

	public ExtendedGraphicsPawnWrapper()
	{
	}

	public virtual bool HasApparelGraphics()
	{
		return GetWornApparel.Any((Apparel ap) => ap.def.apparel.HasDefinedGraphicProperties);
	}

	public virtual bool HasBackStory(BackstoryDef backstory)
	{
		return GetBackstories().Contains(backstory);
	}

	private bool IsHediffOfDefAndPart(Hediff hediff, HediffDef hediffDef, BodyPartDef part, string partLabel)
	{
		if (hediff.def == hediffDef)
		{
			if (hediff.Part != null)
			{
				return IsBodyPart(hediff.Part, part, partLabel);
			}
			return true;
		}
		return false;
	}

	public virtual IEnumerable<float> SeverityOfHediffsOnPart(HediffDef hediffDef, BodyPartDef part, string partLabel)
	{
		return from h in GetHediffList()
			where IsHediffOfDefAndPart(h, hediffDef, part, partLabel)
			select h.Severity;
	}

	public virtual Hediff HasHediffOfDefAndPart(HediffDef hediffDef, BodyPartDef part, string partLabel)
	{
		return GetHediffList().FirstOrDefault((Hediff h) => IsHediffOfDefAndPart(h, hediffDef, part, partLabel));
	}

	public virtual bool CurrentLifeStageDefMatches(LifeStageDef lifeStageDef)
	{
		return WrappedPawn.ageTracker?.CurLifeStage?.Equals(lifeStageDef) == true;
	}

	public virtual bool IsPartBelowHealthThreshold(BodyPartDef part, string partLabel, float healthThreshold)
	{
		return (from hediff in GetHediffList()
			select hediff.Part into hediffPart
			where hediffPart != null
			where IsBodyPart(hediffPart, part, partLabel)
			select hediffPart).Any((BodyPartRecord p) => healthThreshold >= GetHediffSet().GetPartHealth(p));
	}

	public virtual bool HasTraitWithIdentifier(string traitId)
	{
		string capitalisedTrait = traitId.CapitalizeFirst();
		return (from t in GetTraitList()
			select t.CurrentData).Any((TraitDegreeData t) => capitalisedTrait == t.LabelCap || capitalisedTrait == t.GetLabelCapFor(WrappedPawn) || capitalisedTrait == t.untranslatedLabel.CapitalizeFirst());
	}

	public virtual IEnumerable<ApparelProperties> GetWornApparelProps()
	{
		return GetWornApparel?.Select((Apparel ap) => ap.def.apparel) ?? Array.Empty<ApparelProperties>();
	}

	public virtual bool VisibleInBed(bool noBed = true)
	{
		return WrappedPawn.CurrentBed()?.def?.building?.bed_showSleeperBody ?? noBed;
	}

	public virtual bool HasNamedBodyPart(BodyPartDef part, string partLabel)
	{
		if (part != null || !partLabel.NullOrEmpty())
		{
			return GetBodyPart(part, partLabel) != null;
		}
		return true;
	}

	public virtual BodyPartRecord GetBodyPart(BodyPartDef part, string partLabel)
	{
		return GetHediffSet().GetNotMissingParts()?.FirstOrDefault((BodyPartRecord bpr) => IsBodyPart(bpr, part, partLabel));
	}

	public virtual bool IsBodyPart(BodyPartRecord bpr, BodyPartDef part, string partLabel)
	{
		if (partLabel.NullOrEmpty() || bpr.untranslatedCustomLabel == partLabel)
		{
			if (part != null)
			{
				return bpr.def == part;
			}
			return true;
		}
		return false;
	}

	public virtual bool LinkToCorePart(bool drawWithoutPart, bool alignWithHead, BodyPartDef part, string partLabel)
	{
		if (drawWithoutPart && !NamedBodyPartExists(part, partLabel))
		{
			if (alignWithHead)
			{
				return GetHediffSet().HasHead;
			}
			return true;
		}
		return false;
	}

	public virtual bool NamedBodyPartExists(BodyPartDef part, string partLabel)
	{
		if (part != null || !partLabel.NullOrEmpty())
		{
			return GetAnyBodyPart(part, partLabel) != null;
		}
		return true;
	}

	public virtual BodyPartRecord GetAnyBodyPart(BodyPartDef part, string partLabel)
	{
		return WrappedPawn.RaceProps.body.AllParts.Find((BodyPartRecord bpr) => IsBodyPart(bpr, part, partLabel));
	}

	public virtual float GetNeed(NeedDef needDef, bool percentage)
	{
		Need need = WrappedPawn.needs?.TryGetNeed(needDef);
		if (need != null)
		{
			if (!percentage)
			{
				return need.CurLevel;
			}
			return need.CurLevelPercentage;
		}
		return 0f;
	}

	public virtual Gender GetGender()
	{
		return WrappedPawn.gender;
	}

	public virtual PawnPosture GetPosture()
	{
		return WrappedPawn.GetPosture();
	}

	public virtual bool HasBodyType(BodyTypeDef bodyType)
	{
		return WrappedPawn.story.bodyType == bodyType;
	}

	public virtual bool HasHeadTypeNamed(HeadTypeDef headType)
	{
		return WrappedPawn.story.headType == headType;
	}

	public virtual RotStage? GetRotStage()
	{
		return WrappedPawn.Corpse?.GetRotStage();
	}

	public virtual RotDrawMode GetRotDrawMode()
	{
		return WrappedPawn.Drawer.renderer.CurRotDrawMode;
	}

	public virtual List<Hediff> GetHediffList()
	{
		return GetHediffSet().hediffs;
	}

	public virtual List<Trait> GetTraitList()
	{
		return GetTraits().allTraits;
	}

	public virtual HediffSet GetHediffSet()
	{
		return WrappedPawn.health?.hediffSet ?? new HediffSet(WrappedPawn);
	}

	public virtual IEnumerable<BackstoryDef> GetBackstories()
	{
		IEnumerable<BackstoryDef> enumerable = WrappedPawn.story?.AllBackstories;
		return enumerable ?? Enumerable.Empty<BackstoryDef>();
	}

	public virtual TraitSet GetTraits()
	{
		return WrappedPawn.story?.traits ?? new TraitSet(WrappedPawn);
	}

	public virtual bool HasGene(GeneDef gene)
	{
		return WrappedPawn.genes.GetGene(gene)?.Active ?? false;
	}

	public virtual bool IsRace(ThingDef race)
	{
		return WrappedPawn.def == race;
	}

	public virtual bool IsMutant(MutantDef def)
	{
		if (WrappedPawn.IsMutant)
		{
			if (def != null)
			{
				return WrappedPawn.mutant.Def == def;
			}
			return true;
		}
		return false;
	}

	public virtual bool IsCreepJoiner(CreepJoinerFormKindDef def)
	{
		if (WrappedPawn.IsCreepJoiner)
		{
			if (def != null)
			{
				return WrappedPawn.creepjoiner.form == def;
			}
			return true;
		}
		return false;
	}

	public virtual bool HasStyle(StyleCategoryDef style)
	{
		return WrappedPawn.Ideo?.thingStyleCategories.Any((ThingStyleCategoryWithPriority tscwp) => tscwp.category == style && tscwp.priority > 0f) ?? false;
	}
}
