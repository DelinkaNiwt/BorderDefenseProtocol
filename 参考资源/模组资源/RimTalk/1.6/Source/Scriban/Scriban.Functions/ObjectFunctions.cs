using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Scriban.Helpers;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Scriban.Functions;

public class ObjectFunctions : ScriptObject
{
	public static object Default(object value, object @default)
	{
		if (value != null && (!(value is string) || !string.IsNullOrEmpty((string)value)))
		{
			return value;
		}
		return @default;
	}

	public static string Format(TemplateContext context, SourceSpan span, object value, string format, string culture = null)
	{
		if (value == null)
		{
			return string.Empty;
		}
		format = format ?? string.Empty;
		return ((value as IFormattable) ?? throw new ScriptRuntimeException(span, $"Unexpected `{value}`. Must be a formattable object")).ToString(format, (culture != null) ? new CultureInfo(culture) : context.CurrentCulture);
	}

	public static bool HasKey(IDictionary<string, object> value, string key)
	{
		if (value == null || key == null)
		{
			return false;
		}
		return value.ContainsKey(key);
	}

	public static bool HasValue(IDictionary<string, object> value, string key)
	{
		if (value == null || key == null)
		{
			return false;
		}
		if (value.ContainsKey(key))
		{
			return value[key] != null;
		}
		return false;
	}

	public new static ScriptArray Keys(IDictionary<string, object> value)
	{
		if (value != null)
		{
			return new ScriptArray(value.Keys);
		}
		return new ScriptArray();
	}

	public static int Size(TemplateContext context, SourceSpan span, object value)
	{
		if (value is string)
		{
			return StringFunctions.Size((string)value);
		}
		if (value is IEnumerable)
		{
			return ArrayFunctions.Size((IEnumerable)value);
		}
		return 0;
	}

	public static string Typeof(object value)
	{
		if (value == null)
		{
			return null;
		}
		Type type = value.GetType();
		TypeInfo typeInfo = type.GetTypeInfo();
		if (type == typeof(string))
		{
			return "string";
		}
		if (type == typeof(bool))
		{
			return "boolean";
		}
		if (type.IsPrimitiveOrDecimal())
		{
			return "number";
		}
		if (typeof(IList).GetTypeInfo().IsAssignableFrom(typeInfo))
		{
			return "array";
		}
		if (!typeof(ScriptObject).GetTypeInfo().IsAssignableFrom(typeInfo) && !typeof(IDictionary).GetTypeInfo().IsAssignableFrom(typeInfo) && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(typeInfo))
		{
			return "iterator";
		}
		return "object";
	}

	public new static ScriptArray Values(IDictionary<string, object> value)
	{
		if (value != null)
		{
			return new ScriptArray(value.Values);
		}
		return new ScriptArray();
	}
}
