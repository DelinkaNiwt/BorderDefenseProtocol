using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace AncotLibrary;

public class DialogUtility
{
	public static DialogDef DialogFallBack(DialogSetDef dialogSetDef, Map map, Pawn initiator, Pawn recipient, out string diaOptionText)
	{
		DialogDef dialogDef = null;
		diaOptionText = "";
		List<DialogProperty> list = new List<DialogProperty>();
		if (dialogSetDef != null)
		{
			foreach (DialogProperty dialogProperty in dialogSetDef.dialogProperties)
			{
				if (dialogProperty.conditions == null)
				{
					list.Add(dialogProperty);
					continue;
				}
				float f = Mathf.Lerp(0f, 100f, map.skyManager.CurSkyGlow);
				_ = dialogProperty.conditions.skyGlow;
				if (dialogProperty.conditions.skyGlow.Includes(f))
				{
					list.Add(dialogProperty);
					continue;
				}
				int num = GenLocalDate.HourOfDay(map);
				_ = dialogProperty.conditions.hourInterval;
				if (dialogProperty.conditions.hourInterval.Includes(num))
				{
					list.Add(dialogProperty);
					continue;
				}
				if (!dialogProperty.conditions.weathers.NullOrEmpty() && dialogProperty.conditions.weathers.Contains(map.weatherManager.curWeather))
				{
					list.Add(dialogProperty);
					continue;
				}
				_ = dialogProperty.conditions.mapTemperature;
				if (dialogProperty.conditions.mapTemperature.Includes(map.mapTemperature.OutdoorTemp))
				{
					list.Add(dialogProperty);
					continue;
				}
				if (dialogProperty.conditions.gameConditions != null)
				{
					List<GameConditionDef> list2 = new List<GameConditionDef>();
					foreach (GameCondition activeCondition in map.gameConditionManager.ActiveConditions)
					{
						list2.Add(activeCondition.def);
					}
					if (list2.Count > 0 && dialogProperty.conditions.gameConditions.ContainsAny((GameConditionDef a) => list2.Contains(a)))
					{
						list.Add(dialogProperty);
						continue;
					}
				}
				if (initiator != null && dialogProperty.conditions.initiatorNeeds != null)
				{
					List<NeedDef> list3 = new List<NeedDef>();
					foreach (Need c in initiator.needs.AllNeeds)
					{
						NeedProperty needProperty = dialogProperty.conditions.initiatorNeeds.Find((NeedProperty a) => a.needDef.Equals(c));
						if (needProperty != null && needProperty.interval.Includes(c.CurLevelPercentage))
						{
							list.Add(dialogProperty);
						}
					}
				}
				if (recipient == null || dialogProperty.conditions.recipientNeeds == null)
				{
					continue;
				}
				List<NeedDef> list4 = new List<NeedDef>();
				foreach (Need c2 in recipient.needs.AllNeeds)
				{
					NeedProperty needProperty2 = dialogProperty.conditions.recipientNeeds.Find((NeedProperty a) => a.needDef.Equals(c2));
					if (needProperty2 != null && needProperty2.interval.Includes(c2.CurLevelPercentage))
					{
						list.Add(dialogProperty);
					}
				}
			}
		}
		if (list.Count > 0)
		{
			dialogDef = list.RandomElementByWeight((DialogProperty a) => a.weight).dialogDef;
			diaOptionText = dialogDef.label;
		}
		return dialogDef;
	}

	public static void OpenAssembledDialog(DialogDef dialogDef, string optionSignalA = null, string optionSignalB = null, string optionSignalC = null, string optionSignalD = null, string outSignal = null)
	{
		Dialog_NodeTree window = new Dialog_NodeTree(AssembleDialog(dialogDef, optionSignalA, optionSignalB, optionSignalC, optionSignalD, outSignal));
		Find.WindowStack.Add(window);
	}

	public static DiaNode AssembleDialog(DialogDef dialogDef, string optionSignalA = null, string optionSignalB = null, string optionSignalC = null, string optionSignalD = null, string outSignal = null)
	{
		if (dialogDef == null || dialogDef.diaNodes == null || dialogDef.diaNodes.Count == 0)
		{
			Log.Error("DialogDef is null or has no nodes.");
			return null;
		}
		Dictionary<DiaNodeDef, DiaNode> dictionary = new Dictionary<DiaNodeDef, DiaNode>();
		foreach (DiaNodeProperty diaNode in dialogDef.diaNodes)
		{
			if (diaNode.diaNodeDef == null)
			{
				Log.Error("DiaNodeProperty contains a null DiaNodeDef.");
				continue;
			}
			AssembleDiaNodeText(diaNode.diaNodeDef, out var text);
			DiaNode value = new DiaNode(text);
			dictionary[diaNode.diaNodeDef] = value;
		}
		foreach (DiaNodeProperty diaNode2 in dialogDef.diaNodes)
		{
			if (!dictionary.TryGetValue(diaNode2.diaNodeDef, out var value2) || diaNode2.diaOptions == null)
			{
				continue;
			}
			foreach (DiaOptionProperty optionSetting in diaNode2.diaOptions)
			{
				if (optionSetting.diaOptionDef == null)
				{
					Log.Error("DiaOptionProperty contains a null DiaOptionDef.");
					continue;
				}
				DiaOption diaOption = new DiaOption(optionSetting.diaOptionDef.label)
				{
					action = delegate
					{
						if (optionSetting.sendMessage != null)
						{
							Messages.Message(optionSetting.sendMessage, MessageTypeDefOf.NeutralEvent);
						}
						if (optionSetting.sendLetter != null)
						{
							Find.LetterStack.ReceiveLetter(optionSetting.sendLetter.letterLabel, optionSetting.sendLetter.letterDesc, optionSetting.sendLetter.letterDef);
						}
						if (optionSetting.sendOptionSignalA && optionSignalA != null)
						{
							Find.SignalManager.SendSignal(new Signal(optionSignalA));
						}
						if (optionSetting.sendOptionSignalB && optionSignalB != null)
						{
							Find.SignalManager.SendSignal(new Signal(optionSignalB));
						}
						if (optionSetting.sendOptionSignalC && optionSignalC != null)
						{
							Find.SignalManager.SendSignal(new Signal(optionSignalC));
						}
						if (optionSetting.sendOptionSignalD && optionSignalD != null)
						{
							Find.SignalManager.SendSignal(new Signal(optionSignalD));
						}
						if (optionSetting.resolveTree && outSignal != null)
						{
							Log.Message("输出信号" + outSignal);
							Find.SignalManager.SendSignal(new Signal(outSignal));
						}
					},
					resolveTree = optionSetting.resolveTree
				};
				if (optionSetting.linkTo != null && dictionary.TryGetValue(optionSetting.linkTo, out var value3))
				{
					diaOption.link = value3;
				}
				value2.options.Add(diaOption);
			}
		}
		return (dialogDef.diaNodes.Find((DiaNodeProperty d) => d.startAtThis)?.diaNodeDef != null) ? dictionary[dialogDef.diaNodes.Find((DiaNodeProperty d) => d.startAtThis).diaNodeDef] : dictionary.Values.First();
	}

	public static void AssembleDiaNodeText(DiaNodeDef diaNodeDef, out string text)
	{
		text = diaNodeDef.label;
		if (diaNodeDef.rulePack != null)
		{
			text = GrammarResolver.Resolve("r_logentry", new GrammarRequest
			{
				Includes = { diaNodeDef.rulePack }
			});
		}
	}
}
