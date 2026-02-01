using System;
using System.Collections.Generic;
using System.Reflection;
using Scriban.Helpers;
using Scriban.Parsing;

namespace Scriban.Runtime.Accessors;

public class TypedObjectAccessor : IObjectAccessor
{
	private readonly MemberFilterDelegate _filter;

	private readonly Type _type;

	private readonly MemberRenamerDelegate _renamer;

	private readonly Dictionary<string, MemberInfo> _members;

	public TypedObjectAccessor(Type targetType, MemberFilterDelegate filter, MemberRenamerDelegate renamer)
	{
		_type = targetType ?? throw new ArgumentNullException("targetType");
		_filter = filter;
		_renamer = renamer ?? StandardMemberRenamer.Default;
		_members = new Dictionary<string, MemberInfo>();
		PrepareMembers();
	}

	public int GetMemberCount(TemplateContext context, SourceSpan span, object target)
	{
		return _members.Count;
	}

	public IEnumerable<string> GetMembers(TemplateContext context, SourceSpan span, object target)
	{
		return _members.Keys;
	}

	public bool HasMember(TemplateContext context, SourceSpan span, object target, string member)
	{
		return _members.ContainsKey(member);
	}

	public bool TryGetValue(TemplateContext context, SourceSpan span, object target, string member, out object value)
	{
		value = null;
		if (_members.TryGetValue(member, out var value2))
		{
			FieldInfo fieldInfo = value2 as FieldInfo;
			if (fieldInfo != null)
			{
				value = fieldInfo.GetValue(target);
				return true;
			}
			PropertyInfo propertyInfo = (PropertyInfo)value2;
			value = propertyInfo.GetValue(target);
			return true;
		}
		return false;
	}

	public bool TrySetValue(TemplateContext context, SourceSpan span, object target, string member, object value)
	{
		if (_members.TryGetValue(member, out var value2))
		{
			FieldInfo fieldInfo = value2 as FieldInfo;
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(target, value);
			}
			else
			{
				((PropertyInfo)value2).SetValue(target, value);
			}
		}
		return true;
	}

	private void PrepareMembers()
	{
		TypeInfo typeInfo = _type.GetTypeInfo();
		while (typeInfo != null)
		{
			foreach (FieldInfo declaredField in typeInfo.GetDeclaredFields())
			{
				if (declaredField.GetCustomAttribute<ScriptMemberIgnoreAttribute>() == null && !declaredField.IsStatic && declaredField.IsPublic && (_filter == null || _filter(declaredField)))
				{
					string text = Rename(declaredField);
					if (string.IsNullOrEmpty(text))
					{
						text = declaredField.Name;
					}
					if (!_members.ContainsKey(text))
					{
						_members.Add(text, declaredField);
					}
				}
			}
			foreach (PropertyInfo declaredProperty in typeInfo.GetDeclaredProperties())
			{
				bool num = declaredProperty.GetCustomAttribute<ScriptMemberIgnoreAttribute>() == null;
				MethodInfo getMethod = declaredProperty.GetMethod;
				if (num && declaredProperty.CanRead && !getMethod.IsStatic && getMethod.IsPublic && (_filter == null || _filter(declaredProperty)))
				{
					string text2 = Rename(declaredProperty);
					if (string.IsNullOrEmpty(text2))
					{
						text2 = declaredProperty.Name;
					}
					if (!_members.ContainsKey(text2))
					{
						_members.Add(text2, declaredProperty);
					}
				}
			}
			if (!(typeInfo.BaseType == typeof(object)))
			{
				typeInfo = typeInfo.BaseType.GetTypeInfo();
				continue;
			}
			break;
		}
	}

	private string Rename(MemberInfo member)
	{
		return _renamer(member);
	}
}
