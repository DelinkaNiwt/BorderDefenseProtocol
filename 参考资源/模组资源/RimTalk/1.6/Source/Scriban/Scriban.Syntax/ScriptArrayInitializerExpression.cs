using System.Collections.Generic;
using Scriban.Helpers;
using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("array initializer", "[item1, item2,...]")]
public class ScriptArrayInitializerExpression : ScriptExpression
{
	public List<ScriptExpression> Values { get; private set; }

	public ScriptArrayInitializerExpression()
	{
		Values = new List<ScriptExpression>();
	}

	public override object Evaluate(TemplateContext context)
	{
		ScriptArray scriptArray = new ScriptArray();
		foreach (ScriptExpression value in Values)
		{
			object item = context.Evaluate(value);
			scriptArray.Add(item);
		}
		return scriptArray;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("[");
		context.WriteListWithCommas(Values);
		context.Write("]");
	}

	public override string ToString()
	{
		return "[" + StringHelper.Join(", ", Values) + "]";
	}
}
