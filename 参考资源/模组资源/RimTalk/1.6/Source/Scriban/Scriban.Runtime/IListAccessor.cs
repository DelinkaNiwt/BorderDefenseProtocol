using Scriban.Parsing;

namespace Scriban.Runtime;

public interface IListAccessor
{
	int GetLength(TemplateContext context, SourceSpan span, object target);

	object GetValue(TemplateContext context, SourceSpan span, object target, int index);

	void SetValue(TemplateContext context, SourceSpan span, object target, int index, object value);
}
