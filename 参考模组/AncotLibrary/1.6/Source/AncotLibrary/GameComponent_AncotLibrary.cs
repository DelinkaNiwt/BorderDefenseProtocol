using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class GameComponent_AncotLibrary : GameComponent
{
	public List<Thing> specialWeaponCached = new List<Thing>();

	public List<Thing> starWeaponCached = new List<Thing>();

	public int MaxTraitSlots = 0;

	private bool vanillaExpandedFramework_Active = false;

	public Dictionary<ThingDef, ApparelPolicy> raceApparelPolicy = new Dictionary<ThingDef, ApparelPolicy>();

	public Dictionary<Faction, List<SitePartDef>> allowedSite = new Dictionary<Faction, List<SitePartDef>>();

	private List<ThingDef> raceApparelPolicyKeys;

	private List<ApparelPolicy> raceApparelPolicyValues;

	private List<Faction> allowedSiteKeys;

	private List<List<SitePartDef>> allowedSiteValues;

	public static GameComponent_AncotLibrary GC => Current.Game.GetComponent<GameComponent_AncotLibrary>();

	public bool VanillaExpandedFramework_Active => vanillaExpandedFramework_Active;

	public List<Thing> AllWeapons => SpecialWeapon.Union(StarWeapon).ToList();

	public List<Thing> SpecialWeapon
	{
		get
		{
			return specialWeaponCached;
		}
		set
		{
			if (specialWeaponCached != value)
			{
				RefreshCached(value);
				specialWeaponCached = value;
			}
		}
	}

	public List<Thing> StarWeapon
	{
		get
		{
			return starWeaponCached;
		}
		set
		{
			if (starWeaponCached != value)
			{
				RefreshStarCached(value);
				starWeaponCached = value;
			}
		}
	}

	public GameComponent_AncotLibrary(Game game)
	{
	}

	public override void StartedNewGame()
	{
		base.StartedNewGame();
		NotifyModActive_VanillaExpandedFramework();
		GenerateApparelPolicy();
	}

	public override void LoadedGame()
	{
		base.LoadedGame();
		NotifyModActive_VanillaExpandedFramework();
		if (specialWeaponCached.NullOrEmpty())
		{
			specialWeaponCached = new List<Thing>();
		}
		if (starWeaponCached.NullOrEmpty())
		{
			starWeaponCached = new List<Thing>();
		}
		if (raceApparelPolicy.NullOrEmpty())
		{
			raceApparelPolicy = new Dictionary<ThingDef, ApparelPolicy>();
		}
	}

	public void RefreshCached()
	{
		RefreshCached(SpecialWeapon);
	}

	public void RefreshCached(List<Thing> weapons)
	{
		List<Thing> list = new List<Thing>();
		foreach (Thing weapon in weapons)
		{
			if (weapon == null || weapon.Destroyed)
			{
				continue;
			}
			CompUniqueWeapon compUniqueWeapon = weapon.TryGetComp<CompUniqueWeapon>();
			if (compUniqueWeapon != null && compUniqueWeapon.TraitsListForReading.Count != 0)
			{
				int num = ((compUniqueWeapon is CompEmptyUniqueWeapon compEmptyUniqueWeapon) ? compEmptyUniqueWeapon.Traits_Props.max_traits : 3);
				if (MaxTraitSlots < num)
				{
					MaxTraitSlots = num;
				}
				list.Add(weapon);
			}
		}
		specialWeaponCached = list;
	}

	public void RefreshStarCached()
	{
		RefreshStarCached(starWeaponCached);
	}

	public void RefreshStarCached(List<Thing> weapons)
	{
		List<Thing> list = new List<Thing>();
		foreach (Thing weapon in weapons)
		{
			if (weapon == null || weapon.Destroyed)
			{
				continue;
			}
			CompUniqueWeapon compUniqueWeapon = weapon.TryGetComp<CompUniqueWeapon>();
			if (compUniqueWeapon != null)
			{
				int num = ((compUniqueWeapon is CompEmptyUniqueWeapon compEmptyUniqueWeapon) ? compEmptyUniqueWeapon.Traits_Props.max_traits : 3);
				if (MaxTraitSlots < num)
				{
					MaxTraitSlots = num;
				}
				list.Add(weapon);
			}
		}
		starWeaponCached = list;
	}

	public void GenerateApparelPolicy()
	{
		ApparelPolicyGenerator.GenerateApparelPolicyFromDef(out var dictionary);
		raceApparelPolicy = dictionary;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref raceApparelPolicy, "raceApparelPolicy", LookMode.Def, LookMode.Reference, ref raceApparelPolicyKeys, ref raceApparelPolicyValues);
		Scribe_Collections.Look(ref specialWeaponCached, "specialWeaponCached", LookMode.Reference);
		Scribe_Collections.Look(ref starWeaponCached, "starWeaponCached", LookMode.Reference);
		Scribe_Collections.Look(ref allowedSite, "allowedSite", LookMode.Reference, LookMode.Deep, ref allowedSiteKeys, ref allowedSiteValues);
		Scribe_Values.Look(ref MaxTraitSlots, "MaxTraitSlots", 0);
	}

	public void NotifyModActive_VanillaExpandedFramework()
	{
		if (ModsConfig.IsActive("oskarpotocki.vanillafactionsexpanded.core"))
		{
			vanillaExpandedFramework_Active = true;
		}
		else
		{
			vanillaExpandedFramework_Active = false;
		}
	}
}
