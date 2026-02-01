using Scriban.Parsing;

namespace Scriban.Syntax;

public abstract class ScriptNode
{
	public SourceSpan Span;

	public ScriptTrivias Trivias { get; set; }

	public abstract object Evaluate(TemplateContext context);

	public virtual bool CanHaveLeadingTrivia()
	{
		return true;
	}

	public abstract void Write(TemplateRewriterContext context);
}
