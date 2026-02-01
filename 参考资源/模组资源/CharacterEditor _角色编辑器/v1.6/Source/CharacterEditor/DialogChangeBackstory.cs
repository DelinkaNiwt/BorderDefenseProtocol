using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogChangeBackstory : Window
{
	private Dictionary<BackstoryDef, string> dicBackstory;

	private bool isChildhood;

	private bool isFiltered;

	private bool isFilteredOld;

	private Vector2 scrollPos;

	private BackstoryDef selectedBackstory;

	private string selectedCategory;

	private KeyValuePair<string, SkillDef> selectedFilter;

	private string selectedFilter2;

	private bool doOnce;

	private Gender gender;

	private HashSet<string> lOfCategories;

	private Dictionary<string, SkillDef> dicOfFilters;

	private List<string> lOfFilter2;

	private List<BackstoryDef> lOfBackstories;

	private BackstoryDef oBackAdult;

	private BackstoryDef oBackChild;

	private Func<BackstoryDef, string> FBackstoryLabel;

	private Func<BackstoryDef, string> FBackstoryTooltip;

	private Func<BackstoryDef, BackstoryDef, bool> FBackstoryComparator;

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	internal void RememberOldBackstory()
	{
		oBackAdult = CEditor.API.Pawn.story.GetBackstory(BackstorySlot.Adulthood);
		oBackChild = CEditor.API.Pawn.story.GetBackstory(BackstorySlot.Childhood);
	}

	internal void RecalcSkills()
	{
		BackstoryDef backstory = CEditor.API.Pawn.story.GetBackstory(BackstorySlot.Adulthood);
		BackstoryDef backstory2 = CEditor.API.Pawn.story.GetBackstory(BackstorySlot.Childhood);
		foreach (SkillRecord skill in CEditor.API.Pawn.skills.skills)
		{
			int num = TryGetSkillValueForSkill(oBackAdult, skill.def);
			int num2 = TryGetSkillValueForSkill(oBackChild, skill.def);
			int num3 = TryGetSkillValueForSkill(backstory, skill.def);
			int num4 = TryGetSkillValueForSkill(backstory2, skill.def);
			skill.levelInt -= ((oBackAdult != null) ? num : 0);
			skill.levelInt -= ((oBackChild != null) ? num2 : 0);
			skill.levelInt += ((backstory != null) ? num3 : 0);
			skill.levelInt += ((backstory2 != null) ? num4 : 0);
		}
	}

	internal int TryGetSkillValueForSkill(BackstoryDef def, SkillDef skillDef)
	{
		int result = 0;
		if (def != null)
		{
			foreach (SkillGain skillGain in def.skillGains)
			{
				if (skillGain.skill == skillDef)
				{
					result = skillGain.amount;
					break;
				}
			}
		}
		return result;
	}

	internal DialogChangeBackstory(bool _isChildhood)
	{
		RememberOldBackstory();
		scrollPos = default(Vector2);
		isChildhood = _isChildhood;
		selectedBackstory = (isChildhood ? CEditor.API.Pawn.story.Childhood : CEditor.API.Pawn.story.Adulthood);
		selectedCategory = Label.ALL;
		selectedFilter = new KeyValuePair<string, SkillDef>(Label.ALL, null);
		selectedFilter2 = Label.ALL;
		isFiltered = false;
		isFilteredOld = false;
		doOnce = true;
		gender = CEditor.API.Pawn.gender;
		MessageTool.Show("backstory=" + selectedBackstory.SDefname());
		UpdateDictionary();
		SearchTool.Update(_isChildhood ? SearchTool.SIndex.BackstoryDefChild : SearchTool.SIndex.BackstroryDefAdult);
		List<List<string>> list = dicBackstory.Keys.Select((BackstoryDef td) => td.spawnCategories).ToList();
		lOfCategories = new HashSet<string>();
		lOfCategories.Add(Label.ALL);
		foreach (List<string> item in list)
		{
			foreach (string item2 in item)
			{
				lOfCategories.Add(item2);
			}
		}
		dicOfFilters = new Dictionary<string, SkillDef>();
		dicOfFilters.Add(Label.ALL, null);
		foreach (SkillRecord skill in CEditor.API.Pawn.skills.skills)
		{
			dicOfFilters.Add(skill.def.LabelCap.ToString() + " +", skill.def);
			dicOfFilters.Add(skill.def.LabelCap.ToString() + " ++", skill.def);
			dicOfFilters.Add(skill.def.LabelCap.ToString() + " +++", skill.def);
		}
		lOfFilter2 = new List<string>();
		lOfFilter2.Add(Label.ALL);
		lOfFilter2.Add(Label.POSITIVE_ONLY);
		lOfFilter2.Add(Label.SUMME + " > 3");
		lOfFilter2.Add(Label.SUMME + " > 4");
		lOfFilter2.Add(Label.SUMME + " > 5");
		lOfFilter2.Add(Label.SUMME + " > 6");
		lOfFilter2.Add(Label.SUMME + " > 7");
		lOfFilter2.Add(Label.SUMME + " > 8");
		lOfFilter2.Add(Label.SUMME + " > 9");
		lOfFilter2.Add(Label.SUMME + " > 10");
		lOfFilter2.Add(Label.SUMME + " > 11");
		lOfFilter2.Add(Label.SUMME + " > 12");
		FBackstoryLabel = GetBackstoryLabel;
		FBackstoryTooltip = GetBackstoryTooltip;
		FBackstoryComparator = IsSameBackstory;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	private string GetBackstoryLabel(BackstoryDef backstory)
	{
		return backstory.TitleCapFor(gender);
	}

	private string GetBackstoryTooltip(BackstoryDef backstory)
	{
		return dicBackstory[backstory];
	}

	private bool IsSameBackstory(BackstoryDef a, BackstoryDef b)
	{
		return a == b;
	}

	private bool HasSkillDef(List<SkillGain> l, SkillDef skillDef)
	{
		foreach (SkillGain item in l)
		{
			if (item.skill == skillDef)
			{
				return true;
			}
		}
		return false;
	}

	private int GetSkillgainValue(List<SkillGain> l, SkillDef skillDef)
	{
		int result = 0;
		if (l != null)
		{
			foreach (SkillGain item in l)
			{
				if (item.skill == skillDef)
				{
					result = item.amount;
					break;
				}
			}
		}
		return result;
	}

	private void UpdateDictionary()
	{
		dicBackstory = new Dictionary<BackstoryDef, string>();
		List<BackstoryDef> list = (from td in DefDatabase<BackstoryDef>.AllDefs
			where td != null && (uint)td.slot == ((!isChildhood) ? 1u : 0u) && !string.IsNullOrEmpty(td.title) && (!isFiltered || td.DisabledWorkTypes.Count() == 0)
			orderby td.title
			select td).ToList();
		bool flag = selectedCategory == null || selectedCategory == Label.ALL;
		bool flag2 = selectedFilter.Key == null || selectedFilter.Key == Label.ALL;
		bool flag3 = selectedFilter2 == null || selectedFilter2 == Label.ALL;
		foreach (BackstoryDef item in list)
		{
			bool flag4 = false;
			if (!flag && !item.spawnCategories.Contains(selectedCategory))
			{
				continue;
			}
			if (flag2)
			{
				flag4 = true;
			}
			else if (selectedFilter.Value == null || HasSkillDef(item.skillGains, selectedFilter.Value))
			{
				flag4 = false;
				int num = ((selectedFilter.Value != null) ? GetSkillgainValue(item.skillGains, selectedFilter.Value) : 0);
				if (selectedFilter.Key.EndsWith("+++"))
				{
					if (num > 3)
					{
						flag4 = true;
					}
				}
				else if (selectedFilter.Key.EndsWith("++"))
				{
					if (num > 2)
					{
						flag4 = true;
					}
				}
				else if (selectedFilter.Key.EndsWith("+") && num > 0)
				{
					flag4 = true;
				}
			}
			if (flag4)
			{
				if (flag3)
				{
					flag4 = true;
				}
				else if (selectedFilter2.Contains(" > "))
				{
					flag4 = false;
					int num2 = 0;
					int num3 = selectedFilter2.SubstringFrom(">").Trim().AsInt32();
					foreach (SkillGain skillGain in item.skillGains)
					{
						num2 += skillGain.amount;
					}
					if (num2 > num3)
					{
						flag4 = true;
					}
				}
				else if (selectedFilter2 == Label.POSITIVE_ONLY)
				{
					flag4 = true;
					foreach (SkillGain skillGain2 in item.skillGains)
					{
						flag4 &= skillGain2.amount > 0;
					}
				}
			}
			if (flag4)
			{
				dicBackstory.Add(item, item.FullDescriptionFor(CEditor.API.Pawn).Resolve());
			}
		}
		lOfBackstories = dicBackstory.Keys.ToList();
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (doOnce)
		{
			SearchTool.SetPosition(isChildhood ? SearchTool.SIndex.BackstoryDefChild : SearchTool.SIndex.BackstroryDefAdult, ref windowRect, ref doOnce, 105);
		}
		float num = InitialSize.x - 40f;
		float num2 = InitialSize.y - 150f;
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(0f, 0f, num, 30f), isChildhood ? "Childhood".Translate() : "Adulthood".Translate());
		SZWidgets.ButtonImage(num - 25f, 0f, 25f, 25f, "brandom", ARandomBackstory);
		Text.Font = GameFont.Small;
		Widgets.CheckboxLabeled(new Rect(0f, 30f, num, 30f), Label.NO_BLOCKING_SKILLS, ref isFiltered);
		if (isFiltered != isFilteredOld)
		{
			isFilteredOld = isFiltered;
			UpdateDictionary();
		}
		SZWidgets.FloatMenuOnButtonText(new Rect(0f, 60f, num, 30f), selectedCategory ?? Label.ALL, lOfCategories, (string s) => s, ACategoryChanged);
		SZWidgets.FloatMenuOnButtonText(new Rect(0f, 90f, num, 30f), selectedFilter.Key, dicOfFilters.Keys.ToList(), (string s) => s, AFilterChanged);
		SZWidgets.FloatMenuOnButtonText(new Rect(0f, 120f, num, 30f), selectedFilter2, lOfFilter2, (string s) => s, AFilter2Changed);
		SZWidgets.ListView(0f, 150f, num, num2 - 85f, lOfBackstories, FBackstoryLabel, FBackstoryTooltip, FBackstoryComparator, ref selectedBackstory, ref scrollPos);
		WindowTool.SimpleAcceptButton(this, DoAndClose);
		if ((isChildhood && CEditor.API.Pawn.ageTracker.AgeBiologicalYears < 3) || !isChildhood)
		{
			WindowTool.SimpleCustomButton(this, 0, DoRemoveAndClose, "Remove".Translate(), "");
		}
	}

	private void ACategoryChanged(string val)
	{
		selectedCategory = val;
		UpdateDictionary();
	}

	private void AFilterChanged(string val)
	{
		selectedFilter = new KeyValuePair<string, SkillDef>(val, dicOfFilters[val]);
		UpdateDictionary();
	}

	private void AFilter2Changed(string val)
	{
		selectedFilter2 = val;
		UpdateDictionary();
	}

	private void DoRemoveAndClose()
	{
		selectedBackstory = null;
		DoAndClose();
	}

	private void DoAndClose()
	{
		BackstoryDef childhood = (isChildhood ? selectedBackstory : CEditor.API.Pawn.story?.Childhood);
		BackstoryDef adulthood = ((!isChildhood) ? selectedBackstory : CEditor.API.Pawn.story?.Adulthood);
		CEditor.API.Pawn.SetBackstory(childhood, adulthood);
		RecalcSkills();
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(isChildhood ? SearchTool.SIndex.BackstoryDefChild : SearchTool.SIndex.BackstroryDefAdult, windowRect.position);
		base.Close(doCloseSound);
	}

	public override void OnAcceptKeyPressed()
	{
		base.OnAcceptKeyPressed();
		DoAndClose();
	}

	private void ARandomBackstory()
	{
		selectedBackstory = lOfBackstories.RandomElement();
		SZWidgets.sFind = FBackstoryLabel(selectedBackstory);
	}
}
