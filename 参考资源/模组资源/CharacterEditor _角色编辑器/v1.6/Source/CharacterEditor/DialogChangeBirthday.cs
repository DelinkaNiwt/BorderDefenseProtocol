using System;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogChangeBirthday : Window, IPawnable
{
	private Vector2 scrollPos;

	private string selectedLifestage;

	private int iMaxLifestage;

	private int iSelectedLifestage;

	private int iSelectedHour;

	private int iSelectedDay;

	private int iSelctedQuadrum;

	private int iSelectedYear;

	private int iSelectedBioDay;

	private int iSelectedBioHour;

	private int iSelectedBioQuadrum;

	private int iSelectedBioYear;

	private string selectedHour;

	private string selectedDay;

	private string selectedQuadrum;

	private string selectedYear;

	private string selectedBioHour;

	private string selectedBioDay;

	private string selectedBioQuadrum;

	private string selectedBioYear;

	private long startTick;

	private long startBioTick;

	private long ticksPerDay = 60000L;

	private long ticksPerHour = 2500L;

	private long ticksPerQuadrum = 900000L;

	private long ticksPerYear = 3600000L;

	private bool doOnce;

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	public Pawn SelectedPawn { get; set; }

	public Pawn SelectedPawn2 { get; set; }

	public Pawn SelectedPawn3 { get; set; }

	public Pawn SelectedPawn4 { get; set; }

	internal DialogChangeBirthday(Pawn p)
	{
		scrollPos = default(Vector2);
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.ChangeBirthday);
		SelectedPawn = p;
		if (!p.RaceProps.lifeStageAges.NullOrEmpty())
		{
			iSelectedLifestage = p.ageTracker.CurLifeStageIndex;
			iMaxLifestage = p.RaceProps.lifeStageAges.Count - 1;
		}
		startTick = -19800059000L;
		long num = startTick - SelectedPawn.ageTracker.BirthAbsTicks;
		iSelectedYear = SelectedPawn.ageTracker.BirthYear;
		iSelctedQuadrum = (int)SelectedPawn.ageTracker.BirthQuadrum;
		iSelectedDay = (SelectedPawn.ageTracker.BirthDayOfYear + 1) % 15;
		if (iSelectedDay == 0)
		{
			iSelectedDay = 15;
		}
		num %= ticksPerYear;
		num %= ticksPerQuadrum;
		num %= ticksPerDay;
		iSelectedHour = Math.Abs((int)(num / ticksPerHour));
		num %= ticksPerHour;
		startBioTick = 0L;
		long num2 = startBioTick - SelectedPawn.ageTracker.AgeBiologicalTicks;
		iSelectedBioYear = SelectedPawn.ageTracker.AgeBiologicalYears;
		num2 %= ticksPerYear;
		iSelectedBioQuadrum = (int)Math.Abs(num2 / ticksPerQuadrum);
		num2 %= ticksPerQuadrum;
		iSelectedBioDay = (int)Math.Abs(num2 / ticksPerDay);
		num2 %= ticksPerDay;
		iSelectedBioHour = (int)Math.Abs(num2 / ticksPerHour);
		num2 %= ticksPerHour;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	public override void DoWindowContents(Rect inRect)
	{
		int num = 0;
		int num2 = 0;
		int num3 = (int)InitialSize.x - 16;
		int num4 = (int)InitialSize.y - 16 - 60;
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.ChangeBirthday, ref windowRect, ref doOnce, 370);
		}
		Rect outRect = new Rect(num, num2, num3, num4);
		Rect rect = new Rect(0f, 0f, outRect.width - 16f, 400f);
		Widgets.BeginScrollView(outRect, ref scrollPos, rect);
		Rect rect2 = rect.ContractedBy(4f);
		rect2.y -= 4f;
		rect2.height = 600f;
		Listing_X listing_X = new Listing_X();
		listing_X.Begin(rect2);
		listing_X.verticalSpacing = 30f;
		Text.Font = GameFont.Medium;
		listing_X.Label(Label.BIRTHDAY, 200f, 0f);
		listing_X.GapLine();
		listing_X.AddIntSection(Label.YEAR, "", ref selectedYear, ref iSelectedYear, -9999, 5500, small: true);
		listing_X.AddIntSection(Label.QUADRUM, "quadrum", ref selectedQuadrum, ref iSelctedQuadrum, 0, 3, small: true);
		listing_X.AddIntSection(Label.DAY, "", ref selectedDay, ref iSelectedDay, 1, 15, small: true);
		listing_X.AddIntSection(Label.HOUR, "", ref selectedHour, ref iSelectedHour, 0, 23, small: true);
		listing_X.Label(Label.CHRONOAGE + " [" + ChronologicalAge() + "]", -1f, 0f);
		listing_X.Gap(20f);
		listing_X.Label(Label.BIOAGE + " [" + iSelectedBioYear + "]", -1f, 0f);
		listing_X.GapLine();
		listing_X.AddIntSection(Label.YEARS, "", ref selectedBioYear, ref iSelectedBioYear, 0, (int)SelectedPawn.RaceProps.lifeExpectancy + 30, small: true);
		listing_X.AddIntSection(Label.QUARTALS, "", ref selectedBioQuadrum, ref iSelectedBioQuadrum, 0, 3, small: true);
		listing_X.AddIntSection(Label.DAYS, "", ref selectedBioDay, ref iSelectedBioDay, 0, 14, small: true);
		listing_X.AddIntSection(Label.HOURS, "", ref selectedBioHour, ref iSelectedBioHour, 0, 23, small: true);
		listing_X.Gap();
		if (iMaxLifestage > 0)
		{
			try
			{
				iSelectedLifestage = SelectedPawn.ageTracker.CurLifeStageIndex;
				listing_X.AddIntSection(Label.LIFESTAGE, "DEF" + SelectedPawn.RaceProps.lifeStageAges[iSelectedLifestage].def.LabelCap, ref selectedLifestage, ref iSelectedLifestage, 0, iMaxLifestage, small: true, SelectedPawn.ageTracker.CurLifeStage.defName);
			}
			catch
			{
			}
		}
		listing_X.End();
		Widgets.EndScrollView();
		WindowTool.SimpleAcceptButton(this, DoAndClose);
	}

	private void DoAndClose()
	{
		SelectedPawn.ageTracker.BirthAbsTicks = ChronoTicks();
		SelectedPawn.SetAgeTicks(BioTicks());
		if (iSelectedBioYear < 18 && SelectedPawn.HasStoryTracker())
		{
			SelectedPawn.SetBackstory(SelectedPawn.story.Childhood, null);
		}
		CEditor.API.UpdateGraphics();
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.ChangeBirthday, windowRect.position);
		base.Close(doCloseSound);
	}

	private int ChronologicalAge()
	{
		return (int)((GenTicks.TicksAbs - ChronoTicks()) / ticksPerYear);
	}

	private long ChronoTicks()
	{
		long num = startTick + iSelectedYear * ticksPerYear;
		num += iSelctedQuadrum * ticksPerQuadrum;
		num += iSelectedDay * ticksPerDay;
		return num + iSelectedHour * ticksPerHour;
	}

	private long BioTicks()
	{
		long num = startBioTick + iSelectedBioYear * ticksPerYear;
		num += iSelectedBioQuadrum * ticksPerQuadrum;
		num += iSelectedBioDay * ticksPerDay;
		return num + iSelectedBioHour * ticksPerHour;
	}
}
