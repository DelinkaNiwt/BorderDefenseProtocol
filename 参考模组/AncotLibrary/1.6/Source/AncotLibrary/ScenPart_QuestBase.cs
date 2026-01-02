using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public abstract class ScenPart_QuestBase : ScenPart
{
	protected QuestScriptDef questDef;

	protected abstract string QuestTag { get; }

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref questDef, "questDef");
		if (Scribe.mode == LoadSaveMode.PostLoadInit && questDef == null)
		{
			questDef = RandomizableQuests().FirstOrDefault();
			Log.Error("ScenPart has null questDef after loading. Changing to " + questDef.ToStringSafe());
		}
	}

	public override int GetHashCode()
	{
		return base.GetHashCode() ^ ((questDef != null) ? questDef.GetHashCode() : 0);
	}

	public override void DoEditInterface(Listing_ScenEdit listing)
	{
		Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight);
		DoQuestEditInterface(scenPartRect);
	}

	public override string Summary(Scenario scen)
	{
		string key = "Ancot.ScenPart_" + QuestTag;
		return ScenSummaryList.SummaryWithList(scen, QuestTag, key.Translate());
	}

	public override IEnumerable<string> GetSummaryListEntries(string tag)
	{
		if (tag == QuestTag)
		{
			yield return ((string)questDef.LabelCap != null) ? questDef.LabelCap : ((TaggedString)questDef.defName);
		}
	}

	public override void Randomize()
	{
		questDef = RandomizableQuests().RandomElement();
	}

	public override bool TryMerge(ScenPart other)
	{
		if (other is ScenPart_QuestBase scenPart_QuestBase)
		{
			return scenPart_QuestBase.questDef == questDef;
		}
		return false;
	}

	public override bool CanCoexistWith(ScenPart other)
	{
		if (other is ScenPart_QuestBase scenPart_QuestBase)
		{
			return scenPart_QuestBase.questDef != questDef;
		}
		return true;
	}

	protected virtual IEnumerable<QuestScriptDef> RandomizableQuests()
	{
		return Enumerable.Empty<QuestScriptDef>();
	}

	protected void DoQuestEditInterface(Rect rect)
	{
		if (!Widgets.ButtonText(rect, questDef.defName))
		{
			return;
		}
		FloatMenuUtility.MakeMenu(DefDatabase<QuestScriptDef>.AllDefsListForReading.Where((QuestScriptDef def) => def.root != null), (QuestScriptDef def) => def.defName, (QuestScriptDef def) => delegate
		{
			questDef = def;
		});
	}
}
