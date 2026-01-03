using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace AlienRace.ExtendedGraphics;

public class DefaultGraphicsLoader(IGraphicFinder<Texture2D> graphicFinder2D) : IGraphicsLoader
{
	public DefaultGraphicsLoader()
		: this(new GraphicFinder2D())
	{
	}

	private static void LogFor(StringBuilder logBuilder, string logLine, bool shouldLog = false)
	{
		if (shouldLog && AlienRaceMod.settings.textureLogs)
		{
			logBuilder.AppendLine(logLine);
		}
	}

	private void LoadAll2DVariantsForGraphic(IExtendedGraphic graphic, StringBuilder logBuilder, string source, bool shouldLog = false)
	{
		graphic.Init();
		LogFor(logBuilder, "Loading variants for " + graphic.GetPath());
		for (int i = 0; i < graphic.GetPathCount(); i++)
		{
			while (graphicFinder2D.GetByPath(graphic.GetPath(i), graphic.GetVariantCount(i), "south", reportFailure: false) != null)
			{
				graphic.IncrementVariantCount(i);
			}
			LogFor(logBuilder, $"Variants found for {graphic.GetPath(i)}: {graphic.GetVariantCount(i)}", shouldLog);
		}
		if (graphic.GetVariantCount() <= 0 && graphic.UseFallback())
		{
			for (int j = 0; j < graphic.GetPathCount(); j++)
			{
				while (graphicFinder2D.GetByPath(graphic.GetPath(j), graphic.GetVariantCount(j), "south", reportFailure: false) != null)
				{
					graphic.IncrementVariantCount(j);
				}
				LogFor(logBuilder, $"Variants found for {graphic.GetPath(j)}: {graphic.GetVariantCount(j)}", shouldLog);
			}
		}
		LogFor(logBuilder, $"Total variants found for {graphic.GetPath()}: {graphic.GetVariantCount()}", shouldLog);
		if (graphic.GetVariantCount() == 0 && Prefs.DevMode)
		{
			LogFor(logBuilder, $"No graphics found at {graphic.GetPath()} for {graphic.GetType()} in {source}.", shouldLog);
		}
	}

	public void LoadAllGraphics(string source, params AlienPartGenerator.ExtendedGraphicTop[] graphicTops)
	{
		Stack<IEnumerable<IExtendedGraphic>> topGraphics = new Stack<IEnumerable<IExtendedGraphic>>();
		StringBuilder logBuilder = new StringBuilder();
		foreach (AlienPartGenerator.ExtendedGraphicTop topGraphic in graphicTops)
		{
			if (topGraphic is AlienPartGenerator.BodyAddon ba)
			{
				ba.resolveData.head = ba.alignWithHead;
				foreach (Condition baCondition in ba.conditions)
				{
					AddConditionTypesFromCondition(baCondition);
				}
			}
			if (topGraphic.GetVariantCount() != 0)
			{
				continue;
			}
			LoadAll2DVariantsForGraphic(topGraphic, logBuilder, source, topGraphic.Debug);
			topGraphic.VariantCountMax = topGraphic.GetVariantCount();
			topGraphics.Push(topGraphic.GetSubGraphics());
			while (topGraphics.Count > 0)
			{
				IEnumerable<IExtendedGraphic> subGraphicSet = topGraphics.Pop();
				foreach (IExtendedGraphic currentGraphic in subGraphicSet)
				{
					if (currentGraphic == null)
					{
						break;
					}
					LoadAll2DVariantsForGraphic(currentGraphic, logBuilder, source, topGraphic.Debug);
					topGraphic.VariantCountMax = currentGraphic.GetVariantCount();
					if (currentGraphic is AlienPartGenerator.ExtendedConditionGraphic conditionGraphic)
					{
						foreach (Condition condition in conditionGraphic.conditions)
						{
							AddConditionTypesFromCondition(condition);
						}
					}
					topGraphics.Push(currentGraphic.GetSubGraphics());
				}
			}
			if (topGraphic.VariantCountMax <= 0 && !topGraphic.path.Equals(string.Empty))
			{
				Log.Message("Textures were not found for one or more extended graphics for " + source + ": " + topGraphic.path);
			}
			void AddConditionTypesFromCondition(Condition condition2)
			{
				if (condition2 is ConditionLogicCollection clc)
				{
					{
						foreach (Condition clcCondition in clc.conditions)
						{
							AddConditionTypesFromCondition(clcCondition);
						}
						return;
					}
				}
				if (condition2 is ConditionLogicSingle cls)
				{
					AddConditionTypesFromCondition(cls.condition);
				}
				else
				{
					topGraphic.conditionTypes.Add(condition2.GetType());
				}
			}
		}
		if (logBuilder.Length > 0)
		{
			Log.Message($"Loaded graphic variants for {source}\n{logBuilder}");
		}
	}
}
