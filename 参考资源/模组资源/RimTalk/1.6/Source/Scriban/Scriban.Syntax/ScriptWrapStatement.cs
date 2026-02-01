using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("wrap statement", "wrap <function_call> ... end")]
public class ScriptWrapStatement : ScriptStatement
{
	public ScriptExpression Target { get; set; }

	public ScriptBlockStatement Body { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		if (!(Target is ScriptFunctionCall scriptNode))
		{
			object obj = context.Evaluate(Target, aliasReturnedFunction: true);
			if (!(obj is IScriptCustomFunction) && !(obj is ScriptFunction))
			{
				ScriptSyntaxAttribute scriptSyntaxAttribute = ScriptSyntaxAttribute.Get(Target);
				throw new ScriptRuntimeException(Target.Span, $"Expecting a direct function instead of the expression `{Target}/{scriptSyntaxAttribute.Name}`");
			}
			context.BlockDelegates.Push(Body);
			return ScriptFunctionCall.Call(context, this, obj, processPipeArguments: false);
		}
		context.BlockDelegates.Push(Body);
		return context.Evaluate(scriptNode);
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("wrap").ExpectSpace();
		context.Write(Target);
		context.ExpectEos();
		context.Write(Body);
		context.ExpectEnd();
	}
}
