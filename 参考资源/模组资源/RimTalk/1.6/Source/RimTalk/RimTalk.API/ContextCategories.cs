using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RimTalk.API;

public static class ContextCategories
{
	public static class Pawn
	{
		public static readonly ContextCategory Name = new ContextCategory("name");

		public static readonly ContextCategory FullName = new ContextCategory("fullname");

		public static readonly ContextCategory Gender = new ContextCategory("gender");

		public static readonly ContextCategory Age = new ContextCategory("age");

		public static readonly ContextCategory Race = new ContextCategory("race");

		public static readonly ContextCategory Title = new ContextCategory("title");

		public static readonly ContextCategory Faction = new ContextCategory("faction");

		public static readonly ContextCategory Role = new ContextCategory("role");

		public static readonly ContextCategory Job = new ContextCategory("job");

		public static readonly ContextCategory Personality = new ContextCategory("personality");

		public static readonly ContextCategory Mood = new ContextCategory("mood");

		public static readonly ContextCategory MoodPercent = new ContextCategory("moodpercent");

		public static readonly ContextCategory Profile = new ContextCategory("profile");

		public static readonly ContextCategory Backstory = new ContextCategory("backstory");

		public static readonly ContextCategory Traits = new ContextCategory("traits");

		public static readonly ContextCategory Skills = new ContextCategory("skills");

		public static readonly ContextCategory Health = new ContextCategory("health");

		public static readonly ContextCategory Thoughts = new ContextCategory("thoughts");

		public static readonly ContextCategory Social = new ContextCategory("social");

		public static readonly ContextCategory Equipment = new ContextCategory("equipment");

		public static readonly ContextCategory Genes = new ContextCategory("genes");

		public static readonly ContextCategory Ideology = new ContextCategory("ideology");

		public static readonly ContextCategory CaptiveStatus = new ContextCategory("captive_status");

		public static readonly ContextCategory Location = new ContextCategory("location");

		public static readonly ContextCategory Terrain = new ContextCategory("terrain");

		public static readonly ContextCategory Beauty = new ContextCategory("beauty");

		public static readonly ContextCategory Cleanliness = new ContextCategory("cleanliness");

		public static readonly ContextCategory Surroundings = new ContextCategory("surroundings");

		private static readonly Lazy<IReadOnlyList<ContextCategory>> _all = new Lazy<IReadOnlyList<ContextCategory>>(() => (from f in typeof(Pawn).GetFields(BindingFlags.Static | BindingFlags.Public)
			where f.FieldType == typeof(ContextCategory)
			select (ContextCategory)f.GetValue(null)).ToList().AsReadOnly());

		public static IReadOnlyList<ContextCategory> All => _all.Value;
	}

	public static class Environment
	{
		public static readonly ContextCategory Time = new ContextCategory("time", ContextType.Environment);

		public static readonly ContextCategory Date = new ContextCategory("date", ContextType.Environment);

		public static readonly ContextCategory Season = new ContextCategory("season", ContextType.Environment);

		public static readonly ContextCategory Weather = new ContextCategory("weather", ContextType.Environment);

		public static readonly ContextCategory Temperature = new ContextCategory("temperature", ContextType.Environment);

		public static readonly ContextCategory Wealth = new ContextCategory("wealth", ContextType.Environment);

		private static readonly Lazy<IReadOnlyList<ContextCategory>> _all = new Lazy<IReadOnlyList<ContextCategory>>(() => (from f in typeof(Environment).GetFields(BindingFlags.Static | BindingFlags.Public)
			where f.FieldType == typeof(ContextCategory)
			select (ContextCategory)f.GetValue(null)).ToList().AsReadOnly());

		public static IReadOnlyList<ContextCategory> All => _all.Value;
	}

	public static ContextCategory? TryGetPawnCategory(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return null;
		}
		string lowerKey = key.ToLowerInvariant();
		return Pawn.All.FirstOrDefault((ContextCategory c) => c.Key == lowerKey);
	}

	public static ContextCategory? TryGetEnvironmentCategory(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return null;
		}
		string lowerKey = key.ToLowerInvariant();
		return Environment.All.FirstOrDefault((ContextCategory c) => c.Key == lowerKey);
	}
}
