using System.Collections.Generic;
using System.Diagnostics;
using Scriban.Parsing;
using Scriban.Syntax;

namespace Scriban.Runtime;

[DebuggerDisplay("<empty object>")]
public sealed class EmptyScriptObject : IScriptObject
{
	public static readonly EmptyScriptObject Default = new EmptyScriptObject();

	public int Count => 0;

	public bool IsReadOnly
	{
		get
		{
			return true;
		}
		set
		{
		}
	}

	private EmptyScriptObject()
	{
	}

	public IEnumerable<string> GetMembers()
	{
		yield break;
	}

	public bool Contains(string member)
	{
		return false;
	}

	public bool TryGetValue(TemplateContext context, SourceSpan span, string member, out object value)
	{
		value = null;
		return false;
	}

	public bool CanWrite(string member)
	{
		return false;
	}

	public void SetValue(TemplateContext context, SourceSpan span, string member, object value, bool readOnly)
	{
		throw new ScriptRuntimeException(span, "Cannot set a property on the empty object");
	}

	public bool Remove(string member)
	{
		return false;
	}

	public void SetReadOnly(string member, bool readOnly)
	{
	}

	public IScriptObject Clone(bool deep)
	{
		return this;
	}

	public override string ToString()
	{
		return string.Empty;
	}
}
