using System.Collections.Generic;
using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("object initializer expression", "{ member1: <expression>, member2: ... }")]
public class ScriptObjectInitializerExpression : ScriptExpression
{
	public Dictionary<ScriptExpression, ScriptExpression> Members { get; private set; }

	public ScriptObjectInitializerExpression()
	{
		Members = new Dictionary<ScriptExpression, ScriptExpression>();
	}

	public override object Evaluate(TemplateContext context)
	{
		ScriptObject scriptObject = new ScriptObject();
		foreach (KeyValuePair<ScriptExpression, ScriptExpression> member2 in Members)
		{
			ScriptVariable obj = member2.Key as ScriptVariable;
			ScriptLiteral scriptLiteral = member2.Key as ScriptLiteral;
			string member = obj?.Name ?? scriptLiteral?.Value?.ToString();
			scriptObject.SetValue(context, Span, member, context.Evaluate(member2.Value), readOnly: false);
		}
		return scriptObject;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("{");
		bool flag = false;
		foreach (KeyValuePair<ScriptExpression, ScriptExpression> member in Members)
		{
			if (flag)
			{
				context.Write(",");
			}
			context.Write(member.Key);
			context.Write(":");
			context.Write(member.Value);
			flag = !member.Value.HasTrivia(ScriptTriviaType.Comma, before: false);
		}
		context.Write("}");
	}

	public override string ToString()
	{
		return "{...}";
	}
}
