using System;
using Verse;

namespace CharacterEditor;

public static class CustomLabel
{
	public static void LoadLabelsFromFile(string path)
	{
		if (path.NullOrEmpty() || !FileIO.Exists(path))
		{
			return;
		}
		byte[] array = FileIO.ReadFile(path);
		if (array == null || array.Length == 0)
		{
			return;
		}
		string text = array.AsStringUNICODE();
		if (text.NullOrEmpty())
		{
			return;
		}
		try
		{
			string[] array2 = text.SplitNo(";\r\n");
			Log.Message("Loading CharacterEditor labels from file... label-count: " + array2.Length);
			string[] array3 = array2;
			foreach (string text2 in array3)
			{
				string labelName = text2.SubstringTo("=").Trim();
				string text3 = text2.SubstringFrom("\"").SubstringTo("\"");
				SetLabel(labelName, text3);
			}
		}
		catch
		{
		}
	}

	public static string GetLabel(string labelName)
	{
		try
		{
			Type aType = Reflect.GetAType("CharacterEditor", "Label");
			return (string)aType.GetMemberValue(labelName);
		}
		catch
		{
		}
		return null;
	}

	public static void SetLabel(string labelName, string text)
	{
		try
		{
			Type aType = Reflect.GetAType("CharacterEditor", "Label");
			aType.SetMemberValue(labelName, text);
		}
		catch
		{
		}
	}
}
