using System;
using System.Collections.Generic;
using Scriban.Helpers;
using Scriban.Parsing;

namespace Scriban.Syntax;

public class ScriptParserRuntimeException : ScriptRuntimeException
{
	public List<LogMessage> ParserMessages { get; }

	public override string Message
	{
		get
		{
			string text = StringHelper.Join("\n", ParserMessages);
			return base.Message + " Parser messages:\n " + text;
		}
	}

	public ScriptParserRuntimeException(SourceSpan span, string message, List<LogMessage> parserMessages)
		: this(span, message, parserMessages, null)
	{
	}

	public ScriptParserRuntimeException(SourceSpan span, string message, List<LogMessage> parserMessages, Exception innerException)
		: base(span, message, innerException)
	{
		if (parserMessages == null)
		{
			throw new ArgumentNullException("parserMessages");
		}
		ParserMessages = parserMessages;
	}
}
