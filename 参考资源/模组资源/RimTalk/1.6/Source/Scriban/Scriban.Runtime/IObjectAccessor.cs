using System.Collections.Generic;
using Scriban.Parsing;

namespace Scriban.Runtime;

public interface IObjectAccessor
{
	int GetMemberCount(TemplateContext context, SourceSpan span, object target);

	IEnumerable<string> GetMembers(TemplateContext context, SourceSpan span, object target);

	bool HasMember(TemplateContext context, SourceSpan span, object target, string member);

	bool TryGetValue(TemplateContext context, SourceSpan span, object target, string member, out object value);

	bool TrySetValue(TemplateContext context, SourceSpan span, object target, string member, object value);
}
