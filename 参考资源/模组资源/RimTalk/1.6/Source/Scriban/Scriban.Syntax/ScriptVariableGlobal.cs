namespace Scriban.Syntax;

public class ScriptVariableGlobal : ScriptVariable
{
	public ScriptVariableGlobal(string name)
		: base(name, ScriptVariableScope.Global)
	{
	}

	public override object GetValue(TemplateContext context)
	{
		return context.GetValue(this);
	}
}
