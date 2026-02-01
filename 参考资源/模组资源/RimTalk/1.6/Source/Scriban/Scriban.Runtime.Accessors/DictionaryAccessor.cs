using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Scriban.Helpers;
using Scriban.Parsing;

namespace Scriban.Runtime.Accessors;

public sealed class DictionaryAccessor : IObjectAccessor
{
	public static readonly DictionaryAccessor Default = new DictionaryAccessor();

	private DictionaryAccessor()
	{
	}

	public static bool TryGet(object target, out IObjectAccessor accessor)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if (target is IDictionary<string, object>)
		{
			accessor = DictionaryStringObjectAccessor.Default;
			return true;
		}
		if (target is IDictionary)
		{
			accessor = Default;
			return true;
		}
		Type baseOrInterface = target.GetType().GetBaseOrInterface(typeof(IDictionary<, >));
		accessor = null;
		if (baseOrInterface == null)
		{
			return false;
		}
		Type type = baseOrInterface.GetTypeInfo().GetGenericArguments()[0];
		Type type2 = baseOrInterface.GetTypeInfo().GetGenericArguments()[1];
		Type type3 = typeof(GenericDictionaryAccessor<, >).GetTypeInfo().MakeGenericType(type, type2);
		accessor = (IObjectAccessor)Activator.CreateInstance(type3);
		return true;
	}

	public int GetMemberCount(TemplateContext context, SourceSpan span, object target)
	{
		return ((IDictionary)target).Count;
	}

	public IEnumerable<string> GetMembers(TemplateContext context, SourceSpan span, object target)
	{
		foreach (object key in ((IDictionary)target).Keys)
		{
			yield return context.ToString(span, key);
		}
	}

	public bool HasMember(TemplateContext context, SourceSpan span, object target, string member)
	{
		return ((IDictionary)target).Contains(member);
	}

	public bool TryGetValue(TemplateContext context, SourceSpan span, object target, string member, out object value)
	{
		value = null;
		if (((IDictionary)target).Contains(member))
		{
			value = ((IDictionary)target)[member];
			return true;
		}
		return false;
	}

	public bool TrySetValue(TemplateContext context, SourceSpan span, object target, string member, object value)
	{
		((IDictionary)target)[member] = value;
		return true;
	}
}
