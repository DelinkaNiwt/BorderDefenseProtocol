namespace Scriban.Parsing;

public struct SourceSpan
{
	public string FileName { get; set; }

	public TextPosition Start { get; set; }

	public TextPosition End { get; set; }

	public SourceSpan(string fileName, TextPosition start, TextPosition end)
	{
		FileName = fileName;
		Start = start;
		End = end;
	}

	public override string ToString()
	{
		return $"{FileName}({Start})-({End})";
	}

	public string ToStringSimple()
	{
		return FileName + "(" + Start.ToStringSimple() + ")";
	}
}
