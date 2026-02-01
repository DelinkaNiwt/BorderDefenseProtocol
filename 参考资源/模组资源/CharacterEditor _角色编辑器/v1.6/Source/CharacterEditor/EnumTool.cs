using System;
using System.Collections.Generic;
using System.Linq;

namespace CharacterEditor;

internal static class EnumTool
{
	internal static List<T> GetAllEnumsOfType<T>()
	{
		return Enum.GetValues(typeof(T)).OfType<T>().ToList();
	}

	internal static HashSet<string> GetEnumsNamesAsStringHashSet<T>()
	{
		return Enum.GetNames(typeof(T)).ToHashSet();
	}
}
