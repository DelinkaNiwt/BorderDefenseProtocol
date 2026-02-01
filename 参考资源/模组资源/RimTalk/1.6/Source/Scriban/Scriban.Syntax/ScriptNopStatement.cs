namespace Scriban.Syntax;

public class ScriptNopStatement : ScriptStatement
{
	public override object Evaluate(TemplateContext context)
	{
		return null;
	}

	public override void Write(TemplateRewriterContext context)
	{
	}
}
