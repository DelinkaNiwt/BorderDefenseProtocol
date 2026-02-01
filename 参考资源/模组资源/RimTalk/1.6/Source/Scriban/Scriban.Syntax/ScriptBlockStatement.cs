using System.Collections.Generic;

namespace Scriban.Syntax;

[ScriptSyntax("block statement", "<statement>...end")]
public sealed class ScriptBlockStatement : ScriptStatement
{
	public List<ScriptStatement> Statements { get; private set; }

	public ScriptBlockStatement()
	{
		Statements = new List<ScriptStatement>();
	}

	public override object Evaluate(TemplateContext context)
	{
		object obj = null;
		for (int i = 0; i < Statements.Count; i++)
		{
			ScriptStatement scriptStatement = Statements[i];
			bool num = (scriptStatement as ScriptExpressionStatement)?.Expression is ScriptAssignExpression;
			obj = context.Evaluate(scriptStatement);
			if (num)
			{
				obj = null;
			}
			else if (obj != null && context.FlowState != ScriptFlowState.Return && context.EnableOutput)
			{
				context.Write(Span, obj);
				obj = null;
			}
			if (context.FlowState != ScriptFlowState.None)
			{
				break;
			}
		}
		return obj;
	}

	public override void Write(TemplateRewriterContext context)
	{
		foreach (ScriptStatement statement in Statements)
		{
			context.Write(statement);
		}
	}

	public override bool CanHaveLeadingTrivia()
	{
		return false;
	}

	public override string ToString()
	{
		return $"<statements[{Statements.Count}]>";
	}
}
