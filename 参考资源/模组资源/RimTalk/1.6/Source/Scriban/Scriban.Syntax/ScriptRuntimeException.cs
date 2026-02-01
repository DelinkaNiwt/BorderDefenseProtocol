using System;
using Scriban.Parsing;

namespace Scriban.Syntax;

public class ScriptRuntimeException : Exception
{
	public SourceSpan Span { get; }

	public override string Message => new LogMessage(ParserMessageType.Error, Span, base.Message).ToString();

	public string OriginalMessage => base.Message;

	public ScriptRuntimeException(SourceSpan span, string message)
		: base(message)
	{
		Span = span;
	}

	public ScriptRuntimeException(SourceSpan span, string message, Exception innerException)
		: base(message, innerException)
	{
		Span = span;
	}

	public override string ToString()
	{
		return Message;
	}
}
