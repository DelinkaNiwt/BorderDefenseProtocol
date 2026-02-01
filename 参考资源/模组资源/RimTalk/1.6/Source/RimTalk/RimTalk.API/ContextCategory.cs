using System;
using System.Collections.Generic;

namespace RimTalk.API;

public readonly struct ContextCategory : IEquatable<ContextCategory>
{
	private class CategoryComparer : IEqualityComparer<ContextCategory>
	{
		public bool Equals(ContextCategory x, ContextCategory y)
		{
			return x.Key == y.Key && x.Type == y.Type;
		}

		public int GetHashCode(ContextCategory obj)
		{
			return ((obj.Key?.GetHashCode() ?? 0) * 397) ^ (int)obj.Type;
		}
	}

	public static readonly IEqualityComparer<ContextCategory> Comparer = new CategoryComparer();

	public string Key { get; }

	public ContextType Type { get; }

	public ContextCategory(string key, ContextType type = ContextType.Pawn)
	{
		Key = key?.ToLowerInvariant() ?? throw new ArgumentNullException("key");
		Type = type;
	}

	public bool Equals(ContextCategory other)
	{
		return Key == other.Key && Type == other.Type;
	}

	public override bool Equals(object obj)
	{
		return obj is ContextCategory c && Equals(c);
	}

	public override int GetHashCode()
	{
		return ((Key?.GetHashCode() ?? 0) * 397) ^ (int)Type;
	}

	public override string ToString()
	{
		return $"{Type}:{Key}";
	}

	public static bool operator ==(ContextCategory left, ContextCategory right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ContextCategory left, ContextCategory right)
	{
		return !left.Equals(right);
	}
}
