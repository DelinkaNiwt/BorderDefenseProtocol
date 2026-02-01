using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using VEF.Utils;
using Verse;

namespace VanillaPsycastsExpanded.UI;

[StaticConstructorOnStartup]
public class ITab_Pawn_Psycasts : ITab
{
	private readonly Dictionary<AbilityDef, Vector2> abilityPos = new Dictionary<AbilityDef, Vector2>();

	private readonly List<MeditationFocusDef> foci;

	private readonly Dictionary<string, List<PsycasterPathDef>> pathsByTab;

	private readonly List<TabRecord> tabs;

	private CompAbilities compAbilities;

	private string curTab;

	private bool devMode;

	private Hediff_PsycastAbilities hediff;

	private float lastPathsHeight;

	private int pathsPerRow;

	private Vector2 pathsScrollPos;

	private Pawn pawn;

	private Vector2 psysetsScrollPos;

	private bool smallMode;

	private bool useAltBackgrounds;

	public Vector2 Size => size;

	public float RequestedPsysetsHeight { get; private set; }

	public override bool IsVisible
	{
		get
		{
			if (Find.Selector.SingleSelectedThing is Pawn pawn && pawn.health.hediffSet.HasHediff(VPE_DefOf.VPE_PsycastAbilityImplant))
			{
				return pawn.Faction?.IsPlayer ?? false;
			}
			return false;
		}
	}

	static ITab_Pawn_Psycasts()
	{
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			RaceProperties race = allDef.race;
			if (race != null && race.Humanlike)
			{
				allDef.inspectorTabs?.Add(typeof(ITab_Pawn_Psycasts));
				allDef.inspectorTabsResolved?.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Psycasts)));
			}
		}
	}

	public ITab_Pawn_Psycasts()
	{
		labelKey = "VPE.Psycasts";
		size = new Vector2(Verse.UI.screenWidth, (float)Verse.UI.screenHeight * 0.75f);
		pathsByTab = (from def in DefDatabase<PsycasterPathDef>.AllDefs
			group def by def.tab).ToDictionary((IGrouping<string, PsycasterPathDef> group) => group.Key, (IGrouping<string, PsycasterPathDef> group) => group.ToList());
		foci = (from def in DefDatabase<MeditationFocusDef>.AllDefs
			orderby def.modContentPack.IsOfficialMod descending, def.label descending
			select def).ToList();
		tabs = pathsByTab.Select((KeyValuePair<string, List<PsycasterPathDef>> kv) => new TabRecord(kv.Key, delegate
		{
			curTab = kv.Key;
		}, () => curTab == kv.Key)).ToList();
		curTab = pathsByTab.Keys.FirstOrDefault();
	}

	protected override void UpdateSize()
	{
		base.UpdateSize();
		size.y = PaneTopY - 30f;
		pathsPerRow = Mathf.FloorToInt(size.x * 0.67f / 200f);
		smallMode = PsycastsMod.Settings.smallMode switch
		{
			MultiCheckboxState.On => true, 
			MultiCheckboxState.Off => false, 
			_ => size.y <= 1080f / Prefs.UIScale, 
		};
	}

	public override void OnOpen()
	{
		base.OnOpen();
		pawn = (Pawn)Find.Selector.SingleSelectedThing;
		InitCache();
	}

	private void InitCache()
	{
		PsycastsUIUtility.Hediff = (hediff = pawn.Psycasts());
		PsycastsUIUtility.CompAbilities = (compAbilities = ((ThingWithComps)pawn).GetComp<CompAbilities>());
		abilityPos.Clear();
	}

	protected override void CloseTab()
	{
		base.CloseTab();
		pawn = null;
		PsycastsUIUtility.Hediff = (hediff = null);
		PsycastsUIUtility.CompAbilities = (compAbilities = null);
		abilityPos.Clear();
	}

	protected override void FillTab()
	{
		if (Find.Selector.SingleSelectedThing is Pawn pawn && this.pawn != pawn)
		{
			this.pawn = pawn;
			InitCache();
		}
		if (devMode && !Prefs.DevMode)
		{
			devMode = false;
		}
		if (this.pawn == null || hediff == null || compAbilities == null)
		{
			return;
		}
		GameFont font = Text.Font;
		TextAnchor anchor = Text.Anchor;
		Rect rect = new Rect(Vector2.one * 20f, base.size - Vector2.one * 40f);
		Rect rect2 = UIUtility.TakeLeftPart(ref rect, base.size.x * 0.3f);
		Rect rect3 = rect.ContractedBy(5f);
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(rect2);
		Text.Font = GameFont.Medium;
		listing_Standard.Label(this.pawn.Name.ToStringFull);
		listing_Standard.Label("VPE.PsyLevel".Translate(((Hediff_Level)(object)hediff).level));
		listing_Standard.Gap(10f);
		if (((Hediff_Level)(object)hediff).level < PsycastsMod.Settings.maxLevel)
		{
			Rect rect4 = listing_Standard.GetRect(60f).ContractedBy(10f, 0f);
			Text.Anchor = TextAnchor.MiddleCenter;
			int num = Hediff_PsycastAbilities.ExperienceRequiredForLevel(((Hediff_Level)(object)hediff).level + 1);
			if (devMode)
			{
				Text.Font = GameFont.Small;
				if (Widgets.ButtonText(UIUtility.TakeRightPart(ref rect4, 80f), "Dev: Level up"))
				{
					hediff.GainExperience(num, sendLetter: false);
				}
				Text.Font = GameFont.Medium;
			}
			Widgets.FillableBar(rect4, hediff.experience / (float)num);
			Widgets.Label(rect4, $"{hediff.experience.ToStringByStyle(ToStringStyle.FloatOne)} / {num}");
			Text.Font = GameFont.Tiny;
			listing_Standard.Label("VPE.EarnXP".Translate());
			listing_Standard.Gap(10f);
		}
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		listing_Standard.Label("VPE.Points".Translate(hediff.points));
		Text.Font = GameFont.Tiny;
		listing_Standard.Label("VPE.SpendPoints".Translate());
		listing_Standard.Gap(3f);
		Text.Anchor = TextAnchor.MiddleLeft;
		Text.Font = GameFont.Small;
		float curHeight = listing_Standard.CurHeight;
		if (listing_Standard.ButtonTextLabeled("VPE.PsycasterStats".Translate() + (smallMode ? string.Format(" ({0})", "VPE.Hover".Translate()) : ""), "VPE.Upgrade".Translate()))
		{
			int num2 = GenUI.CurrentAdjustmentMultiplier();
			if (devMode)
			{
				hediff.ImproveStats(num2);
			}
			else if (hediff.points >= num2)
			{
				hediff.SpentPoints(num2);
				hediff.ImproveStats(num2);
			}
			else
			{
				Messages.Message("VPE.NotEnoughPoints".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
		}
		float curHeight2 = listing_Standard.CurHeight;
		if (smallMode)
		{
			if (Mouse.IsOver(new Rect(rect2.x, curHeight, rect2.width / 2f, curHeight2 - curHeight)))
			{
				Vector2 size = new Vector2(rect2.width, 150f);
				Find.WindowStack.ImmediateWindow(9040170, new Rect(GenUI.GetMouseAttachedWindowPos(size.x, size.y), size), WindowLayer.Super, delegate
				{
					Listing_Standard listing_Standard2 = new Listing_Standard();
					listing_Standard2.Begin(new Rect(Vector2.one * 5f, size));
					listing_Standard2.StatDisplay(TexPsycasts.IconNeuralHeatLimit, StatDefOf.PsychicEntropyMax, this.pawn);
					listing_Standard2.StatDisplay(TexPsycasts.IconNeuralHeatRegenRate, StatDefOf.PsychicEntropyRecoveryRate, this.pawn);
					listing_Standard2.StatDisplay(TexPsycasts.IconPsychicSensitivity, StatDefOf.PsychicSensitivity, this.pawn);
					if (PsycastsMod.Settings.changeFocusGain)
					{
						listing_Standard2.StatDisplay(TexPsycasts.IconPsyfocusGain, StatDefOf.MeditationFocusGain, this.pawn);
					}
					listing_Standard2.StatDisplay(TexPsycasts.IconPsyfocusCost, VPE_DefOf.VPE_PsyfocusCostFactor, this.pawn);
					listing_Standard2.End();
				});
			}
		}
		else
		{
			listing_Standard.StatDisplay(TexPsycasts.IconNeuralHeatLimit, StatDefOf.PsychicEntropyMax, this.pawn);
			listing_Standard.StatDisplay(TexPsycasts.IconNeuralHeatRegenRate, StatDefOf.PsychicEntropyRecoveryRate, this.pawn);
			listing_Standard.StatDisplay(TexPsycasts.IconPsychicSensitivity, StatDefOf.PsychicSensitivity, this.pawn);
			if (PsycastsMod.Settings.changeFocusGain)
			{
				listing_Standard.StatDisplay(TexPsycasts.IconPsyfocusGain, StatDefOf.MeditationFocusGain, this.pawn);
			}
			listing_Standard.StatDisplay(TexPsycasts.IconPsyfocusCost, VPE_DefOf.VPE_PsyfocusCostFactor, this.pawn);
		}
		listing_Standard.LabelWithIcon(TexPsycasts.IconFocusTypes, "VPE.FocusTypes".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
		Rect rect5 = listing_Standard.GetRect(48f);
		float num3 = rect2.x;
		foreach (MeditationFocusDef focus in foci)
		{
			if (num3 + 50f >= rect2.width)
			{
				num3 = rect2.x;
				listing_Standard.Gap(3f);
				rect5 = listing_Standard.GetRect(48f);
			}
			Rect inRect = new Rect(num3, rect5.y, 48f, 48f);
			DoFocus(inRect, focus);
			num3 += 50f;
		}
		listing_Standard.Gap(10f);
		if (smallMode)
		{
			if (listing_Standard.ButtonTextLabeled("VPE.PsysetCustomize".Translate(), "VPE.Edit".Translate()))
			{
				Find.WindowStack.Add(new Dialog_EditPsysets(this));
			}
		}
		else
		{
			listing_Standard.Label("VPE.PsysetCustomize".Translate());
		}
		Text.Font = GameFont.Tiny;
		listing_Standard.Label("VPE.PsysetDesc".Translate());
		if (!smallMode)
		{
			float num4 = rect2.height - listing_Standard.CurHeight;
			num4 -= 30f;
			if (Prefs.DevMode)
			{
				num4 -= 30f;
			}
			Rect rect6 = listing_Standard.GetRect(num4);
			Widgets.DrawMenuSection(rect6);
			Rect rect7 = new Rect(0f, 0f, rect6.width - 20f, RequestedPsysetsHeight);
			Widgets.BeginScrollView(rect6.ContractedBy(3f, 6f), ref psysetsScrollPos, rect7);
			DoPsysets(rect7);
			Widgets.EndScrollView();
		}
		listing_Standard.CheckboxLabeled("VPE.UseAltBackground".Translate(), ref useAltBackgrounds);
		if (Prefs.DevMode)
		{
			listing_Standard.CheckboxLabeled("VPE.DevMode".Translate(), ref devMode);
		}
		listing_Standard.End();
		if (pathsByTab.NullOrEmpty())
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Medium;
			Widgets.DrawMenuSection(rect3);
			Widgets.Label(rect3, "No Paths");
		}
		else
		{
			TabDrawer.DrawTabs(new Rect(rect3.x, rect3.y + 40f, rect3.width, rect3.height), tabs);
			rect3.yMin += 40f;
			Widgets.DrawMenuSection(rect3);
			Rect rect7 = new Rect(0f, 0f, rect3.width - 20f, lastPathsHeight);
			Widgets.BeginScrollView(rect3.ContractedBy(2f), ref pathsScrollPos, rect7);
			DoPaths(rect7);
			Widgets.EndScrollView();
		}
		Text.Font = font;
		Text.Anchor = anchor;
	}

	private void DoFocus(Rect inRect, MeditationFocusDef def)
	{
		Widgets.DrawBox(inRect, 3, Texture2D.grayTexture);
		bool flag = def.CanPawnUse(pawn);
		string reason;
		bool flag2 = def.CanUnlock(pawn, out reason);
		GUI.color = (flag ? Color.white : Color.gray);
		GUI.DrawTexture(inRect.ContractedBy(5f), (Texture)def.Icon());
		GUI.color = Color.white;
		TooltipHandler.TipRegion(inRect, def.LabelCap + (def.description.NullOrEmpty() ? "" : "\n\n") + def.description + (flag2 ? "" : ("\n\n" + reason)));
		Widgets.DrawHighlightIfMouseover(inRect);
		if ((hediff.points >= 1 || devMode) && !flag && (flag2 || devMode) && Widgets.ButtonText(new Rect(inRect.xMax - 13f, inRect.yMax - 13f, 12f, 12f), "▲"))
		{
			if (!devMode)
			{
				hediff.SpentPoints();
			}
			hediff.UnlockMeditationFocus(def);
		}
	}

	public void DoPsysets(Rect inRect)
	{
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(inRect);
		foreach (PsySet item in hediff.psysets.ToList())
		{
			Rect rect = listing_Standard.GetRect(30f);
			Widgets.Label(rect.LeftHalf().LeftHalf(), item.Name);
			if (Widgets.ButtonText(rect.LeftHalf().RightHalf(), "VPE.Rename".Translate()))
			{
				Find.WindowStack.Add(new Dialog_RenamePsyset(item));
			}
			if (Widgets.ButtonText(rect.RightHalf().LeftHalf(), "VPE.Edit".Translate()))
			{
				Find.WindowStack.Add(new Dialog_Psyset(item, pawn));
			}
			if (Widgets.ButtonText(rect.RightHalf().RightHalf(), "VPE.Remove".Translate()))
			{
				hediff.RemovePsySet(item);
			}
		}
		if (Widgets.ButtonText(listing_Standard.GetRect(70f).LeftHalf().ContractedBy(5f), "VPE.CreatePsyset".Translate()))
		{
			PsySet psySet = new PsySet
			{
				Name = "VPE.Untitled".Translate()
			};
			hediff.psysets.Add(psySet);
			Find.WindowStack.Add(new Dialog_Psyset(psySet, pawn));
		}
		RequestedPsysetsHeight = listing_Standard.CurHeight + 70f;
		listing_Standard.End();
	}

	private void DoPaths(Rect inRect)
	{
		Vector2 position = inRect.position + Vector2.one * 10f;
		float num = (inRect.width - (float)(pathsPerRow + 1) * 10f) / (float)pathsPerRow;
		float num2 = 0f;
		int num3 = pathsPerRow;
		foreach (PsycasterPathDef def in pathsByTab[curTab].OrderByDescending(hediff.unlockedPaths.Contains).ThenBy((PsycasterPathDef path) => path.order).ThenBy((PsycasterPathDef path) => path.label))
		{
			Texture2D texture2D = (useAltBackgrounds ? def.backgroundImage : def.altBackgroundImage);
			float num4 = num / (float)texture2D.width * (float)texture2D.height + 30f;
			Rect rect = new Rect(position, new Vector2(num, num4));
			PsycastsUIUtility.DrawPathBackground(ref rect, def, useAltBackgrounds);
			if (hediff.unlockedPaths.Contains(def))
			{
				if (def.HasAbilities)
				{
					PsycastsUIUtility.DoPathAbilities(rect, def, abilityPos, DoAbility);
				}
			}
			else
			{
				Widgets.DrawRectFast(rect, new Color(0f, 0f, 0f, useAltBackgrounds ? 0.7f : 0.55f));
				if (hediff.points >= 1 || devMode)
				{
					Rect rect2 = rect.CenterRect(new Vector2(140f, 30f));
					if (devMode || def.CanPawnUnlock(pawn))
					{
						if (Widgets.ButtonText(rect2, "VPE.Unlock".Translate()))
						{
							if (!devMode)
							{
								hediff.SpentPoints();
							}
							hediff.UnlockPath(def);
						}
					}
					else
					{
						GUI.color = Color.grey;
						string text = "VPE.Locked".Translate().Resolve() + ": " + def.lockedReason;
						rect2.width = Mathf.Max(rect2.width, Text.CalcSize(text).x + 10f);
						Widgets.ButtonText(rect2, text, drawBackground: true, doMouseoverSound: true, active: false);
						GUI.color = Color.white;
					}
				}
				TooltipHandler.TipRegion(rect, () => def.tooltip + "\n\n" + "VPE.AbilitiesList".Translate() + "\n" + def.abilities.Select((AbilityDef ab) => ((Def)(object)ab).label).ToLineList("  ", capitalizeItems: true), def.GetHashCode());
			}
			num2 = Mathf.Max(num2, num4 + 10f);
			position.x += num + 10f;
			num3--;
			if (num3 == 0)
			{
				position.x = inRect.x + 10f;
				position.y += num2;
				num3 = pathsPerRow;
				num2 = 0f;
			}
		}
		lastPathsHeight = position.y + num2;
	}

	private void DoAbility(Rect inRect, AbilityDef ability)
	{
		bool unlockable = false;
		bool flag = false;
		if (!compAbilities.HasAbility(ability))
		{
			if (devMode || (ability.Psycast().PrereqsCompleted(compAbilities) && hediff.points >= 1))
			{
				unlockable = true;
			}
			else
			{
				flag = true;
			}
		}
		if (unlockable)
		{
			Widgets.DrawStrongHighlight(inRect.ExpandedBy(12f));
		}
		PsycastsUIUtility.DrawAbility(inRect, ability);
		if (flag)
		{
			Widgets.DrawRectFast(inRect, new Color(0f, 0f, 0f, 0.6f));
		}
		TooltipHandler.TipRegion(inRect, () => string.Format("{0}\n\n{1}{2}", ((Def)(object)ability).LabelCap, ((Def)(object)ability).description, unlockable ? ("\n\n" + "VPE.ClickToUnlock".Translate().Resolve().ToUpper()) : ""), ((object)ability).GetHashCode());
		if (unlockable && Widgets.ButtonInvisible(inRect))
		{
			if (!devMode)
			{
				hediff.SpentPoints();
			}
			compAbilities.GiveAbility(ability);
		}
	}
}
