namespace Scriban.Syntax;

[ScriptSyntax("else statement", "else | else if <expression> ... end|else|else if")]
public class ScriptElseStatement : ScriptConditionStatement
{
	public ScriptBlockStatement Body { get; set; }

	public ScriptConditionStatement Else { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		if (Body == null)
		{
			return context.Evaluate(Else);
		}
		return context.Evaluate(Body);
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("else").ExpectEos();
		context.Write(Body);
		context.Write(Else);
	}
}
