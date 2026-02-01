using System;
using System.Collections.Generic;
using System.Reflection;

namespace Scriban.Helpers;

internal static class ReflectionHelper
{
	public static bool IsPrimitiveOrDecimal(this Type type)
	{
		if (!type.GetTypeInfo().IsPrimitive)
		{
			return type == typeof(decimal);
		}
		return true;
	}

	public static Type GetBaseOrInterface(this Type type, Type lookInterfaceTypeArg)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (lookInterfaceTypeArg == null)
		{
			throw new ArgumentNullException("lookInterfaceTypeArg");
		}
		TypeInfo typeInfo = lookInterfaceTypeArg.GetTypeInfo();
		if (typeInfo.IsGenericTypeDefinition)
		{
			if (typeInfo.IsInterface)
			{
				foreach (Type implementedInterface in type.GetTypeInfo().ImplementedInterfaces)
				{
					if (implementedInterface.GetTypeInfo().IsGenericType && implementedInterface.GetTypeInfo().GetGenericTypeDefinition() == lookInterfaceTypeArg)
					{
						return implementedInterface;
					}
				}
			}
			Type type2 = type;
			while (type2 != null)
			{
				if (type2.GetTypeInfo().IsGenericType && type2.GetTypeInfo().GetGenericTypeDefinition() == lookInterfaceTypeArg)
				{
					return type2;
				}
				type2 = type2.GetTypeInfo().BaseType;
			}
		}
		else if (typeInfo.IsAssignableFrom(type.GetTypeInfo()))
		{
			return lookInterfaceTypeArg;
		}
		return null;
	}

	public static Type[] GetGenericArguments(this TypeInfo type)
	{
		return type.GenericTypeArguments;
	}

	public static IEnumerable<FieldInfo> GetDeclaredFields(this TypeInfo type)
	{
		return type.DeclaredFields;
	}

	public static IEnumerable<PropertyInfo> GetDeclaredProperties(this TypeInfo type)
	{
		return type.DeclaredProperties;
	}

	public static IEnumerable<MethodInfo> GetDeclaredMethods(this TypeInfo type)
	{
		return type.DeclaredMethods;
	}
}
