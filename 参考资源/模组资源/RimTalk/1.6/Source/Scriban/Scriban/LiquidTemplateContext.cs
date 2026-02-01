using Scriban.Functions;
using Scriban.Parsing;

namespace Scriban;

public class LiquidTemplateContext : TemplateContext
{
	public LiquidTemplateContext()
		: base(new LiquidBuiltinsFunctions())
	{
		base.EnableBreakAndContinueAsReturnOutsideLoop = true;
		base.EnableRelaxedMemberAccess = true;
		base.TemplateLoaderLexerOptions = new LexerOptions
		{
			Mode = ScriptMode.Liquid
		};
		base.TemplateLoaderParserOptions = new ParserOptions
		{
			LiquidFunctionsToScriban = true
		};
		base.IsLiquid = true;
	}
}
