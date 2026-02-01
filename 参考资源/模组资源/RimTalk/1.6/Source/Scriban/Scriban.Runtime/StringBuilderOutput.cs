using System;
using System.Text;

namespace Scriban.Runtime;

public class StringBuilderOutput : IScriptOutput
{
	[ThreadStatic]
	private static StringBuilder TlsBuilder;

	public StringBuilder Builder { get; }

	public StringBuilderOutput()
		: this(new StringBuilder())
	{
	}

	public StringBuilderOutput(StringBuilder builder)
	{
		Builder = builder ?? throw new ArgumentNullException("builder");
	}

	public IScriptOutput Write(string text, int offset, int count)
	{
		Builder.Append(text, offset, count);
		return this;
	}

	public static StringBuilderOutput GetThreadInstance()
	{
		if (TlsBuilder == null)
		{
			TlsBuilder = new StringBuilder();
		}
		TlsBuilder.Length = 0;
		return new StringBuilderOutput(TlsBuilder);
	}

	public override string ToString()
	{
		return Builder.ToString();
	}
}
