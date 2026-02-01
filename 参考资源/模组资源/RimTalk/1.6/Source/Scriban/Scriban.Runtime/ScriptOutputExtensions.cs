using System;

namespace Scriban.Runtime;

public static class ScriptOutputExtensions
{
	public static IScriptOutput Write(this IScriptOutput scriptOutput, string text)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		return scriptOutput.Write(text, 0, text.Length);
	}
}
