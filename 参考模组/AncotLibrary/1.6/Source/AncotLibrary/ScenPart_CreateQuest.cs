using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class ScenPart_CreateQuest : ScenPart_QuestBase
{
	private const float IntervalMidpoint = 30f;

	private const float IntervalDeviation = 15f;

	private float intervalDays;

	private float minDays;

	private float maxDays;

	private bool repeat;

	private string intervalDaysBuffer;

	private string minDaysBuffer;

	private string maxDaysBuffer;

	private float occurTick;

	private bool isFinished;

	protected override string QuestTag => "CreateQuest";

	private float RandomInterval
	{
		get
		{
			float maxInclusive = ((maxDays > 0f && maxDays > minDays) ? maxDays : minDays);
			return Rand.Range(minDays, maxInclusive);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref intervalDays, "intervalDays", 0f);
		Scribe_Values.Look(ref minDays, "minDays", intervalDays);
		Scribe_Values.Look(ref maxDays, "maxDays", 0f);
		Scribe_Values.Look(ref repeat, "repeat", defaultValue: false);
		Scribe_Values.Look(ref occurTick, "occurTick", 0f);
		Scribe_Values.Look(ref isFinished, "isFinished", defaultValue: false);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode() ^ intervalDays.GetHashCode() ^ (repeat ? 1 : 0) ^ occurTick.GetHashCode() ^ (isFinished ? 1 : 0);
	}

	public override void DoEditInterface(Listing_ScenEdit listing)
	{
		Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 3f);
		Rect rect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, scenPartRect.height / 3f);
		Rect rect2 = new Rect(scenPartRect.x, scenPartRect.y + scenPartRect.height / 3f, scenPartRect.width / 2f, scenPartRect.height / 3f);
		Rect rect3 = new Rect(scenPartRect.x + scenPartRect.width / 2f, scenPartRect.y + scenPartRect.height / 3f, scenPartRect.width / 2f, scenPartRect.height / 3f);
		Rect rect4 = new Rect(scenPartRect.x, scenPartRect.y + scenPartRect.height * 2f / 3f, scenPartRect.width, scenPartRect.height / 3f);
		DoQuestEditInterface(rect);
		Widgets.TextFieldNumericLabeled(rect2, "minDays".Translate(), ref minDays, ref minDaysBuffer);
		Widgets.TextFieldNumericLabeled(rect3, "maxDays".Translate(), ref maxDays, ref maxDaysBuffer);
		Widgets.CheckboxLabeled(rect4, "repeat".Translate(), ref repeat);
	}

	public override void Randomize()
	{
		base.Randomize();
		intervalDays = 15f * Rand.Gaussian() + 30f;
		if (intervalDays < 0f)
		{
			intervalDays = 0f;
		}
		minDays = intervalDays;
		repeat = Rand.Range(0, 100) < 50;
	}

	protected override IEnumerable<QuestScriptDef> RandomizableQuests()
	{
		return DefDatabase<QuestScriptDef>.AllDefsListForReading.Where((QuestScriptDef def) => def.root != null);
	}

	public override void PostGameStart()
	{
		base.PostGameStart();
		occurTick = (float)Find.TickManager.TicksGame + 60000f * RandomInterval;
	}

	public override void Tick()
	{
		base.Tick();
		if (isFinished || Find.AnyPlayerHomeMap == null)
		{
			return;
		}
		if (questDef == null)
		{
			Log.Error("Trying to tick ScenPart_CreateQuest but the questDef is null");
			isFinished = true;
		}
		else if ((float)Find.TickManager.TicksGame >= occurTick)
		{
			Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, 0f);
			QuestUtility.SendLetterQuestAvailable(quest);
			if (repeat && (minDays > 0f || maxDays > minDays))
			{
				occurTick += 60000f * RandomInterval;
			}
			else
			{
				isFinished = true;
			}
		}
	}
}
