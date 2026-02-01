using System;
using System.Net;
using System.Text.RegularExpressions;
using Scriban.Runtime;

namespace Scriban.Functions;

public class HtmlFunctions : ScriptObject
{
	private const string RegexMatchHtml = "<script.*?</script>|<!--.*?-->|<style.*?</style>|<(?:[^>=]|='[^']*'|=\"[^\"]*\"|=[^'\"][^\\s>]*)*>";

	public static string Strip(TemplateContext context, string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return new Regex("<script.*?</script>|<!--.*?-->|<style.*?</style>|<(?:[^>=]|='[^']*'|=\"[^\"]*\"|=[^'\"][^\\s>]*)*>", RegexOptions.IgnoreCase | RegexOptions.Singleline, context.RegexTimeOut).Replace(text, string.Empty);
	}

	public static string Escape(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return WebUtility.HtmlEncode(text);
	}

	public static string UrlEncode(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return Uri.EscapeDataString(text);
	}

	public static string UrlEscape(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		return Uri.EscapeUriString(text);
	}
}
