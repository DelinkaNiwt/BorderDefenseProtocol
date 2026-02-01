namespace Scriban.Syntax;

[ScriptSyntax("expression statement", "<expression>")]
public class ScriptExpressionStatement : ScriptStatement
{
	public ScriptExpression Expression { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		object obj = context.Evaluate(Expression);
		if (obj is ScriptNode scriptNode)
		{
			return context.Evaluate(scriptNode);
		}
		return obj;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(Expression);
		context.ExpectEos();
	}

	public override string ToString()
	{
		return Expression?.ToString();
	}
}
