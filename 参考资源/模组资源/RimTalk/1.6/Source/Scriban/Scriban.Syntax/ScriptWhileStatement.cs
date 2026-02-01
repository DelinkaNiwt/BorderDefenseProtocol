namespace Scriban.Syntax;

[ScriptSyntax("while statement", "while <expression> ... end")]
public class ScriptWhileStatement : ScriptLoopStatementBase
{
	public ScriptExpression Condition { get; set; }

	protected override object LoopItem(TemplateContext context, LoopState state)
	{
		return context.Evaluate(base.Body);
	}

	protected override object EvaluateImpl(TemplateContext context)
	{
		int localIndex = 0;
		object result = null;
		BeforeLoop(context);
		LoopState loopState = CreateLoopState();
		context.SetValue(ScriptVariable.WhileObject, loopState);
		while (context.StepLoop(this) && context.ToBool(Condition.Span, context.Evaluate(Condition)))
		{
			loopState.Index = localIndex++;
			loopState.LocalIndex = localIndex;
			loopState.IsLast = false;
			result = LoopItem(context, loopState);
			if (!ContinueLoop(context))
			{
				break;
			}
		}
		AfterLoop(context);
		return result;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("while").ExpectSpace();
		context.Write(Condition);
		context.ExpectEos();
		context.Write(base.Body);
		context.ExpectEnd();
	}

	public override string ToString()
	{
		return $"while {Condition} ... end";
	}
}
