using RimWorld;
using Verse;

namespace AlienRace;

[DefOf]
public static class AlienDefOf
{
	public static TraitDef HAR_Xenophobia;

	public static ThoughtDef HAR_XenophobiaVsAlien;

	public static ThingCategoryDef HAR_AlienCorpseCategory;

	[MayRequireIdeology]
	public static HistoryEventDef HAR_AteAlienMeat;

	[MayRequireIdeology]
	public static HistoryEventDef HAR_AteNonAlienFood;

	[MayRequireIdeology]
	public static HistoryEventDef HAR_ButcheredAlien;

	[MayRequireIdeology]
	public static HistoryEventDef HAR_AlienDating_Dating;

	[MayRequireIdeology]
	public static HistoryEventDef HAR_AlienDating_BeginRomance;

	[MayRequireIdeology]
	public static HistoryEventDef HAR_AlienDating_SharedBed;

	[MayRequireIdeology]
	public static HistoryEventDef HAR_Alien_SoldSlave;

	public static TraitDef Cannibal;

	public static ThoughtDef AteHumanlikeMeatDirect;

	public static ThoughtDef AteHumanlikeMeatAsIngredient;

	public static ThoughtDef AteHumanlikeMeatDirectCannibal;

	public static ThoughtDef AteHumanlikeMeatAsIngredientCannibal;

	public static ThoughtDef ButcheredHumanlikeCorpse;

	public static ThoughtDef KnowButcheredHumanlikeCorpse;

	public static NeedDef Mood;
}
