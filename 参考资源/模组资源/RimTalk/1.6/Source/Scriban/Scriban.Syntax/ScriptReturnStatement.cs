namespace Scriban.Syntax;

[ScriptSyntax("return statement", "return <expression>?")]
public class ScriptReturnStatement : ScriptStatement
{
	public ScriptExpression Expression { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		object result = context.Evaluate(Expression);
		context.FlowState = ScriptFlowState.Return;
		return result;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("ret").ExpectSpace();
		context.Write(Expression);
		context.ExpectEos();
	}
}
