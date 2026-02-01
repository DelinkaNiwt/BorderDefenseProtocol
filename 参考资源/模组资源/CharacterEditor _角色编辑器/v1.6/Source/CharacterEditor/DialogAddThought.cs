using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogAddThought : Window, IPawnable
{
	private Vector2 scrollPos;

	private string selectedModName;

	private string selectedType;

	private string selectedStage;

	private RoyalTitleDef selectedTitle;

	private ThoughtDef selectedThought;

	private HashSet<string> lModnames;

	private HashSet<string> lListType;

	private HashSet<ThoughtDef> lListThoughts;

	private HashSet<ThoughtDef> lTMemory;

	private HashSet<ThoughtDef> lTMemorySocial;

	private HashSet<ThoughtDef> lTSituational;

	private HashSet<ThoughtDef> lTSituationalSocial;

	private HashSet<ThoughtDef> lTUnsupported;

	private HashSet<ThoughtDef> lTAll;

	private HashSet<RoyalTitleDef> lRoyalTitles;

	private Dictionary<int, string> dicStages;

	private float baseMoodOffset;

	private float selectedMoodOffset;

	private float selectedOpinionOffset;

	private bool doOnce;

	private bool bNeedOtherPawn = false;

	private bool bNeedTitle = false;

	private bool bNeedStage = false;

	private bool bAllOk = false;

	public Pawn SelectedPawn { get; set; }

	public Pawn SelectedPawn2 { get; set; }

	public Pawn SelectedPawn3 { get; set; }

	public Pawn SelectedPawn4 { get; set; }

	private Func<ThoughtDef, string> FGetTooltip => (ThoughtDef t) => GetTooltipForThought(t);

	private Func<ThoughtDef, string> FGetThoughtLabel => (ThoughtDef t) => GetLabelForThought(t);

	private Func<RoyalTitleDef, string> FGetRoyalTitleLabel => (RoyalTitleDef r) => (r == null) ? Label.NONE : r.LabelCap.ToString();

	private Func<Pawn, string> FGetPawnName => (Pawn p) => (p == null) ? Label.NONE : p.LabelShort;

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	private string GetLabelForThought(ThoughtDef t)
	{
		if (t == null)
		{
			return Label.NONE;
		}
		if (t.defName == "DeadMansApparel")
		{
			return t.defName;
		}
		return t.GetThoughtLabel(0, CEditor.API.Pawn);
	}

	private string GetTooltipForThought(ThoughtDef t)
	{
		string text = t.SDefname();
		if (t != null)
		{
			text = text + "\n\n" + t.GetThoughtDescription();
			text += "\n";
			if (!t.stages.NullOrEmpty())
			{
				for (int i = 0; i < t.stages.Count; i++)
				{
					try
					{
						int num = (int)t.stages[i].baseMoodEffect;
						int num2 = (int)t.stages[i].baseOpinionOffset;
						text = text + "\nStage " + i + " BaseMood: " + num.ToString().Colorize((num == 0) ? ColorLibrary.Grey : ((num > 0) ? ColorLibrary.Green : ColorLibrary.Red));
						text = text + " BaseOpinion: " + num2.ToString().Colorize((num2 == 0) ? ColorLibrary.Grey : ((num2 > 0) ? ColorLibrary.Green : ColorLibrary.Red));
					}
					catch
					{
					}
				}
			}
			text = text + "\n" + t.ThoughtClass.ToString();
			if (t.IsTypeOf<Thought_PsychicHarmonizer>())
			{
				text += ("\nrequires hediff: " + HediffDefOf.PsychicHarmonizer.LabelCap.ToString()).Colorize(ColorLibrary.Purple);
			}
			if (t.IsTypeOf<Thought_WeaponTrait>())
			{
				text += "\nrequires weapon".Colorize(ColorLibrary.Purple);
			}
			if (t.IsTypeOf<ThoughtWorker_WantToSleepWithSpouseOrLover>() || t.IsTypeOf<Thought_OpinionOfMyLover>())
			{
				text += "\nrequires love relation".Colorize(ColorLibrary.Purple);
			}
			if (!t.requiredTraits.NullOrEmpty())
			{
				text += ("\nrequires trait: " + t.requiredTraits.First().SDefname()).Colorize(ColorLibrary.Purple);
			}
		}
		return text;
	}

	internal DialogAddThought(string _selectedType = null)
	{
		scrollPos = default(Vector2);
		SearchTool.Update(SearchTool.SIndex.ThoughtDef);
		baseMoodOffset = 0f;
		selectedMoodOffset = 1f;
		selectedOpinionOffset = 1f;
		selectedModName = Label.ALL;
		selectedType = _selectedType ?? Label.ALL;
		SelectedPawn = null;
		selectedStage = null;
		selectedThought = null;
		selectedTitle = null;
		lModnames = DefTool.ListModnamesWithNull((ThoughtDef x) => true);
		lListType = new HashSet<string>
		{
			Label.ALL,
			Label.TH_MEMORY,
			Label.TH_SOCIAL,
			Label.TH_SITUATIONAL,
			Label.TH_SITUATIONAL + Label.TH_SOCIAL,
			Label.TH_UNSUPPORTED
		};
		lTMemory = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtMemory);
		lTMemorySocial = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtMemorySocial);
		lTSituational = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtSituational);
		lTSituationalSocial = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtSituationalSocial);
		lTUnsupported = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtUnsupported);
		lTAll = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtsAll);
		lListThoughts = lTAll;
		lRoyalTitles = DefTool.ListBy((RoyalTitleDef x) => true);
		dicStages = new Dictionary<int, string>();
		AThoughtTypeSelected(selectedType);
		doOnce = true;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	public override void DoWindowContents(Rect inRect)
	{
		int w = (int)InitialSize.x - 40;
		int y = 0;
		int x = 0;
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.ThoughtDef, ref windowRect, ref doOnce, 105);
		}
		DrawTitle(x, ref y, w, 30);
		DrawUpperDropdowns(x, ref y, w, 30);
		DrawThoughtList(x, ref y, w, 500);
		DrawLowerDropdowns(x, ref y, w, 30);
		DrawSlider(x, ref y, w, 30);
		DrawAccept(x, ref y, w, 30);
	}

	private void DrawTitle(int x, ref int y, int w, int h)
	{
		Text.Font = GameFont.Medium;
		SZWidgets.Label(new Rect(x, y, w, h), Label.ADD_THOUGHT);
		GUI.DrawTexture(new Rect(x + w - 32, y, 30f, 30f), (Texture)ContentFinder<Texture2D>.Get("bmemory"));
		y += 30;
	}

	private void DrawUpperDropdowns(int x, ref int y, int w, int h)
	{
		Text.Font = GameFont.Small;
		SZWidgets.FloatMenuOnButtonText(new Rect(x, y, w, h), selectedModName ?? Label.ALL, lModnames, (string s) => s ?? Label.ALL, AModnameSelected);
		y += 30;
		SZWidgets.FloatMenuOnButtonText(new Rect(x, y, w, h), selectedType ?? Label.ALL, lListType, (string s) => s ?? Label.ALL, AThoughtTypeSelected);
		y += 32;
	}

	private void AModnameSelected(string val)
	{
		selectedModName = val;
		if (selectedModName == null || val == Label.ALL)
		{
			lTMemory = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtMemory);
			lTMemorySocial = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtMemorySocial);
			lTSituational = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtSituational);
			lTSituationalSocial = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtSituationalSocial);
			lTUnsupported = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtUnsupported);
			lTAll = CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtsAll);
		}
		else
		{
			lTMemory = (from t in CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtMemory)
				where t.IsFromMod(selectedModName)
				select t).ToHashSet();
			lTMemorySocial = (from t in CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtMemorySocial)
				where t.IsFromMod(selectedModName)
				select t).ToHashSet();
			lTSituational = (from t in CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtSituational)
				where t.IsFromMod(selectedModName)
				select t).ToHashSet();
			lTSituationalSocial = (from t in CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtSituationalSocial)
				where t.IsFromMod(selectedModName)
				select t).ToHashSet();
			lTUnsupported = (from t in CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtUnsupported)
				where t.IsFromMod(selectedModName)
				select t).ToHashSet();
			lTAll = (from t in CEditor.API.Get<HashSet<ThoughtDef>>(EType.ThoughtsAll)
				where t.IsFromMod(selectedModName)
				select t).ToHashSet();
		}
		AThoughtTypeSelected(selectedType);
	}

	private void AThoughtTypeSelected(string val)
	{
		selectedType = val;
		SelectedPawn = null;
		selectedStage = null;
		selectedThought = null;
		selectedTitle = null;
		dicStages.Clear();
		if (val == Label.TH_MEMORY)
		{
			lListThoughts = lTMemory;
		}
		else if (val == Label.TH_SOCIAL)
		{
			lListThoughts = lTMemorySocial;
		}
		else if (val == Label.TH_SITUATIONAL)
		{
			lListThoughts = lTSituational;
		}
		else if (val == Label.TH_SITUATIONAL + Label.TH_SOCIAL)
		{
			lListThoughts = lTSituationalSocial;
		}
		else if (val == Label.TH_UNSUPPORTED)
		{
			lListThoughts = lTUnsupported;
		}
		else
		{
			lListThoughts = lTAll;
		}
	}

	private void DrawThoughtList(int x, ref int y, int w, int h)
	{
		SZWidgets.ListView(new Rect(x, y, w, h), lListThoughts, FGetThoughtLabel, FGetTooltip, (ThoughtDef t1, ThoughtDef t2) => t1 == t2, ref selectedThought, ref scrollPos, withRemove: false, AThoughtSelected);
		y += 510;
	}

	private void AThoughtSelected(ThoughtDef t)
	{
		selectedThought = t;
		SelectedPawn = null;
		selectedStage = null;
		selectedTitle = null;
		dicStages.Clear();
		if (t == null)
		{
			return;
		}
		selectedMoodOffset = 1f;
		int num = selectedThought.stages.CountAllowNull();
		if (!selectedThought.stages.NullOrEmpty())
		{
			for (int i = 0; i < num; i++)
			{
				string thoughtLabel = t.GetThoughtLabel(i);
				if (thoughtLabel == t.defName)
				{
					dicStages.Add(i, "");
				}
				else
				{
					dicStages.Add(i, thoughtLabel);
				}
			}
			selectedStage = dicStages.First().Value;
			baseMoodOffset = (int)t.stages[dicStages.KeyByValue(selectedStage)].baseMoodEffect;
			selectedOpinionOffset = t.stages[dicStages.KeyByValue(selectedStage)].baseOpinionOffset;
		}
		else
		{
			selectedStage = null;
		}
	}

	private void DrawLowerDropdowns(int x, ref int y, int w, int h)
	{
		Text.Font = GameFont.Small;
		if (selectedThought.HasOtherPawnMember())
		{
			bNeedOtherPawn = true;
			SZWidgets.ButtonText(new Rect(x, y, w, h), FGetPawnName(SelectedPawn), ASelectPawn);
			y += 30;
		}
		else
		{
			bNeedOtherPawn = false;
			SelectedPawn = null;
		}
		if (selectedThought != null && selectedThought.stages.CountAllowNull() > 1)
		{
			bNeedStage = true;
			SZWidgets.FloatMenuOnButtonText(new Rect(x, y, w, h), selectedStage, dicStages.Values.ToList(), (string s) => s, AStageSelected);
			y += 30;
		}
		else
		{
			bNeedStage = false;
			selectedStage = null;
		}
		if (selectedThought != null && selectedThought.IsForTitle())
		{
			bNeedTitle = true;
			SZWidgets.FloatMenuOnButtonText(new Rect(x, y, w, h), FGetRoyalTitleLabel(selectedTitle), lRoyalTitles, FGetRoyalTitleLabel, ARoyalTitleSelected);
			y += 30;
		}
		else
		{
			bNeedTitle = false;
			selectedTitle = null;
		}
	}

	private void ASelectPawn()
	{
		WindowTool.Open(new DialogChoosePawn(this));
	}

	private void AStageSelected(string val)
	{
		selectedStage = val;
		if (!selectedThought.stages.NullOrEmpty())
		{
			baseMoodOffset = (int)selectedThought.stages[dicStages.KeyByValue(selectedStage)].baseMoodEffect;
			selectedOpinionOffset = selectedThought.stages[dicStages.KeyByValue(selectedStage)].baseOpinionOffset;
		}
	}

	private void ARoyalTitleSelected(RoyalTitleDef val)
	{
		selectedTitle = val;
	}

	private void DrawSlider(int x, ref int y, int w, int h)
	{
		if (selectedThought == null)
		{
			return;
		}
		Listing_X listing_X = new Listing_X();
		if (baseMoodOffset != 0f)
		{
			if (selectedThought.IsTypeOf<Thought_Memory>())
			{
				SZWidgets.SimpleMultiplierSlider(new Rect(x, y, w, 45f), "mood", "int", showNumeric: false, baseMoodOffset, ref selectedMoodOffset, -4f, 5f);
				y += 45;
			}
			else
			{
				int num = (int)(selectedMoodOffset * baseMoodOffset);
				string s = "mood [" + num + "]";
				SZWidgets.Label(new Rect(x, y, w, 30f), s.Colorize((num > 0) ? Color.green : ((num == 0) ? Color.grey : Color.red)));
				y += 30;
			}
		}
		if (selectedThought.IsTypeOf<Thought_MemorySocial>())
		{
			SZWidgets.SimpleMultiplierSlider(new Rect(x, y, w, 45f), "opinion", "int", showNumeric: false, 1f, ref selectedOpinionOffset, -100f, 100f);
			y += 45;
		}
		else if (selectedOpinionOffset != 0f || SelectedPawn != null)
		{
			SZWidgets.Label(new Rect(x, y, w, 30f), "opinion [" + (int)selectedOpinionOffset + "]");
			y += 30;
		}
	}

	private void DrawAccept(int x, ref int y, int w, int h)
	{
		bAllOk = selectedThought != null && (!bNeedOtherPawn || SelectedPawn != null) && (!bNeedTitle || selectedTitle != null) && (!bNeedStage || selectedStage != null);
		GUI.color = ((!bAllOk || (bNeedStage && selectedStage.NullOrEmpty())) ? Color.red : Color.green);
		if (bAllOk)
		{
			WindowTool.SimpleAcceptButton(this, DoAndClose);
		}
	}

	private void DoAndClose()
	{
		int stage = 0;
		if (selectedStage != null)
		{
			stage = dicStages.KeyByValue(selectedStage);
		}
		CEditor.API.Pawn.AddThought(selectedThought, SelectedPawn, stage, selectedTitle.SDefname(), selectedMoodOffset, selectedOpinionOffset);
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.ThoughtDef, windowRect.position);
		base.Close(doCloseSound);
	}
}
