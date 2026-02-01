namespace Scriban.Syntax;

public class ScriptVariableLocal : ScriptVariable
{
	public ScriptVariableLocal(string name)
		: base(name, ScriptVariableScope.Local)
	{
	}
}
