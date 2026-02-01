using System.Text.RegularExpressions;
using Scriban.Runtime;

namespace Scriban.Functions;

public class RegexFunctions : ScriptObject
{
	public static string Escape(string pattern)
	{
		return Regex.Escape(pattern);
	}

	public static ScriptArray Match(TemplateContext context, string text, string pattern, string options = null)
	{
		Match match = Regex.Match(text, pattern, GetOptions(options), context.RegexTimeOut);
		ScriptArray scriptArray = new ScriptArray();
		if (match.Success)
		{
			foreach (Group group in match.Groups)
			{
				scriptArray.Add(group.Value);
			}
		}
		return scriptArray;
	}

	public static string Replace(TemplateContext context, string text, string pattern, string replace, string options = null)
	{
		return Regex.Replace(text, pattern, replace, GetOptions(options), context.RegexTimeOut);
	}

	public static ScriptArray Split(TemplateContext context, string text, string pattern, string options = null)
	{
		return new ScriptArray(Regex.Split(text, pattern, GetOptions(options), context.RegexTimeOut));
	}

	public static string Unescape(string pattern)
	{
		return Regex.Unescape(pattern);
	}

	private static RegexOptions GetOptions(string options)
	{
		if (options == null)
		{
			return RegexOptions.None;
		}
		RegexOptions regexOptions = RegexOptions.None;
		for (int i = 0; i < options.Length; i++)
		{
			switch (options[i])
			{
			case 'i':
				regexOptions |= RegexOptions.IgnoreCase;
				break;
			case 'm':
				regexOptions |= RegexOptions.Multiline;
				break;
			case 's':
				regexOptions |= RegexOptions.Singleline;
				break;
			case 'x':
				regexOptions |= RegexOptions.IgnorePatternWhitespace;
				break;
			}
		}
		return regexOptions;
	}
}
