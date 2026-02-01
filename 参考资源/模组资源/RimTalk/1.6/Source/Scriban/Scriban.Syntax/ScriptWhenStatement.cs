using System.Collections.Generic;
using System.Text;

namespace Scriban.Syntax;

[ScriptSyntax("when statement", "when <expression> ... end|when|else")]
public class ScriptWhenStatement : ScriptConditionStatement
{
	public List<ScriptExpression> Values { get; }

	public ScriptBlockStatement Body { get; set; }

	public ScriptConditionStatement Next { get; set; }

	public ScriptWhenStatement()
	{
		Values = new List<ScriptExpression>();
	}

	public override object Evaluate(TemplateContext context)
	{
		object leftValue = context.PeekCase();
		foreach (ScriptExpression value in Values)
		{
			object rightValue = context.Evaluate(value);
			object obj = ScriptBinaryExpression.Evaluate(context, Span, ScriptBinaryOperator.CompareEqual, leftValue, rightValue);
			if (obj is bool && (bool)obj)
			{
				return context.Evaluate(Body);
			}
		}
		return context.Evaluate(Next);
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("when").ExpectSpace();
		context.WriteListWithCommas(Values);
		context.ExpectEos();
		context.Write(Body);
		context.Write(Next);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("when ");
		for (int i = 0; i < Values.Count; i++)
		{
			ScriptExpression value = Values[i];
			if (i > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}
}
