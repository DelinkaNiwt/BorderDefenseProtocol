using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("capture statement", "capture <variable> ... end")]
public class ScriptCaptureStatement : ScriptStatement
{
	public ScriptExpression Target { get; set; }

	public ScriptBlockStatement Body { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		context.PushOutput();
		try
		{
			context.Evaluate(Body);
		}
		finally
		{
			IScriptOutput value = context.PopOutput();
			context.SetValue(Target, value);
		}
		return null;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("capture").ExpectSpace();
		context.Write(Target);
		context.ExpectEos();
		context.Write(Body);
		context.ExpectEnd();
	}
}
