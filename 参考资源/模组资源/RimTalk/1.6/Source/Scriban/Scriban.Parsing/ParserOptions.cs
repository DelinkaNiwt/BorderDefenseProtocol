namespace Scriban.Parsing;

public struct ParserOptions
{
	public int? ExpressionDepthLimit { get; set; }

	public bool LiquidFunctionsToScriban { get; set; }
}
