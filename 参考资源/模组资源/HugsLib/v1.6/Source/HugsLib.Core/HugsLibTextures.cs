using System.Reflection;
using UnityEngine;
using Verse;

namespace HugsLib.Core;

/// <summary>
/// Loads and stores textures from the HugsLib /Textures folder
/// </summary>
[StaticConstructorOnStartup]
internal static class HugsLibTextures
{
	public static Texture2D quickstartIcon;

	public static Texture2D HLMenuIcon;

	public static Texture2D HLMenuIconPlus;

	public static Texture2D HLInfoIcon;

	static HugsLibTextures()
	{
		FieldInfo[] fields = typeof(HugsLibTextures).GetFields(BindingFlags.Static | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			fieldInfo.SetValue(null, ContentFinder<Texture2D>.Get(fieldInfo.Name));
		}
	}
}
