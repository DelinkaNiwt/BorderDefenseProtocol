using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace CharacterEditor;

internal static class Reflect
{
	internal static string APP_VERISON_AND_DATE
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			AssemblyDescriptionAttribute customAttribute = executingAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
			AssemblyName name = executingAssembly.GetName();
			Version version = name.Version;
			stringBuilder.Append("v");
			stringBuilder.Append(version.Major.ToString());
			stringBuilder.Append(".");
			stringBuilder.Append(version.Minor.ToString());
			stringBuilder.Append(".");
			stringBuilder.Append(version.Build.ToString());
			stringBuilder.Append(" " + customAttribute.Description);
			return stringBuilder.ToString();
		}
	}

	internal static int VERSION_BUILD => Assembly.GetExecutingAssembly().GetName().Version.Build;

	internal static string APP_NAME_AND_VERISON
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			AssemblyDescriptionAttribute customAttribute = executingAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
			AssemblyName name = executingAssembly.GetName();
			Version version = name.Version;
			stringBuilder.Append(APP_ATTRIBUTE_TITLE);
			stringBuilder.Append(" v");
			stringBuilder.Append(version.Major.ToString());
			stringBuilder.Append(".");
			stringBuilder.Append(version.Minor.ToString());
			stringBuilder.Append(".");
			stringBuilder.Append(version.Build.ToString());
			return stringBuilder.ToString();
		}
	}

	internal static string APP_ATTRIBUTE_TITLE => ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), inherit: false)).Title;

	internal static Type ByName(string name)
	{
		foreach (Assembly item in AppDomain.CurrentDomain.GetAssemblies().Reverse())
		{
			Type type = item.GetType(name);
			if (type != null)
			{
				return type;
			}
		}
		return null;
	}

	internal static object GetMemberValue(this Type type, string name)
	{
		BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		return (type?.GetField(name, bindingAttr))?.GetValue(null);
	}

	internal static void SetMemberValue(this Type type, string name, object value)
	{
		BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		(type?.GetField(name, bindingAttr))?.SetValue(null, value);
	}

	internal static object GetMemberConstValue(this object obj, string name)
	{
		return (obj?.GetType().GetField(name))?.GetValue(null);
	}

	internal static T GetMemberValue<T>(this object obj, string name, T fallback)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		object obj2 = (obj?.GetType().GetField(name, bindingAttr))?.GetValue(obj);
		if (obj2 != null)
		{
			return (T)obj2;
		}
		return fallback;
	}

	internal static string GetMemberValueAsString<T>(this object obj, string name, string fallback)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		object obj2 = (obj?.GetType().GetField(name, bindingAttr))?.GetValue(obj);
		if (obj2 != null)
		{
			return ((T)obj2/*cast due to .constrained prefix*/).ToString();
		}
		return fallback;
	}

	internal static object GetMemberStructValue(this object obj, string structName, string fieldName)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		object obj2 = (obj?.GetType().GetField(structName, bindingAttr))?.GetValue(obj);
		return (obj2?.GetType().GetField(fieldName, bindingAttr))?.GetValue(obj2);
	}

	[Obsolete]
	internal static object GetMemberValueOld(this object obj, string name)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		return (obj?.GetType().GetField(name, bindingAttr))?.GetValue(obj);
	}

	internal static void SetMemberValue(this object obj, string name, object value)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		(obj?.GetType().GetField(name, bindingAttr))?.SetValue(obj, value);
	}

	internal static object CallMethod(this object obj, string name, object[] param)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		return obj?.GetType().GetMethod(name, bindingAttr)?.Invoke(obj, param);
	}

	internal static object CallMethodAmbiguous(this object obj, string name, object[] param)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		MethodInfo[] array = obj?.GetType().GetMethods(bindingAttr);
		MethodInfo[] array2 = array;
		foreach (MethodInfo methodInfo in array2)
		{
			if (methodInfo.Name == name && methodInfo.GetParameters().Length == param.Length)
			{
				return methodInfo?.Invoke(obj, param);
			}
		}
		return null;
	}

	internal static object CallMethod(this Type type, string name, object[] param)
	{
		BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		return type?.GetMethod(name, bindingAttr)?.Invoke(null, param);
	}

	internal static object CallMethod(this MethodInfo mi, object[] param, object instance = null)
	{
		return mi?.Invoke(instance, param);
	}

	internal static MethodInfo GetMethodInfo(this Type type, string name)
	{
		BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		return type?.GetMethod(name, bindingAttr);
	}

	internal static MethodInfo GetMethodInfo(this object obj, string name)
	{
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		return obj?.GetType().GetMethod(name, bindingAttr);
	}

	internal static Type GetAssemblyType(string name, string type)
	{
		Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault((Assembly assembly2) => assembly2.GetName().Name == name);
		return assembly.GetType(type);
	}

	internal static Type GetAType(string nameSpace, string className)
	{
		return GenTypes.GetTypeInAnyAssembly(nameSpace + "." + className, nameSpace);
	}
}
