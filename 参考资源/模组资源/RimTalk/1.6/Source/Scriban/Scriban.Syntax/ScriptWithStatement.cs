using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("with statement", "with <variable> ... end")]
public class ScriptWithStatement : ScriptStatement
{
	public ScriptExpression Name { get; set; }

	public ScriptBlockStatement Body { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		object value = context.GetValue(Name);
		if (!(value is IScriptObject))
		{
			string arg = value?.GetType().Name ?? "null";
			throw new ScriptRuntimeException(Name.Span, $"Invalid target property `{Name}` used for [with] statement. Must be a ScriptObject instead of `{arg}`");
		}
		context.PushGlobal((IScriptObject)value);
		try
		{
			return context.Evaluate(Body);
		}
		finally
		{
			context.PopGlobal();
		}
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("with").ExpectSpace();
		context.Write(Name);
		context.ExpectEos();
		context.Write(Body);
		context.ExpectEnd();
	}

	public override string ToString()
	{
		return $"with {Name} <...> end";
	}
}
