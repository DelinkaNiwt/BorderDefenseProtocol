using System.Collections.Generic;
using Scriban.Parsing;

namespace Scriban.Runtime.Accessors;

internal class GenericDictionaryAccessor<TKey, TValue> : IObjectAccessor
{
	public int GetMemberCount(TemplateContext context, SourceSpan span, object target)
	{
		return ((IDictionary<TKey, TValue>)target).Count;
	}

	public IEnumerable<string> GetMembers(TemplateContext context, SourceSpan span, object target)
	{
		foreach (TKey key in ((IDictionary<TKey, TValue>)target).Keys)
		{
			yield return context.ToString(span, key);
		}
	}

	public bool HasMember(TemplateContext context, SourceSpan span, object value, string member)
	{
		return ((IDictionary<TKey, TValue>)value).ContainsKey(TransformToKey(context, member));
	}

	public bool TryGetValue(TemplateContext context, SourceSpan span, object target, string member, out object value)
	{
		TValue value2;
		bool result = ((IDictionary<TKey, TValue>)target).TryGetValue(TransformToKey(context, member), out value2);
		value = value2;
		return result;
	}

	public bool TrySetValue(TemplateContext context, SourceSpan span, object target, string member, object value)
	{
		((IDictionary<TKey, TValue>)value)[TransformToKey(context, member)] = (TValue)value;
		return true;
	}

	private TKey TransformToKey(TemplateContext context, string member)
	{
		return (TKey)context.ToObject(default(SourceSpan), member, typeof(TKey));
	}
}
