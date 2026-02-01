namespace Scriban.Syntax;

public class ScriptVariableLoop : ScriptVariable
{
	public ScriptVariableLoop(string name)
		: base(name, ScriptVariableScope.Loop)
	{
	}

	public override void Write(TemplateRewriterContext context)
	{
		if (context.IsInWhileLoop)
		{
			context.Write(ToString().Replace("for", "while"));
		}
		else
		{
			base.Write(context);
		}
	}
}
