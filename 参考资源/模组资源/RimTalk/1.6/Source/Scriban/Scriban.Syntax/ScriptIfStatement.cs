namespace Scriban.Syntax;

[ScriptSyntax("if statement", "if <expression> ... end|else|else if")]
public class ScriptIfStatement : ScriptConditionStatement
{
	public ScriptExpression Condition { get; set; }

	public bool InvertCondition { get; set; }

	public ScriptBlockStatement Then { get; set; }

	public ScriptConditionStatement Else { get; set; }

	public bool IsElseIf { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		bool flag = context.ToBool(Condition.Span, context.Evaluate(Condition));
		if (InvertCondition)
		{
			flag = !flag;
		}
		if (!flag)
		{
			return context.Evaluate(Else);
		}
		return context.Evaluate(Then);
	}

	public override void Write(TemplateRewriterContext context)
	{
		if (IsElseIf)
		{
			context.Write("else ");
		}
		context.Write("if").ExpectSpace();
		if (InvertCondition)
		{
			context.Write("!(");
		}
		context.Write(Condition);
		if (InvertCondition)
		{
			context.Write(")");
		}
		context.ExpectEos();
		context.Write(Then);
		context.Write(Else);
		if (!IsElseIf)
		{
			context.ExpectEnd();
		}
	}

	public override string ToString()
	{
		return $"if {Condition}";
	}
}
