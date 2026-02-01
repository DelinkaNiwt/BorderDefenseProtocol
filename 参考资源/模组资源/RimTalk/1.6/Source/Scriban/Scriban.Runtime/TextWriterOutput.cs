using System;
using System.IO;

namespace Scriban.Runtime;

public class TextWriterOutput : IScriptOutput
{
	public TextWriter Writer { get; }

	public TextWriterOutput()
		: this(new StringWriter())
	{
	}

	public TextWriterOutput(TextWriter writer)
	{
		Writer = writer ?? throw new ArgumentNullException("writer");
	}

	public IScriptOutput Write(string text, int offset, int count)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		Writer.Write(text.Substring(offset, count));
		return this;
	}

	public override string ToString()
	{
		return Writer.ToString();
	}
}
