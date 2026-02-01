using System.Collections;
using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("pipe expression", "<expression> | <expression>")]
public class ScriptPipeCall : ScriptExpression
{
	public ScriptExpression From { get; set; }

	public ScriptExpression To { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		object obj = context.Evaluate(From);
		context.PushPipeArguments();
		try
		{
			if (From is ScriptUnaryExpression { Operator: ScriptUnaryOperator.FunctionParametersExpand })
			{
				if (obj is IEnumerable enumerable)
				{
					ScriptPipeArguments pipeArguments = context.PipeArguments;
					foreach (object item in enumerable)
					{
						pipeArguments.Add(item);
					}
				}
				else
				{
					context.PipeArguments.Add(obj);
				}
			}
			else
			{
				context.PipeArguments.Add(obj);
			}
			object result = context.Evaluate(To);
			if (context.PipeArguments.Count > 0)
			{
				throw new ScriptRuntimeException(To.Span, $"Pipe expression destination `{To}` is not a valid function ");
			}
			return result;
		}
		finally
		{
			context.PopPipeArguments();
		}
	}

	public override bool CanHaveLeadingTrivia()
	{
		return false;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(From);
		context.Write("|");
		context.Write(To);
	}

	public override string ToString()
	{
		return $"{From} | {To}";
	}
}
