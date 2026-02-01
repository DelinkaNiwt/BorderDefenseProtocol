using System.Collections.Generic;
using Scriban.Parsing;

namespace Scriban.Runtime;

public interface IScriptObject
{
	int Count { get; }

	bool IsReadOnly { get; set; }

	IEnumerable<string> GetMembers();

	bool Contains(string member);

	bool TryGetValue(TemplateContext context, SourceSpan span, string member, out object value);

	bool CanWrite(string member);

	void SetValue(TemplateContext context, SourceSpan span, string member, object value, bool readOnly);

	bool Remove(string member);

	void SetReadOnly(string member, bool readOnly);

	IScriptObject Clone(bool deep);
}
