using Scriban.Parsing;

namespace Scriban.Syntax;

public struct ScriptTrivia
{
	public readonly SourceSpan Span;

	public readonly ScriptTriviaType Type;

	public readonly string Text;

	public ScriptTrivia(SourceSpan span, ScriptTriviaType type)
	{
		Span = span;
		Type = type;
		Text = null;
	}

	public ScriptTrivia(SourceSpan span, ScriptTriviaType type, string text)
	{
		Span = span;
		Type = type;
		Text = text;
	}

	public void Write(TemplateRewriterContext context)
	{
		string text = ToString();
		int num;
		if (Type == ScriptTriviaType.CommentMulti)
		{
			num = ((!text.StartsWith("##")) ? 1 : 0);
			if (num != 0)
			{
				text = text.Replace("#", "\\#");
				text = text.Replace("}", "\\}");
				context.Write("## ");
			}
		}
		else
		{
			num = 0;
		}
		context.Write(text);
		if (num != 0)
		{
			context.Write(" ##");
		}
	}

	public override string ToString()
	{
		switch (Type)
		{
		case ScriptTriviaType.Empty:
			return string.Empty;
		case ScriptTriviaType.End:
			return "end";
		case ScriptTriviaType.Comma:
			return ",";
		case ScriptTriviaType.SemiColon:
			return ";";
		default:
		{
			int length = Span.End.Offset - Span.Start.Offset + 1;
			return Text?.Substring(Span.Start.Offset, length);
		}
		}
	}
}
