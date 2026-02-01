using System;

namespace Scriban.Parsing;

[AttributeUsage(AttributeTargets.Field)]
internal class TokenTextAttribute : Attribute
{
	public string Text { get; }

	public TokenTextAttribute(string text)
	{
		Text = text;
	}
}
