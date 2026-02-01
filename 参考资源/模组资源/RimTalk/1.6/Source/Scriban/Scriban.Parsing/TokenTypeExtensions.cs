using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Scriban.Helpers;

namespace Scriban.Parsing;

public static class TokenTypeExtensions
{
	private static readonly Dictionary<TokenType, string> TokenTexts;

	static TokenTypeExtensions()
	{
		TokenTexts = new Dictionary<TokenType, string>();
		foreach (FieldInfo item in from field in typeof(TokenType).GetTypeInfo().GetDeclaredFields()
			where field.IsPublic && field.IsStatic
			select field)
		{
			TokenTextAttribute customAttribute = item.GetCustomAttribute<TokenTextAttribute>();
			if (customAttribute != null)
			{
				TokenType key = (TokenType)item.GetValue(null);
				TokenTexts.Add(key, customAttribute.Text);
			}
		}
	}

	public static bool HasText(this TokenType type)
	{
		return TokenTexts.ContainsKey(type);
	}

	public static string ToText(this TokenType type)
	{
		TokenTexts.TryGetValue(type, out var value);
		return value;
	}
}
