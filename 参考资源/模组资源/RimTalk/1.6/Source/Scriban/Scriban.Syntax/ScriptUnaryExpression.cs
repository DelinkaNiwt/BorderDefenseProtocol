namespace Scriban.Syntax;

[ScriptSyntax("unary expression", "<operator> <expression>")]
public class ScriptUnaryExpression : ScriptExpression
{
	public ScriptUnaryOperator Operator { get; set; }

	public ScriptExpression Right { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		switch (Operator)
		{
		case ScriptUnaryOperator.Not:
		{
			object value = context.Evaluate(Right);
			return !context.ToBool(Right.Span, value);
		}
		case ScriptUnaryOperator.Negate:
		case ScriptUnaryOperator.Plus:
		{
			object obj = context.Evaluate(Right);
			bool flag = Operator == ScriptUnaryOperator.Negate;
			if (obj == null)
			{
				break;
			}
			if (obj is int)
			{
				if (!flag)
				{
					return obj;
				}
				return -(int)obj;
			}
			if (obj is double)
			{
				if (!flag)
				{
					return obj;
				}
				return 0.0 - (double)obj;
			}
			if (obj is float)
			{
				if (!flag)
				{
					return obj;
				}
				return 0f - (float)obj;
			}
			if (obj is long)
			{
				if (!flag)
				{
					return obj;
				}
				return -(long)obj;
			}
			if (obj is decimal)
			{
				if (!flag)
				{
					return obj;
				}
				return -(decimal)obj;
			}
			throw new ScriptRuntimeException(Span, $"Unexpected value `{obj} / Type: {obj?.GetType()}`. Cannot negate(-)/positive(+) a non-numeric value");
		}
		case ScriptUnaryOperator.FunctionAlias:
			return context.Evaluate(Right, aliasReturnedFunction: true);
		case ScriptUnaryOperator.FunctionParametersExpand:
			return context.Evaluate(Right);
		}
		throw new ScriptRuntimeException(Span, $"Operator `{Operator}` is not supported");
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write(Operator.ToText());
		context.Write(Right);
	}

	public override string ToString()
	{
		return $"{Operator}{Right}";
	}
}
