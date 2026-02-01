using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace NiceInventoryTab;

public static class DevUtility
{
	private static HashSet<object> _visitedObjects = new HashSet<object>();

	public static void LogObjectDeep(object obj, string label = "Object", int maxDepth = 3)
	{
		_visitedObjects.Clear();
		LogObjectRecursive(obj, label, 0, maxDepth);
	}

	private static void LogObjectRecursive(object obj, string path, int currentDepth, int maxDepth)
	{
		if (obj == null)
		{
			Log.Message(GetIndent(currentDepth) + path + ": NULL");
			return;
		}
		Type type = obj.GetType();
		if (currentDepth >= maxDepth)
		{
			Log.Message(GetIndent(currentDepth) + path + ": [" + type.Name + "] (max depth reached)");
			return;
		}
		if (!type.IsPrimitive && !type.IsEnum && !(obj is string))
		{
			if (_visitedObjects.Contains(obj))
			{
				Log.Message(GetIndent(currentDepth) + path + ": [" + type.Name + "] (already visited - circular reference)");
				return;
			}
			_visitedObjects.Add(obj);
		}
		Log.Message(GetIndent(currentDepth) + path + ": [" + type.Name + "]");
		if (type.IsPrimitive || type.IsEnum || !(obj is string))
		{
			Log.Message($"{GetIndent(currentDepth + 1)}Value: {obj}");
			return;
		}
		if (obj is IEnumerable enumerable && !(obj is string))
		{
			int num = 0;
			{
				foreach (object item in enumerable)
				{
					if (num >= 10)
					{
						Log.Message(GetIndent(currentDepth + 1) + "[...] (more items)");
						break;
					}
					LogObjectRecursive(item, $"{path}[{num}]", currentDepth + 1, maxDepth);
					num++;
				}
				return;
			}
		}
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (fields.Length != 0)
		{
			Log.Message($"{GetIndent(currentDepth + 1)}=== FIELDS ({fields.Length}) ===");
			FieldInfo[] array = fields;
			foreach (FieldInfo fieldInfo in array)
			{
				try
				{
					object value = fieldInfo.GetValue(obj);
					string path2 = fieldInfo.Name + " (" + fieldInfo.FieldType.Name + ") [" + GetMemberModifiers(fieldInfo) + "]";
					LogObjectRecursive(value, path2, currentDepth + 2, maxDepth);
				}
				catch (Exception ex)
				{
					Log.Warning(GetIndent(currentDepth + 2) + fieldInfo.Name + ": ERROR - " + ex.Message);
				}
			}
		}
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (properties.Length != 0)
		{
			Log.Message($"{GetIndent(currentDepth + 1)}=== PROPERTIES ({properties.Length}) ===");
			PropertyInfo[] array2 = properties;
			foreach (PropertyInfo propertyInfo in array2)
			{
				try
				{
					if (propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
					{
						object value2 = propertyInfo.GetValue(obj);
						string path3 = propertyInfo.Name + " (" + propertyInfo.PropertyType.Name + ") [" + GetPropertyModifiers(propertyInfo) + "]";
						LogObjectRecursive(value2, path3, currentDepth + 2, maxDepth);
					}
					else
					{
						Log.Message(GetIndent(currentDepth + 2) + propertyInfo.Name + " (" + propertyInfo.PropertyType.Name + ") [indexed or write-only]");
					}
				}
				catch (Exception ex2)
				{
					Log.Warning(GetIndent(currentDepth + 2) + propertyInfo.Name + ": ERROR - " + ex2.Message);
				}
			}
		}
		if (currentDepth != 0)
		{
			return;
		}
		MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
		if (methods.Length == 0)
		{
			return;
		}
		Log.Message($"{GetIndent(currentDepth + 1)}=== METHODS ({methods.Length}) ===");
		foreach (MethodInfo item2 in methods.Take(20))
		{
			string text = string.Join(", ", from p in item2.GetParameters()
				select p.ParameterType.Name + " " + p.Name);
			Log.Message(GetIndent(currentDepth + 2) + item2.ReturnType.Name + " " + item2.Name + "(" + text + ")");
		}
	}

	private static string GetIndent(int depth)
	{
		return new string(' ', depth * 2);
	}

	private static string GetMemberModifiers(FieldInfo field)
	{
		List<string> list = new List<string>();
		if (field.IsPublic)
		{
			list.Add("public");
		}
		if (field.IsPrivate)
		{
			list.Add("private");
		}
		if (field.IsStatic)
		{
			list.Add("static");
		}
		if (field.IsInitOnly)
		{
			list.Add("readonly");
		}
		return string.Join(" ", list);
	}

	private static string GetPropertyModifiers(PropertyInfo prop)
	{
		List<string> list = new List<string>();
		MethodInfo getMethod = prop.GetGetMethod(nonPublic: true);
		MethodInfo setMethod = prop.GetSetMethod(nonPublic: true);
		if (getMethod != null)
		{
			if (getMethod.IsPublic)
			{
				list.Add("get:public");
			}
			else if (getMethod.IsPrivate)
			{
				list.Add("get:private");
			}
		}
		if (setMethod != null)
		{
			if (setMethod.IsPublic)
			{
				list.Add("set:public");
			}
			else if (setMethod.IsPrivate)
			{
				list.Add("set:private");
			}
		}
		return string.Join(" ", list);
	}

	public static void LogInfo(Thing th)
	{
		LogObjectDeep(th, th.LabelCap, 2);
	}
}
