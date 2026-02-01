namespace Scriban.Syntax;

[ScriptSyntax("continue statement", "continue")]
public class ScriptContinueStatement : ScriptStatement
{
	public override object Evaluate(TemplateContext context)
	{
		if (context.IsInLoop)
		{
			context.FlowState = ScriptFlowState.Continue;
		}
		else
		{
			if (!context.EnableBreakAndContinueAsReturnOutsideLoop)
			{
				throw new ScriptRuntimeException(Span, "The <continue> statement can only be used inside for/while loops");
			}
			context.FlowState = ScriptFlowState.Return;
		}
		return null;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("continue").ExpectEos();
	}
}
