using System.Collections.Generic;
using System.Linq;
using AlienRace.ExtendedGraphics;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

[UsedImplicitly]
internal class Comp_OutfitStandHAR : ThingComp
{
	private ThingDef race;

	private BodyTypeDef bodyType;

	private HeadTypeDef headType;

	public Gender gender;

	[Unsaved(false)]
	public Graphic_Multi bodyGraphic;

	[Unsaved(false)]
	public Graphic_Multi headGraphic;

	private bool blockRecache;

	public Building_OutfitStand OutfitStand => parent as Building_OutfitStand;

	public ThingDef Race
	{
		get
		{
			return race ?? ThingDefOf.Human;
		}
		set
		{
			if (race != value && value != null)
			{
				race = value;
				blockRecache = true;
				IEnumerable<BodyTypeDef> bodies = BodyTypesAvailable.ToList();
				if (!bodies.Contains(BodyType))
				{
					BodyType = bodies.RandomElement();
				}
				IEnumerable<HeadTypeDef> heads = HeadTypesAvailable.ToList();
				if (!heads.Contains(HeadType))
				{
					HeadType = heads.RandomElement();
				}
				gender = (race.race.hasGenders ? ((Rand.Value < ((race as ThingDef_AlienRace)?.alienRace.generalSettings.maleGenderProbability ?? 0.5f)) ? Gender.Male : Gender.Female) : Gender.None);
				blockRecache = false;
				RecacheGraphics();
			}
		}
	}

	private IEnumerable<BodyTypeDef> BodyTypesAvailable
	{
		get
		{
			if (Race is ThingDef_AlienRace alienProps)
			{
				AlienPartGenerator parts = alienProps.alienRace.generalSettings.alienPartGenerator;
				if (parts != null && !parts.bodyTypes.NullOrEmpty())
				{
					return parts.bodyTypes;
				}
			}
			return DefDatabase<BodyTypeDef>.AllDefsListForReading;
		}
	}

	private IEnumerable<HeadTypeDef> HeadTypesAvailable
	{
		get
		{
			if (Race is ThingDef_AlienRace alienProps)
			{
				AlienPartGenerator parts = alienProps.alienRace.generalSettings.alienPartGenerator;
				if (parts != null && !parts.HeadTypes.NullOrEmpty())
				{
					return parts.HeadTypes;
				}
			}
			return CachedData.DefaultHeadTypeDefs;
		}
	}

	public BodyTypeDef BodyType
	{
		get
		{
			return bodyType;
		}
		set
		{
			if (bodyType != value)
			{
				bodyType = value;
				OutfitStand.StoreSettings.filter.SetAllow(SpecialThingFilterDef.Named("AllowAdultOnlyApparel"), !IsJuvenileBodyType);
				OutfitStand.StoreSettings.filter.SetAllow(SpecialThingFilterDef.Named("AllowChildOnlyApparel"), IsJuvenileBodyType);
				RecacheGraphics();
			}
		}
	}

	public bool IsJuvenileBodyType
	{
		get
		{
			if (bodyType != BodyTypeDefOf.Baby)
			{
				return bodyType == BodyTypeDefOf.Child;
			}
			return true;
		}
	}

	public HeadTypeDef HeadType
	{
		get
		{
			return headType;
		}
		set
		{
			headType = value;
			RecacheGraphics();
		}
	}

	private void RecacheGraphics()
	{
		LongEventHandler.ExecuteWhenFinished(RecacheGraphicsStatic);
	}

	private void RecacheGraphicsStatic()
	{
		if (HeadType == null || BodyType == null || blockRecache)
		{
			return;
		}
		ThingWithComps heldWeapon = OutfitStand.HeldWeapon;
		Thing dropped;
		if (heldWeapon != null && !((IHaulDestination)OutfitStand).Accepts((Thing)heldWeapon))
		{
			OutfitStand.TryDrop(heldWeapon, parent.Position, ThingPlaceMode.Near, 1, out dropped);
		}
		if (OutfitStand.HeldItems.Any())
		{
			foreach (Thing item in OutfitStand.HeldItems.ToList())
			{
				if (item is Apparel && !((IHaulDestination)OutfitStand).Accepts(item))
				{
					OutfitStand.TryDrop(item, parent.Position, ThingPlaceMode.Near, 1, out dropped);
				}
			}
		}
		int savedIndex = parent.HashOffset();
		int shared = 0;
		AlienPartGenerator.ExtendedGraphicTop.drawOverrideDummy = new DummyExtendedGraphicsPawnWrapper
		{
			race = Race,
			bodyType = BodyType,
			headType = HeadType,
			gender = ((HeadType.gender != Gender.None) ? HeadType.gender : gender)
		};
		ThingDef_AlienRace alienRace = Race as ThingDef_AlienRace;
		string bodyPath = alienRace?.alienRace.graphicPaths.body.GetPath(null, ref shared, savedIndex);
		bodyGraphic = CachedData.getInnerGraphic(new GraphicRequest(typeof(Graphic_Multi), bodyPath, ShaderDatabase.Cutout, CachedData.outfitStandDrawSizeBody() * (alienRace?.alienRace.generalSettings.alienPartGenerator.customDrawSize ?? Vector2.one), Color.white, Color.white, null, 0, null, string.Empty));
		string headPath = alienRace?.alienRace.graphicPaths.head.GetPath(null, ref shared, savedIndex);
		headGraphic = CachedData.getInnerGraphic(new GraphicRequest(typeof(Graphic_Multi), headPath, ShaderDatabase.Cutout, CachedData.outfitStandDrawSizeHead() * (alienRace?.alienRace.generalSettings.alienPartGenerator.customHeadDrawSize ?? Vector2.one), Color.white, Color.white, null, 0, null, string.Empty));
		CachedData.outfitStandRecacheGraphics(OutfitStand);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			Race = ((parent.Faction != null) ? (parent.Faction.def.basicMemberKind.race ?? ThingDefOf.Human) : ThingDefOf.Human);
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (parent.Faction == null || parent.Faction != Faction.OfPlayer)
		{
			yield break;
		}
		yield return new Command_Action
		{
			defaultLabel = Race.LabelCap,
			defaultDesc = "HAR.OutfitStandRaceCommandDesc".Translate(),
			icon = Race.uiIcon,
			action = delegate
			{
				Find.WindowStack.Add(new FloatMenu(HarmonyPatches.colonistRaces.Select((ThingDef td) => new FloatMenuOption(td.LabelCap, delegate
				{
					Race = td;
				})).ToList()));
			}
		};
		yield return new Command_Action
		{
			defaultLabel = BodyType.defName,
			defaultDesc = "HAR.OutfitStandBodyCommandDesc".Translate(),
			icon = BodyType.Icon,
			action = delegate
			{
				Find.WindowStack.Add(new FloatMenu(BodyTypesAvailable.Select((BodyTypeDef bd) => new FloatMenuOption(bd.defName, delegate
				{
					BodyType = bd;
				}, bd.Icon, parent.Stuff?.stuffProps.color ?? Color.white)).ToList()));
			}
		};
		yield return new Command_Action
		{
			defaultLabel = HeadType.defName,
			defaultDesc = "HAR.OutfitStandHeadCommandDesc".Translate(),
			icon = HeadType.Icon,
			action = delegate
			{
				Find.WindowStack.Add(new FloatMenu(HeadTypesAvailable.Select((HeadTypeDef hdt) => new FloatMenuOption(hdt.defName, delegate
				{
					HeadType = hdt;
				}, hdt.Icon, parent.Stuff?.stuffProps.color ?? Color.white)).ToList()));
			}
		};
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Defs.Look(ref race, "Race");
		Scribe_Defs.Look(ref bodyType, "BodyType");
		Scribe_Defs.Look(ref headType, "HeadType");
		Scribe_Values.Look(ref gender, "gender", Gender.None);
		if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
		{
			if (Race == null)
			{
				ThingDef thingDef = (Race = ThingDefOf.Human);
			}
			if (BodyType == null)
			{
				BodyTypeDef bodyTypeDef = (BodyType = BodyTypesAvailable.RandomElement());
			}
			if (HeadType == null)
			{
				HeadTypeDef headTypeDef = (HeadType = HeadTypesAvailable.RandomElement());
			}
			RecacheGraphics();
		}
	}
}
