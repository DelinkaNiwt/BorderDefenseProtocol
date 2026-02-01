using System.Collections.Generic;

namespace Scriban.Syntax;

public static class ScriptNodeExtensions
{
	public static void AddTrivia(this ScriptNode node, ScriptTrivia trivia, bool before)
	{
		ScriptTrivias scriptTrivias = node.Trivias;
		if (scriptTrivias == null)
		{
			scriptTrivias = (node.Trivias = new ScriptTrivias());
		}
		(before ? scriptTrivias.Before : scriptTrivias.After).Add(trivia);
	}

	public static void AddTrivias<T>(this ScriptNode node, T trivias, bool before) where T : IEnumerable<ScriptTrivia>
	{
		foreach (ScriptTrivia item in trivias)
		{
			node.AddTrivia(item, before);
		}
	}

	public static bool HasTrivia(this ScriptNode node, ScriptTriviaType triviaType, bool before)
	{
		if (node.Trivias == null)
		{
			return false;
		}
		foreach (ScriptTrivia item in before ? node.Trivias.Before : node.Trivias.After)
		{
			if (item.Type == triviaType)
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasTriviaEndOfStatement(this ScriptNode node, bool before)
	{
		if (node.Trivias == null)
		{
			return false;
		}
		foreach (ScriptTrivia item in before ? node.Trivias.Before : node.Trivias.After)
		{
			if (item.Type == ScriptTriviaType.NewLine || item.Type == ScriptTriviaType.SemiColon)
			{
				return true;
			}
		}
		return false;
	}
}
