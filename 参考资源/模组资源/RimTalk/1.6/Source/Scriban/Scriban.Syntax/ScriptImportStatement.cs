using Scriban.Runtime;

namespace Scriban.Syntax;

[ScriptSyntax("import statement", "import <expression>")]
public class ScriptImportStatement : ScriptStatement
{
	public ScriptExpression Expression { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		object obj = context.Evaluate(Expression);
		if (obj == null)
		{
			return null;
		}
		if (!(obj is ScriptObject other))
		{
			throw new ScriptRuntimeException(Expression.Span, $"Unexpected value `{obj.GetType()}` for import. Expecting an plain script object {{}}");
		}
		context.CurrentGlobal.Import(other);
		return null;
	}

	public override void Write(TemplateRewriterContext context)
	{
		context.Write("import").ExpectSpace();
		context.Write(Expression);
		context.ExpectEos();
	}
}
