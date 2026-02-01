namespace Scriban.Parsing;

public struct LexerOptions
{
	public const string DefaultFrontMatterMarker = "+++";

	public static readonly LexerOptions Default = new LexerOptions
	{
		FrontMatterMarker = "+++"
	};

	public ScriptMode Mode { get; set; }

	public string FrontMatterMarker { get; set; }

	public bool EnableIncludeImplicitString { get; set; }

	public TextPosition StartPosition { get; set; }

	public bool KeepTrivia { get; set; }
}
