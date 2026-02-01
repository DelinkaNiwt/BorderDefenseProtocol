namespace Scriban.Syntax;

[ScriptSyntax("break statement", "break")]
public class ScriptBreakStatement : ScriptStatement
{
	public override object Evaluate(TemplateContext context)
	{
		if (context.IsInLoop)
		{
			context.FlowState = ScriptFlowState.Break;
		}
		else
		{
			if (!context.EnableBreakAndContinueAsReturnOutsideLoop)
			{
				throw new ScriptRuntimeException(Span, "The <break> statement can only be used inside for/while loops");
			}
			context.FlowState = ScriptFlowState.Return;
		}
		return null;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("break").ExpectEos();
	}
}
