using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Scriban.Helpers;
using Scriban.Parsing;

namespace Scriban.Runtime;

public static class ScriptObjectExtensions
{
	public static void AssertNotReadOnly(this IScriptObject scriptObject)
	{
		if (scriptObject.IsReadOnly)
		{
			throw new InvalidOperationException("The object is readonly");
		}
	}

	public static void Import(this IScriptObject script, object obj, MemberFilterDelegate filter = null, MemberRenamerDelegate renamer = null)
	{
		if (obj is IScriptObject)
		{
			script.Import((IScriptObject)obj);
		}
		else if (obj is IDictionary)
		{
			script.ImportDictionary((IDictionary)obj);
		}
		else
		{
			script.Import(obj, ScriptMemberImportFlags.All, filter, renamer);
		}
	}

	public static bool TryGetValue(this IScriptObject @this, string key, out object value)
	{
		return @this.TryGetValue(null, default(SourceSpan), key, out value);
	}

	public static bool TrySetValue(this IScriptObject @this, string member, object value, bool readOnly)
	{
		if (!@this.CanWrite(member))
		{
			return false;
		}
		@this.SetValue(null, default(SourceSpan), member, value, readOnly);
		return true;
	}

	public static void SetValue(this IScriptObject @this, string member, object value, bool readOnly)
	{
		@this.SetValue(null, default(SourceSpan), member, value, readOnly);
	}

	public static void Import(this IScriptObject @this, IScriptObject other)
	{
		if (other == null)
		{
			return;
		}
		ScriptObject scriptObject = @this.GetScriptObject();
		scriptObject.AssertNotReadOnly();
		foreach (KeyValuePair<string, ScriptObject.InternalValue> item in other.GetScriptObject().Store)
		{
			string key = item.Key;
			if (scriptObject.CanWrite(key))
			{
				scriptObject.Store[item.Key] = item.Value;
			}
		}
	}

	private static void ImportDictionary(this IScriptObject @this, IDictionary dictionary)
	{
		if (dictionary == null)
		{
			return;
		}
		foreach (DictionaryEntry item in dictionary)
		{
			string text = item.Key?.ToString();
			if (@this.CanWrite(text))
			{
				ScriptObject scriptObject = @this.GetScriptObject();
				scriptObject.AssertNotReadOnly();
				scriptObject[text] = item.Value;
			}
		}
	}

	public static ScriptObject GetScriptObject(this IScriptObject @this)
	{
		ScriptObject scriptObject = @this as ScriptObject;
		if (scriptObject == null)
		{
			scriptObject = ((@this as ScriptArray) ?? throw new ArgumentException("Expecting ScriptObject or ScriptArray instance", "this")).ScriptObject;
		}
		return scriptObject;
	}

	public static void ImportMember(this IScriptObject script, object obj, string memberName, string exportName = null)
	{
		script.Import(obj, ScriptMemberImportFlags.All, (MemberInfo member) => member.Name == memberName, (exportName != null) ? ((MemberRenamerDelegate)((MemberInfo name) => exportName)) : null);
	}

	public static void Import(this IScriptObject script, object obj, ScriptMemberImportFlags flags, MemberFilterDelegate filter = null, MemberRenamerDelegate renamer = null)
	{
		if (obj == null)
		{
			return;
		}
		if (!ScriptObject.IsImportable(obj))
		{
			throw new ArgumentOutOfRangeException("obj", $"Unsupported object type `{obj.GetType()}`. Expecting plain class or struct");
		}
		TypeInfo typeInfo = ((obj as Type) ?? obj.GetType()).GetTypeInfo();
		bool flag = false;
		bool flag2 = false;
		if (obj is Type)
		{
			flag = true;
			obj = null;
		}
		else
		{
			flag2 = true;
		}
		renamer = renamer ?? StandardMemberRenamer.Default;
		Stack<TypeInfo> stack = new Stack<TypeInfo>();
		while (typeInfo != null)
		{
			stack.Push(typeInfo);
			if (typeInfo.BaseType == typeof(object))
			{
				break;
			}
			typeInfo = typeInfo.BaseType.GetTypeInfo();
		}
		while (stack.Count > 0)
		{
			typeInfo = stack.Pop();
			if ((flags & ScriptMemberImportFlags.Field) != 0)
			{
				foreach (FieldInfo declaredField in typeInfo.GetDeclaredFields())
				{
					if (declaredField.IsPublic && (filter == null || filter(declaredField)) && declaredField.GetCustomAttribute<ScriptMemberIgnoreAttribute>() == null && ((declaredField.IsStatic && flag) || flag2))
					{
						string text = renamer(declaredField);
						if (string.IsNullOrEmpty(text))
						{
							text = declaredField.Name;
						}
						script.SetValue(null, default(SourceSpan), text, declaredField.GetValue(obj), declaredField.IsInitOnly || declaredField.IsLiteral);
					}
				}
			}
			if ((flags & ScriptMemberImportFlags.Property) != 0)
			{
				foreach (PropertyInfo declaredProperty in typeInfo.GetDeclaredProperties())
				{
					MethodInfo getMethod = declaredProperty.GetMethod;
					if (declaredProperty.CanRead && getMethod.IsPublic && (filter == null || filter(declaredProperty)) && declaredProperty.GetCustomAttribute<ScriptMemberIgnoreAttribute>() == null && ((getMethod.IsStatic && flag) || flag2))
					{
						string text2 = renamer(declaredProperty);
						if (string.IsNullOrEmpty(text2))
						{
							text2 = declaredProperty.Name;
						}
						script.SetValue(null, default(SourceSpan), text2, declaredProperty.GetValue(obj), readOnly: false);
					}
				}
			}
			if (!((flags & ScriptMemberImportFlags.Method) != 0 && flag))
			{
				continue;
			}
			foreach (MethodInfo declaredMethod in typeInfo.GetDeclaredMethods())
			{
				if ((filter == null || filter(declaredMethod)) && declaredMethod.GetCustomAttribute<ScriptMemberIgnoreAttribute>() == null && declaredMethod.IsPublic && declaredMethod.IsStatic && !declaredMethod.IsSpecialName)
				{
					string text3 = renamer(declaredMethod);
					if (string.IsNullOrEmpty(text3))
					{
						text3 = declaredMethod.Name;
					}
					script.SetValue(null, default(SourceSpan), text3, DynamicCustomFunction.Create(obj, declaredMethod), readOnly: true);
				}
			}
		}
	}

	public static void Import(this IScriptObject script, string member, Delegate function)
	{
		if (member == null)
		{
			throw new ArgumentNullException("member");
		}
		if ((object)function == null)
		{
			throw new ArgumentNullException("function");
		}
		script.SetValue(null, default(SourceSpan), member, DynamicCustomFunction.Create(function.Target, function.GetMethodInfo()), readOnly: true);
	}
}
