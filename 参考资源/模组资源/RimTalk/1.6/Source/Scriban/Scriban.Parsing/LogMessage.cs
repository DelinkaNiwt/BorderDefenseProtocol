using System.Text;

namespace Scriban.Parsing;

public class LogMessage
{
	public ParserMessageType Type { get; set; }

	public SourceSpan Span { get; set; }

	public string Message { get; set; }

	public LogMessage(ParserMessageType type, SourceSpan span, string message)
	{
		Type = type;
		Span = span;
		Message = message;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Span.ToStringSimple());
		stringBuilder.Append(" : ");
		stringBuilder.Append(Type.ToString().ToLowerInvariant());
		stringBuilder.Append(" : ");
		if (Message != null)
		{
			stringBuilder.Append(Message);
		}
		return stringBuilder.ToString();
	}
}
