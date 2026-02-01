using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace NCL;

public class CompPropertiesWeaponSwitch : CompProperties
{
	public List<TransformData> transformData = new List<TransformData>();

	public TransformData revertData;

	public List<string> sharedComps = new List<string>();

	private HashSet<Type> sharedCompsResolved;

	public HashSet<Type> SharedCompsResolved
	{
		get
		{
			if (sharedCompsResolved == null)
			{
				sharedCompsResolved = new HashSet<Type>();
				ResolveSharedComps();
			}
			return sharedCompsResolved;
		}
	}

	public CompPropertiesWeaponSwitch()
	{
		compClass = typeof(CompFormChange);
	}

	public void ResolveSharedComps()
	{
		for (int i = 0; i < sharedComps.Count; i++)
		{
			Type item = TypeFromString(sharedComps[i]);
			sharedCompsResolved.Add(item);
		}
	}

	public static Type TypeFromString(string typeString)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			Type type = assemblies[i].GetType(typeString, throwOnError: false, ignoreCase: true);
			if (type != null)
			{
				return type;
			}
		}
		Type type2 = Type.GetType(typeString, throwOnError: false, ignoreCase: true);
		if (type2 != null)
		{
			return type2;
		}
		return null;
	}
}
