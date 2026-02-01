using System.Collections.Generic;

namespace Scriban.Syntax;

public interface IScriptNamedArgumentContainer
{
	List<ScriptNamedArgument> NamedArguments { get; set; }
}
