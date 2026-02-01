using System.Collections.Generic;

namespace Scriban.Syntax;

public class ScriptTrivias
{
	public List<ScriptTrivia> Before { get; }

	public List<ScriptTrivia> After { get; }

	public ScriptTrivias()
	{
		Before = new List<ScriptTrivia>();
		After = new List<ScriptTrivia>();
	}
}
