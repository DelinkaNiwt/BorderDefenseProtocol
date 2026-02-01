namespace Scriban.Syntax;

[ScriptSyntax("raw statement", "<raw_text>")]
public class ScriptRawStatement : ScriptStatement
{
	public string Text { get; set; }

	public int EscapeCount { get; set; }

	public override object Evaluate(TemplateContext context)
	{
		if (Text == null)
		{
			return null;
		}
		int num = Span.End.Offset - Span.Start.Offset + 1;
		if (num > 0)
		{
			if (!context.EnableOutput)
			{
				return Text.Substring(Span.Start.Offset, num);
			}
			context.Write(Text, Span.Start.Offset, num);
		}
		return null;
	}

	public override void Write(TemplateRewriterContext context)
	{
		if (Text != null)
		{
			if (EscapeCount > 0)
			{
				context.WriteEnterCode(EscapeCount);
			}
			int num = Span.End.Offset - Span.Start.Offset + 1;
			if (num > 0)
			{
				context.Write(Text.Substring(Span.Start.Offset, num));
			}
			if (EscapeCount > 0)
			{
				context.WriteExitCode(EscapeCount);
			}
		}
	}

	public override string ToString()
	{
		int length = Span.End.Offset - Span.Start.Offset + 1;
		return Text?.Substring(Span.Start.Offset, length) ?? string.Empty;
	}
}
