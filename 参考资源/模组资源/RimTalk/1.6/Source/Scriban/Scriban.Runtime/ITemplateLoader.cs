using Scriban.Parsing;

namespace Scriban.Runtime;

public interface ITemplateLoader
{
	string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName);

	string Load(TemplateContext context, SourceSpan callerSpan, string templatePath);
}
