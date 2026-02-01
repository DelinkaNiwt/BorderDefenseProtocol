using System;
using System.Reflection;

namespace Scriban.Syntax;

public class ScriptSyntaxAttribute : Attribute
{
	public string Name { get; }

	public string Example { get; }

	private ScriptSyntaxAttribute()
	{
	}

	public ScriptSyntaxAttribute(string name, string example)
	{
		Name = name;
		Example = example;
	}

	public static ScriptSyntaxAttribute Get(object obj)
	{
		if (obj == null)
		{
			return null;
		}
		return Get(obj.GetType());
	}

	public static ScriptSyntaxAttribute Get(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		return type.GetTypeInfo().GetCustomAttribute<ScriptSyntaxAttribute>() ?? new ScriptSyntaxAttribute(type.Name, "...");
	}
}
