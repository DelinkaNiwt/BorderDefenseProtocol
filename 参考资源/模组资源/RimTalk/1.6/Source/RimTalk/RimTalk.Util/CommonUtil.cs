using System;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalk.Util;

public static class CommonUtil
{
	public struct InGameData
	{
		public string Hour12HString;

		public string DateString;

		public string SeasonString;

		public string WeatherString;
	}

	public static bool HasPassed(int pastTick, double seconds)
	{
		return GenTicks.TicksGame - pastTick >= GetTicksForDuration(seconds);
	}

	public static int GetTicksForDuration(double seconds)
	{
		int tickRate = GetCurrentTickRate();
		return (int)(seconds * (double)tickRate);
	}

	private static int GetCurrentTickRate()
	{
		return Find.TickManager.CurTimeSpeed switch
		{
			TimeSpeed.Paused => 0, 
			TimeSpeed.Normal => 60, 
			TimeSpeed.Fast => 180, 
			TimeSpeed.Superfast => 360, 
			TimeSpeed.Ultrafast => 1500, 
			_ => 60, 
		};
	}

	public static InGameData GetInGameData()
	{
		InGameData mapData = new InGameData
		{
			Hour12HString = "N/A",
			DateString = "N/A",
			SeasonString = "N/A",
			WeatherString = "N/A"
		};
		try
		{
			Map currentMap = Find.CurrentMap;
			if (currentMap != null)
			{
				_ = currentMap.Tile;
				if (0 == 0)
				{
					long absTicks = Find.TickManager.TicksAbs;
					Vector2 longLat = Find.WorldGrid.LongLatOf(currentMap.Tile);
					mapData.Hour12HString = GetInGameHour12HString(absTicks, longLat);
					mapData.DateString = GetInGameDateString(absTicks, longLat);
					mapData.SeasonString = GetInGameSeasonString(absTicks, longLat);
					mapData.WeatherString = GetInGameWeatherString(currentMap);
					return mapData;
				}
			}
			return mapData;
		}
		catch (Exception)
		{
			return new InGameData
			{
				Hour12HString = "N/A",
				DateString = "N/A",
				SeasonString = "N/A",
				WeatherString = "N/A"
			};
		}
	}

	public static int GetInGameHour(long absTicks, Vector2 longLat)
	{
		return GenDate.HourOfDay(absTicks, longLat.x);
	}

	public static string GetInGameHour12HString(long absTicks, Vector2 longLat)
	{
		int hour24 = GetInGameHour(absTicks, longLat);
		int hour25 = hour24 % 12;
		if (hour25 == 0)
		{
			hour25 = 12;
		}
		string ampm = ((hour24 < 12) ? "am" : "pm");
		return $"{hour25}{ampm}";
	}

	private static string GetInGameDateString(long absTicks, Vector2 longLat)
	{
		return GenDate.DateFullStringAt(absTicks, longLat);
	}

	private static string GetInGameSeasonString(long absTicks, Vector2 longLat)
	{
		return GenDate.Season(absTicks, longLat).Label();
	}

	private static string GetInGameWeatherString(Map currentMap)
	{
		return currentMap.weatherManager?.curWeather?.label ?? "N/A";
	}

	public static int EstimateTokenCount(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		string normalizedText = Regex.Replace(text.Trim(), "\\s+", " ");
		double totalTokens = 0.0;
		string[] words = normalizedText.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string[] array = words;
		foreach (string word in array)
		{
			string cleanWord = word.Trim('!', '?', '.', ',', ':', ';', '"', '\'', '(', ')', '[', ']', '{', '}');
			if (cleanWord.Length == 0)
			{
				totalTokens += 1.0;
			}
			else if (cleanWord.Length <= 3)
			{
				totalTokens += 1.0;
				if (cleanWord.Length != word.Length)
				{
					totalTokens += 0.5;
				}
			}
			else if (cleanWord.Length <= 6)
			{
				totalTokens += 1.0;
				if (cleanWord.Length > 4)
				{
					totalTokens += 0.5;
				}
				if (cleanWord.Length != word.Length)
				{
					totalTokens += 0.5;
				}
			}
			else
			{
				totalTokens += Math.Max(1.0, Math.Ceiling((double)cleanWord.Length / 3.5));
				if (cleanWord.Length != word.Length)
				{
					totalTokens += 0.5;
				}
			}
		}
		totalTokens += Math.Max(1.0, totalTokens * 0.02);
		return Math.Max(1, (int)Math.Ceiling(totalTokens));
	}

	public static int GetMaxAllowedTokens(int cooldownSeconds)
	{
		return Math.Min(80 * cooldownSeconds, 800);
	}

	public static bool ShouldAiBeActiveOnSpeed()
	{
		RimTalkSettings settings = Settings.Get();
		if (settings.DisableAiAtSpeed == 0)
		{
			return true;
		}
		TimeSpeed currentGameSpeed = Find.TickManager.CurTimeSpeed;
		return (int)currentGameSpeed < settings.DisableAiAtSpeed;
	}

	public static string Sanitize(string text, Pawn pawn = null)
	{
		if (pawn != null)
		{
			text = text.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn).Resolve();
		}
		return text.StripTags().RemoveLineBreaks();
	}

	public static string StripFormattingTags(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		text = Regex.Replace(text, "<color[^>]*>|</color>", string.Empty);
		text = Regex.Replace(text, "<b>|</b>", string.Empty);
		text = Regex.Replace(text, "<i>|</i>", string.Empty);
		text = Regex.Replace(text, "<size[^>]*>|</size>", string.Empty);
		return text;
	}
}
