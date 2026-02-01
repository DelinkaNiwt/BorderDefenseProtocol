using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimTalk.API;
using RimTalk.Data;
using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;
using UnityEngine;
using Verse;

namespace RimTalk.Prompt;

public static class ScribanParser
{
	public static string Render(string templateText, PromptContext context, bool logErrors = true)
	{
		if (string.IsNullOrWhiteSpace(templateText))
		{
			return "";
		}
		try
		{
			Template template = Template.Parse(templateText);
			if (template.HasErrors)
			{
				if (logErrors)
				{
					global::RimTalk.Util.Logger.Error("Scriban Parse Errors: " + string.Join("\n", template.Messages));
				}
				return templateText;
			}
			ScriptObject scriptObject = new ScriptObject();
			scriptObject.Import(context, (MemberInfo m) => !(m is MethodInfo methodInfo) || !(methodInfo.ReturnType == typeof(void)));
			scriptObject.Add("ctx", context);
			scriptObject.Add("pawn", context.CurrentPawn);
			scriptObject.Add("recipient", context.TalkRequest?.Recipient);
			scriptObject.Add("pawns", context.AllPawns);
			scriptObject.Add("map", context.Map);
			scriptObject.Add("settings", Settings.Get());
			scriptObject.Import(typeof(PawnUtil), (MemberInfo m) => !(m is MethodInfo methodInfo) || !(methodInfo.ReturnType == typeof(void)), (MemberInfo m) => m.Name);
			scriptObject.Import(typeof(CommonUtil), (MemberInfo m) => !(m is MethodInfo methodInfo) || !(methodInfo.ReturnType == typeof(void)), (MemberInfo m) => m.Name);
			scriptObject.Add("PawnsFinder", typeof(PawnsFinder));
			scriptObject.Add("Find", typeof(Find));
			scriptObject.Add("GenDate", typeof(GenDate));
			scriptObject.Add("lang", Constant.Lang);
			int ticks = Find.TickManager.TicksAbs;
			if (context.Map != null)
			{
				Vector2 longLat = Find.WorldGrid.LongLatOf(context.Map.Tile);
				scriptObject.Add("hour", GenDate.HourOfDay(ticks, longLat.x));
				scriptObject.Add("day", GenDate.DayOfQuadrum(ticks, longLat.x) + 1);
				scriptObject.Add("quadrum", GenDate.Quadrum(ticks, longLat.x).Label());
				scriptObject.Add("year", GenDate.Year(ticks, longLat.x));
				scriptObject.Add("season", GenLocalDate.Season(context.Map).Label());
			}
			else
			{
				scriptObject.Add("hour", GenDate.HourOfDay(ticks, 0f));
				scriptObject.Add("day", GenDate.DayOfQuadrum(ticks, 0f) + 1);
				scriptObject.Add("quadrum", GenDate.Quadrum(ticks, 0f).Label());
				scriptObject.Add("year", GenDate.Year(ticks, 0f));
				scriptObject.Add("season", Season.Undefined.Label());
			}
			ScriptObject json = new ScriptObject();
			json.Add("format", Constant.GetJsonInstruction(Settings.Get().ApplyMoodAndSocialEffects));
			scriptObject.Add("json", json);
			ScriptObject chat = new ScriptObject();
			string historyText = "";
			if (context.ChatHistory != null && context.ChatHistory.Count > 0)
			{
				historyText = string.Join("\n\n", context.ChatHistory.Select(((Role role, string message) h) => $"[{h.role}] {h.message}"));
			}
			else if (context.IsPreview)
			{
				historyText = "[User] (Preview) Hello!\n\n[AI] (Preview) Greetings from RimTalk. This is a placeholder for chat history.";
			}
			chat.Add("history", historyText);
			scriptObject.Add("chat", chat);
			scriptObject.Add("prompt", context.DialoguePrompt);
			scriptObject.Add("context", context.PawnContext);
			if (context.VariableStore != null)
			{
				foreach (KeyValuePair<string, string> kvp in context.VariableStore.GetAllVariables())
				{
					if (!scriptObject.ContainsKey(kvp.Key))
					{
						scriptObject.Add(kvp.Key, kvp.Value);
					}
				}
			}
			TemplateContext templateContext = new TemplateContext
			{
				MemberRenamer = (MemberInfo m) => m.Name,
				MemberFilter = (MemberInfo m) => !(m is MethodInfo methodInfo) || !(methodInfo.ReturnType == typeof(void))
			};
			templateContext.TryGetVariable = delegate(TemplateContext tctx, SourceSpan span, ScriptVariable variable, out object value)
			{
				value = null;
				string varName = variable.Name;
				if (string.IsNullOrEmpty(varName))
				{
					return false;
				}
				if (ContextHookRegistry.TryGetContextVariable(varName, context, out var value2))
				{
					value = value2;
					return true;
				}
				ScriptObject builtinObject = tctx.BuiltinObject;
				if (builtinObject.TryGetValue(varName, out value))
				{
					return true;
				}
				string text = builtinObject.Keys.FirstOrDefault((string k) => k.Equals(varName, StringComparison.OrdinalIgnoreCase));
				if (text != null)
				{
					value = builtinObject[text];
					return true;
				}
				return false;
			};
			templateContext.TryGetMember = delegate(TemplateContext tctx, SourceSpan span, object target, string member, out object value)
			{
				value = null;
				if (target is Pawn pawn)
				{
					if (ContextHookRegistry.TryGetPawnVariable(member, pawn, out var value2))
					{
						value = value2;
						return true;
					}
					ContextCategory? contextCategory = ContextCategories.TryGetPawnCategory(member);
					if (contextCategory.HasValue)
					{
						string magicPawnValue = GetMagicPawnValue(pawn, member);
						value = ContextHookRegistry.ApplyPawnHooks(contextCategory.Value, pawn, magicPawnValue);
						return true;
					}
				}
				else if (target is Map map)
				{
					if (ContextHookRegistry.TryGetEnvironmentVariable(member, map, out var value3))
					{
						value = value3;
						return true;
					}
					ContextCategory? contextCategory2 = ContextCategories.TryGetEnvironmentCategory(member);
					if (contextCategory2.HasValue)
					{
						string magicMapValue = GetMagicMapValue(map, member);
						value = ContextHookRegistry.ApplyEnvironmentHooks(contextCategory2.Value, map, magicMapValue);
						return true;
					}
				}
				if (target is IDictionary<string, object> dictionary)
				{
					if (dictionary.TryGetValue(member, out value))
					{
						return true;
					}
					string text = dictionary.Keys.FirstOrDefault((string k) => k.Equals(member, StringComparison.OrdinalIgnoreCase));
					if (text != null)
					{
						value = dictionary[text];
						return true;
					}
				}
				if (target is Type type)
				{
					BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public;
					PropertyInfo propertyInfo = type.GetProperties(bindingAttr).FirstOrDefault((PropertyInfo p) => p.Name.Equals(member, StringComparison.OrdinalIgnoreCase));
					if (propertyInfo != null)
					{
						value = propertyInfo.GetValue(null);
						return true;
					}
					FieldInfo fieldInfo = type.GetFields(bindingAttr).FirstOrDefault((FieldInfo f) => f.Name.Equals(member, StringComparison.OrdinalIgnoreCase));
					if (fieldInfo != null)
					{
						value = fieldInfo.GetValue(null);
						return true;
					}
				}
				if (target != null && !(target is IDictionary<string, object>))
				{
					Type type2 = target.GetType();
					BindingFlags bindingAttr2 = BindingFlags.Instance | BindingFlags.Public;
					PropertyInfo propertyInfo2 = type2.GetProperties(bindingAttr2).FirstOrDefault((PropertyInfo p) => p.Name.Equals(member, StringComparison.OrdinalIgnoreCase));
					if (propertyInfo2 != null)
					{
						value = propertyInfo2.GetValue(target);
						return true;
					}
					FieldInfo fieldInfo2 = type2.GetFields(bindingAttr2).FirstOrDefault((FieldInfo f) => f.Name.Equals(member, StringComparison.OrdinalIgnoreCase));
					if (fieldInfo2 != null)
					{
						value = fieldInfo2.GetValue(target);
						return true;
					}
				}
				return false;
			};
			templateContext.PushGlobal(scriptObject);
			return template.Render(templateContext);
		}
		catch (Exception ex)
		{
			if (logErrors)
			{
				global::RimTalk.Util.Logger.Error("Scriban Render Error: " + ex.Message);
			}
			return templateText;
		}
	}

	private static string GetMagicPawnValue(Pawn pawn, string member)
	{
		string text = member.ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = text switch
		{
			"name" => pawn.LabelShort, 
			"job" => pawn.GetActivity(), 
			"role" => pawn.GetRole(), 
			"mood" => pawn.needs?.mood?.MoodString ?? "", 
			"personality" => global::RimTalk.Data.Cache.Get(pawn)?.Personality ?? "", 
			"social" => RelationsService.GetRelationsString(pawn), 
			"location" => PromptContextProvider.GetLocationString(pawn), 
			"beauty" => PromptContextProvider.GetBeautyString(pawn), 
			"cleanliness" => PromptContextProvider.GetCleanlinessString(pawn), 
			"surroundings" => ContextHelper.CollectNearbyContextText(pawn, 3) ?? "", 
			_ => null, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string GetMagicMapValue(Map map, string member)
	{
		string text = member.ToLowerInvariant();
		if (1 == 0)
		{
		}
		string result = ((text == "weather") ? (map.weatherManager?.curWeather?.label ?? "") : ((!(text == "temperature")) ? null : Mathf.RoundToInt(map.mapTemperature.OutdoorTemp).ToString()));
		if (1 == 0)
		{
		}
		return result;
	}
}
