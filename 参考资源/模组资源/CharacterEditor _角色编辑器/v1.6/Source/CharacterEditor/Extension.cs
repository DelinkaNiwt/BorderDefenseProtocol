using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class Extension
{
	internal static void AddTag(ref TagFilter t, string s)
	{
		if (t == null)
		{
			t = new TagFilter();
		}
		t.tags.Add(s);
	}

	internal static void ChangeXPosition(this Rect rect, ref bool bdoFlag, int xoffset)
	{
		if (bdoFlag)
		{
			bdoFlag = false;
			rect.position = new Vector2(rect.position.x + (float)xoffset, rect.position.y);
		}
	}

	internal static void AddFromList<T1, T2>(this SortedDictionary<T1, T2> dic, List<T1> otherList, T2 defaultVal = default(T2))
	{
		if (dic == null || otherList.NullOrEmpty())
		{
			return;
		}
		foreach (T1 other in otherList)
		{
			if (!dic.ContainsKey(other))
			{
				dic.Add(other, defaultVal);
			}
		}
	}

	internal static void AddFromList<T1, T2>(this Dictionary<T1, T2> dic, List<T1> otherList, T2 defaultVal = default(T2))
	{
		if (dic == null || otherList.NullOrEmpty())
		{
			return;
		}
		foreach (T1 other in otherList)
		{
			if (!dic.ContainsKey(other))
			{
				dic.Add(other, defaultVal);
			}
		}
	}

	internal static void AddLabeled<T>(this Dictionary<string, T> dic, string key, T value)
	{
		if (dic == null)
		{
			dic = new Dictionary<string, T>();
		}
		if (!dic.ContainsKey(key))
		{
			dic.Add(key, value);
		}
		else
		{
			dic.Add(key + dic.Count, value);
		}
	}

	internal static void AddLabeled<T>(this SortedDictionary<string, T> dic, string key, T value)
	{
		if (dic == null)
		{
			dic = new SortedDictionary<string, T>();
		}
		if (!dic.ContainsKey(key))
		{
			dic.Add(key, value);
		}
		else
		{
			dic.Add(key + dic.Count, value);
		}
	}

	internal static void AddSkipDuplicate<T1, T2>(this Dictionary<T1, T2> dic, T1 key, T2 value)
	{
		if (dic == null)
		{
			dic = new Dictionary<T1, T2>();
		}
		if (!dic.ContainsKey(key))
		{
			dic.Add(key, value);
		}
	}

	internal static void AddSkipDuplicate<T1, T2>(this SortedDictionary<T1, T2> dic, T1 key, T2 value)
	{
		if (dic == null)
		{
			dic = new SortedDictionary<T1, T2>();
		}
		if (!dic.ContainsKey(key))
		{
			dic.Add(key, value);
		}
	}

	internal static string AsString<T1, T2>(this Dictionary<T1, T2> dic, string pairator = "", string separator = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (dic != null && dic.Count > 0)
		{
			int num = dic.Count;
			using Dictionary<T1, T2>.Enumerator enumerator = dic.GetEnumerator();
			while (enumerator.MoveNext())
			{
				num--;
				stringBuilder.Append(enumerator.Current.Key.ToString());
				stringBuilder.Append(pairator);
				stringBuilder.Append(enumerator.Current.Value.ToString());
				if (num > 0)
				{
					stringBuilder.Append(separator);
				}
			}
		}
		return stringBuilder.ToString();
	}

	internal static string AsString<T1, T2>(this SortedDictionary<T1, T2> dic, string pairator = "", string separator = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (dic != null && dic.Count > 0)
		{
			int num = dic.Count;
			using SortedDictionary<T1, T2>.Enumerator enumerator = dic.GetEnumerator();
			while (enumerator.MoveNext())
			{
				num--;
				stringBuilder.Append(enumerator.Current.Key.ToString());
				stringBuilder.Append(pairator);
				stringBuilder.Append(enumerator.Current.Value.ToString());
				if (num > 0)
				{
					stringBuilder.Append(separator);
				}
			}
		}
		return stringBuilder.ToString();
	}

	internal static T2 GetValue<T1, T2>(this Dictionary<T1, T2> dic, T1 key)
	{
		if (dic != null && dic.ContainsKey(key))
		{
			return dic[key];
		}
		return default(T2);
	}

	internal static T2 GetValue<T1, T2>(this SortedDictionary<T1, T2> dic, T1 key)
	{
		if (dic != null && dic.ContainsKey(key))
		{
			return dic[key];
		}
		return default(T2);
	}

	internal static T1 KeyByValue<T1, T2>(this Dictionary<T1, T2> dic, T2 value)
	{
		if (dic != null)
		{
			foreach (T1 key in dic.Keys)
			{
				if (dic[key] != null && dic[key].ToString() == value.ToString())
				{
					return key;
				}
			}
		}
		return default(T1);
	}

	internal static string KeyByValue<T>(this SortedDictionary<string, T> dic, T value)
	{
		if (dic != null)
		{
			foreach (string key in dic.Keys)
			{
				if (dic[key].Equals(value))
				{
					return key;
				}
			}
		}
		return "";
	}

	internal static void Merge<T1, T2>(this Dictionary<T1, T2> dic1, Dictionary<T1, T2> dic2)
	{
		if (dic1 == null)
		{
			dic1 = new Dictionary<T1, T2>();
		}
		foreach (T1 key in dic2.Keys)
		{
			if (!dic1.ContainsKey(key))
			{
				dic1.Add(key, dic2[key]);
			}
		}
	}

	internal static void AddElem<T>(ref List<T> l, T element)
	{
		if (l == null)
		{
			l = new List<T>();
		}
		l.Add(element);
	}

	internal static void AddElemUnique<T>(ref List<T> l, T element)
	{
		if (l == null)
		{
			l = new List<T>();
		}
		if (!l.Contains(element))
		{
			l.Add(element);
		}
	}

	internal static void AddFromList<T>(this List<T> l, List<T> otherList)
	{
		if (l == null || otherList.NullOrEmpty())
		{
			return;
		}
		foreach (T other in otherList)
		{
			if (!l.Contains(other))
			{
				l.Add(other);
			}
		}
	}

	internal static bool IsSame<T>(this List<T> l, List<T> otherList)
	{
		foreach (T other in otherList)
		{
			if (!l.Contains(other))
			{
				return false;
			}
		}
		foreach (T item in l)
		{
			if (!l.Contains(item))
			{
				return false;
			}
		}
		return true;
	}

	internal static string ListToString<T>(this List<T> list)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (list != null)
		{
			if (typeof(T) == typeof(string))
			{
				foreach (string item in list.OfType<string>())
				{
					stringBuilder.Append(item + "|");
				}
			}
			else if (typeof(T) == typeof(int))
			{
				foreach (int item2 in list.OfType<int>())
				{
					stringBuilder.Append(item2 + "|");
				}
			}
			else if (typeof(T) == typeof(float))
			{
				foreach (float item3 in list.OfType<float>())
				{
					stringBuilder.Append(item3 + "|");
				}
			}
			else if (typeof(T) == typeof(StatModifier))
			{
				foreach (StatModifier item4 in list.OfType<StatModifier>())
				{
					if (item4 != null && item4.stat != null && item4.stat.defName != null)
					{
						stringBuilder.Append(item4.stat.defName + "!" + item4.value + "|");
					}
				}
			}
			else if (typeof(T) == typeof(Aptitude))
			{
				foreach (Aptitude item5 in list.OfType<Aptitude>())
				{
					if (item5 != null && item5.skill != null && item5.skill.defName != null)
					{
						stringBuilder.Append(item5.skill.defName + "!" + item5.level + "|");
					}
				}
			}
			else if (typeof(T) == typeof(ThingDefCountClass))
			{
				foreach (ThingDefCountClass item6 in list.OfType<ThingDefCountClass>())
				{
					if (item6 != null && item6.thingDef != null && item6.thingDef.defName != null)
					{
						stringBuilder.Append(item6.thingDef.defName + "!" + item6.count + "|");
					}
				}
			}
			else if (typeof(T) == typeof(GeneticTraitData))
			{
				foreach (GeneticTraitData item7 in list.OfType<GeneticTraitData>())
				{
					if (item7 != null && item7.def != null && item7.def.defName != null)
					{
						stringBuilder.Append(item7.def.defName + "!" + item7.degree + "|");
					}
				}
			}
			else if (typeof(T) == typeof(DamageFactor))
			{
				foreach (DamageFactor item8 in list.OfType<DamageFactor>())
				{
					if (item8 != null && item8.damageDef != null && item8.damageDef.defName != null)
					{
						stringBuilder.Append(item8.damageDef.defName + "!" + item8.factor + "|");
					}
				}
			}
			else if (typeof(T) == typeof(PawnCapacityModifier))
			{
				foreach (PawnCapacityModifier item9 in list.OfType<PawnCapacityModifier>())
				{
					if (item9 != null && item9.capacity != null && item9.capacity.defName != null)
					{
						stringBuilder.Append(item9.capacity.defName + "!" + item9.offset + "#" + item9.postFactor + "|");
					}
				}
			}
			else if (typeof(T) == typeof(BodyPartRecord))
			{
				foreach (BodyPartRecord item10 in list.OfType<BodyPartRecord>())
				{
					if (item10 != null && item10.def.defName != null)
					{
						stringBuilder.Append(item10.def.defName + "|");
					}
				}
			}
			else if (typeof(T) == typeof(TraitDef))
			{
				foreach (TraitDef item11 in list.OfType<TraitDef>())
				{
					if (item11 != null && item11.defName != null)
					{
						stringBuilder.Append(item11.defName + " (" + item11.degreeDatas.ListToString() + ")|");
					}
				}
			}
			else if (typeof(T) == typeof(TraitDegreeData))
			{
				foreach (TraitDegreeData item12 in list.OfType<TraitDegreeData>())
				{
					if (item12 != null && item12.label != null)
					{
						stringBuilder.Append(item12.label + "|");
					}
				}
			}
			else if (typeof(T) == typeof(Apparel))
			{
				foreach (Apparel item13 in list.OfType<Apparel>())
				{
					if (item13 != null && item13.Label != null)
					{
						stringBuilder.Append(item13.Label + ", ");
					}
				}
			}
			else if (typeof(T) == typeof(Pawn))
			{
				foreach (Pawn item14 in list.OfType<Pawn>())
				{
					if (item14 != null && item14.Label != null)
					{
						stringBuilder.Append(item14.Label + "|");
					}
				}
			}
			else if (typeof(T) == typeof(Texture2D))
			{
				foreach (Texture2D item15 in list.OfType<Texture2D>())
				{
					if (item15 != null && item15.name != null)
					{
						stringBuilder.Append(item15.name + "\n");
					}
				}
			}
			else if (typeof(T) == typeof(Def) || typeof(T).BaseType == typeof(Def) || (typeof(T).BaseType != null && typeof(T).BaseType.BaseType == typeof(Def)))
			{
				foreach (Def item16 in list.OfType<Def>())
				{
					if (item16 != null && item16.defName != null)
					{
						stringBuilder.Append(item16.defName + "|");
					}
				}
			}
			string text = stringBuilder.ToString();
			if (!string.IsNullOrEmpty(text))
			{
				text = text.Remove(text.Length - 1, 1);
			}
			return text;
		}
		return "";
	}

	internal static int NextOrPrevIndex<T>(this List<T> l, int index, bool next, bool random)
	{
		if (l.NullOrEmpty())
		{
			return 0;
		}
		if (random)
		{
			return l.IndexOf(l.RandomElement());
		}
		index = (next ? ((index + 1 < l.Count) ? (index + 1) : 0) : ((index - 1 < 0) ? (l.Count - 1) : (index - 1)));
		return index;
	}

	internal static int NextOrPrevIndex<T>(this HashSet<T> l, int index, bool next, bool random)
	{
		if (l.NullOrEmpty())
		{
			return 0;
		}
		if (random)
		{
			return l.IndexOf(l.RandomElement());
		}
		index = (next ? ((index + 1 < l.Count) ? (index + 1) : 0) : ((index - 1 < 0) ? (l.Count - 1) : (index - 1)));
		return index;
	}

	internal static int IndexOf<T>(this HashSet<T> l, T val)
	{
		return (!l.NullOrEmpty() && val != null) ? l.FirstIndexOf(delegate(T y)
		{
			ref T reference = ref val;
			object obj = y;
			return reference.Equals(obj);
		}) : 0;
	}

	internal static T At<T>(this HashSet<T> l, int index)
	{
		return l.NullOrEmpty() ? default(T) : l.ElementAt(index);
	}

	internal static T At<T>(this List<T> l, int index)
	{
		return l.NullOrEmpty() ? default(T) : l.ElementAt(index);
	}

	internal static bool NullOrEmpty<T>(this HashSet<T> l)
	{
		return l == null || l.Count == 0;
	}

	internal static void RemoveDuplicates<T>(this HashSet<T> l) where T : Def
	{
		HashSet<T> hashSet = new HashSet<T>();
		foreach (T item in l)
		{
			if (!hashSet.Contains(item))
			{
				hashSet.Add(item);
			}
		}
		l = hashSet;
	}

	internal static T[] AddSeqRange<T>(this T[] sequence, T[] items)
	{
		return (sequence ?? Enumerable.Empty<T>()).Concat(items).ToArray();
	}

	internal static string[] SplitNo(this string s, string x)
	{
		return s.Split(new string[1] { x }, StringSplitOptions.None);
	}

	internal static string[] SplitNoEmpty(this string s, string x)
	{
		return s.Split(new string[1] { x }, StringSplitOptions.RemoveEmptyEntries);
	}

	internal static bool AsBool(this string s)
	{
		return s == "1" || s == "True";
	}

	internal static bool AsBoolWithDefault(this string s, bool defVal)
	{
		return s.NullOrEmpty() ? defVal : (s == "1" || s == "True");
	}

	internal static float AsFloat(this string input)
	{
		float result = 0f;
		float.TryParse(input, out result);
		return result;
	}

	internal static float? AsFloatZero(this string input)
	{
		float result = 0f;
		if (float.TryParse(input, out result))
		{
			return result;
		}
		return null;
	}

	internal static int AsInt32(this string input)
	{
		int result = 0;
		int.TryParse(input, out result);
		return result;
	}

	internal static long AsLong(this string input)
	{
		long result = 0L;
		long.TryParse(input, out result);
		return result;
	}

	internal static Vector3 AsVector3NonZero(this string input)
	{
		string[] array = input.SplitNo("#");
		if (array.Length == 3 && !array[0].NullOrEmpty() && !array[1].NullOrEmpty() && !array[2].NullOrEmpty())
		{
			float x = array[0].AsFloat();
			float y = array[1].AsFloat();
			float z = array[2].AsFloat();
			return new Vector3(x, y, z);
		}
		return default(Vector3);
	}

	internal static Vector3? AsVector3Zero(this string input)
	{
		string[] array = input.SplitNo("#");
		if (array.Length == 3 && !array[0].NullOrEmpty() && !array[1].NullOrEmpty() && !array[2].NullOrEmpty())
		{
			float x = array[0].AsFloat();
			float y = array[1].AsFloat();
			float z = array[2].AsFloat();
			return new Vector3(x, y, z);
		}
		return null;
	}

	internal static string AsString(this Vector3 v)
	{
		return v.x + "#" + v.y + "#" + v.z;
	}

	internal static string AsString(this Vector3? v)
	{
		return (!v.HasValue) ? "" : (v.Value.x + "#" + v.Value.y + "#" + v.Value.z);
	}

	internal static void AsDefValue(this string s, out string defName, out string value)
	{
		string[] array = s.SplitNo("!");
		defName = array.GetStringValue(0);
		value = array.GetStringValue(1);
	}

	internal static KeyCode AsKeyCode(this string key)
	{
		try
		{
			return (KeyCode)Enum.Parse(typeof(KeyCode), key);
		}
		catch
		{
			return KeyCode.None;
		}
	}

	internal static string ParamDictionaryAsString<T>(SortedDictionary<T, string> dicParams)
	{
		string text = "";
		foreach (T key in dicParams.Keys)
		{
			text = text + dicParams[key] + ",";
		}
		return text.SubstringRemoveLast();
	}

	internal static bool LoadSeparatedStringIntoDictionary<T>(string custom, ref SortedDictionary<T, string> dicParams, Func<int, T> Tgetter, int maxParamCount)
	{
		dicParams = new SortedDictionary<T, string>();
		if (!string.IsNullOrEmpty(custom))
		{
			string[] array = custom.Trim().SplitNo(",");
			string[] array2 = array;
			foreach (string value in array2)
			{
				if (dicParams.Count < maxParamCount)
				{
					dicParams.Add(Tgetter(dicParams.Count), value);
				}
			}
		}
		return dicParams.Count != 0;
	}

	internal static List<string> StringToList(this string slist)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string item in array2)
			{
				list.Add(item);
			}
		}
		return list;
	}

	internal static List<float> StringToFList(this string slist)
	{
		List<float> list = new List<float>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string input in array2)
			{
				list.Add(input.AsFloat());
			}
		}
		return list;
	}

	internal static List<StatModifier> StringToListStatModifier(string slist)
	{
		List<StatModifier> list = new List<StatModifier>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string s in array2)
			{
				s.AsDefValue(out var defName, out var value);
				StatModifier statModifier = new StatModifier();
				statModifier.stat = DefTool.StatDef(defName);
				if (statModifier.stat != null)
				{
					statModifier.value = value.AsFloat();
					list.Add(statModifier);
				}
			}
		}
		return list;
	}

	internal static List<Aptitude> StringToListAptitudes(string slist)
	{
		List<Aptitude> list = new List<Aptitude>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string s in array2)
			{
				s.AsDefValue(out var defName, out var value);
				Aptitude aptitude = new Aptitude();
				aptitude.skill = DefTool.SkillDef(defName);
				if (aptitude.skill != null)
				{
					aptitude.level = value.AsInt32();
					list.Add(aptitude);
				}
			}
		}
		return list;
	}

	internal static List<ThingDefCountClass> StringToListThingDefCountClass(string slist)
	{
		List<ThingDefCountClass> list = new List<ThingDefCountClass>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string s in array2)
			{
				s.AsDefValue(out var defName, out var value);
				ThingDefCountClass thingDefCountClass = new ThingDefCountClass();
				thingDefCountClass.thingDef = DefTool.GetDef<ThingDef>(defName);
				if (thingDefCountClass.thingDef != null)
				{
					thingDefCountClass.count = value.AsInt32();
					list.Add(thingDefCountClass);
				}
			}
		}
		return list;
	}

	internal static List<PassionMod> StringToListPassionMods(string slist)
	{
		List<PassionMod> list = new List<PassionMod>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string s in array2)
			{
				s.AsDefValue(out var defName, out var value);
				PassionMod passionMod = new PassionMod();
				passionMod.skill = DefTool.GetDef<SkillDef>(defName);
				if (passionMod.skill != null && Enum.TryParse<PassionMod.PassionModType>(value, out var result))
				{
					passionMod.modType = result;
					list.Add(passionMod);
				}
			}
		}
		return list;
	}

	internal static TagFilter StringToTagFilter(this string slist)
	{
		if (!slist.NullOrEmpty())
		{
			TagFilter tagFilter = new TagFilter();
			tagFilter.tags = slist.StringToList();
			return tagFilter;
		}
		return null;
	}

	internal static string AsListString(this PassionMod p)
	{
		string result = "";
		if (p != null && p.skill != null && p.skill.defName != null)
		{
			string defName = p.skill.defName;
			int modType = (int)p.modType;
			result = defName + "!" + modType + "|";
		}
		return result;
	}

	internal static string AsListString(this TagFilter t)
	{
		if (t != null && !t.tags.NullOrEmpty())
		{
			return t.tags.ListToString();
		}
		return "";
	}

	internal static List<T> StringToListNonDef<T>(this string slist)
	{
		if (typeof(T) == typeof(StatModifier))
		{
			return StringToListStatModifier(slist) as List<T>;
		}
		if (typeof(T) == typeof(Aptitude))
		{
			return StringToListAptitudes(slist) as List<T>;
		}
		if (typeof(T) == typeof(PawnCapacityModifier))
		{
			return StringToListCapacities(slist) as List<T>;
		}
		if (typeof(T) == typeof(GeneticTraitData))
		{
			return StringToListGeneticTraitData(slist) as List<T>;
		}
		if (typeof(T) == typeof(DamageFactor))
		{
			return StringToListDamageFactors(slist) as List<T>;
		}
		if (typeof(T) == typeof(PassionMod))
		{
			return StringToListPassionMods(slist) as List<T>;
		}
		if (typeof(T) == typeof(ThingDefCountClass))
		{
			return StringToListThingDefCountClass(slist) as List<T>;
		}
		return null;
	}

	internal static List<T> StringToList<T>(this string slist) where T : Def
	{
		List<T> list = new List<T>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string defName in array2)
			{
				T def = DefTool.GetDef<T>(defName);
				if (def != null)
				{
					list.Add(def);
				}
			}
		}
		return list;
	}

	internal static List<DamageFactor> StringToListDamageFactors(string slist)
	{
		List<DamageFactor> list = new List<DamageFactor>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string s in array2)
			{
				s.AsDefValue(out var defName, out var value);
				DamageFactor damageFactor = new DamageFactor();
				damageFactor.damageDef = DefTool.GetDef<DamageDef>(defName);
				if (damageFactor.damageDef != null)
				{
					damageFactor.factor = value.AsFloat();
					list.Add(damageFactor);
				}
			}
		}
		return list;
	}

	internal static List<GeneticTraitData> StringToListGeneticTraitData(string slist)
	{
		List<GeneticTraitData> list = new List<GeneticTraitData>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string s in array2)
			{
				s.AsDefValue(out var defName, out var value);
				GeneticTraitData geneticTraitData = new GeneticTraitData();
				geneticTraitData.def = DefTool.GetDef<TraitDef>(defName);
				if (geneticTraitData.def != null)
				{
					geneticTraitData.degree = value.AsInt32();
					list.Add(geneticTraitData);
				}
			}
		}
		return list;
	}

	internal static List<PawnCapacityModifier> StringToListCapacities(string slist)
	{
		List<PawnCapacityModifier> list = new List<PawnCapacityModifier>();
		if (!string.IsNullOrEmpty(slist))
		{
			string[] array = slist.SplitNoEmpty("|");
			string[] array2 = array;
			foreach (string s in array2)
			{
				string[] array3 = s.SplitNo("!");
				string stringValue = array3.GetStringValue(0);
				string stringValue2 = array3.GetStringValue(1);
				string[] array4 = stringValue2.SplitNo("#");
				string stringValue3 = array4.GetStringValue(0);
				string stringValue4 = array4.GetStringValue(1);
				PawnCapacityModifier pawnCapacityModifier = new PawnCapacityModifier();
				pawnCapacityModifier.capacity = DefTool.PawnCapacityDef(stringValue);
				if (pawnCapacityModifier.capacity != null)
				{
					pawnCapacityModifier.offset = stringValue3.AsFloat();
					pawnCapacityModifier.postFactor = stringValue4.AsFloat();
					list.Add(pawnCapacityModifier);
				}
			}
		}
		return list;
	}

	internal static string GetStringValue(this string[] array, int index)
	{
		return (array.NullOrEmpty() || array.Length <= index) ? "" : array[index];
	}

	internal static string SubstringBackwardFrom(this string text, string startFrom, bool withoutIt = true)
	{
		if (text != null)
		{
			int num = text.LastIndexOf(startFrom);
			if (num >= 0)
			{
				if (withoutIt)
				{
					return text.Substring(num + startFrom.Length);
				}
				return text.Substring(num);
			}
		}
		return text;
	}

	internal static string SubstringBackwardTo(this string text, string endOn, bool withoutIt = true)
	{
		if (text != null)
		{
			int num = text.LastIndexOf(endOn);
			if (num >= 0)
			{
				if (withoutIt)
				{
					return text.Substring(0, num);
				}
				return text.Substring(num);
			}
		}
		return text;
	}

	internal static string SubstringFrom(this string text, string from, int occuranceCount)
	{
		string text2 = text;
		for (int i = 0; i < occuranceCount; i++)
		{
			text2 = text2.SubstringFrom(from);
		}
		return text2;
	}

	internal static string SubstringFrom(this string text, string startFrom, bool withoutIt = true)
	{
		if (text != null)
		{
			int num = text.IndexOf(startFrom);
			if (num >= 0)
			{
				if (withoutIt)
				{
					return text.Substring(num + startFrom.Length);
				}
				return text.Substring(num);
			}
		}
		return text;
	}

	internal static string SubstringTo(this string text, string to, int occuranceCount)
	{
		string text2 = text;
		string text3 = "";
		for (int i = 0; i < occuranceCount; i++)
		{
			text3 += text2.SubstringTo(to, withoutIt: false);
			text2 = text2.SubstringFrom(to);
		}
		if (text3.Length > 0)
		{
			return text3.Substring(0, text3.Length - 1);
		}
		return text3;
	}

	internal static string SubstringTo(this string text, string endOn, bool withoutIt = true)
	{
		if (text != null)
		{
			int num = text.IndexOf(endOn);
			if (num >= 0)
			{
				if (withoutIt)
				{
					return text.Substring(0, num);
				}
				return text.Substring(0, num + endOn.Length);
			}
		}
		return text;
	}

	internal static string SubstringRemoveLast(this string text)
	{
		return text.NullOrEmpty() ? text : text.Substring(0, text.Length - 1);
	}

	internal static string AsStringUNICODE(this byte[] input)
	{
		return input.AsString(Encoding.UTF8);
	}

	internal static string AsString(this byte[] input, Encoding enc)
	{
		if (input.NullOrEmpty())
		{
			return "";
		}
		return enc.GetString(input);
	}

	internal static byte[] AsBytes(this string text, Encoding enc)
	{
		if (text == null)
		{
			return null;
		}
		if (enc == null)
		{
			return Encoding.Default.GetBytes(text);
		}
		return enc.GetBytes(text);
	}

	internal static string AsBase64(this string text, Encoding enc)
	{
		byte[] inArray = text.AsBytes(enc);
		return Convert.ToBase64String(inArray);
	}

	internal static byte[] Base64ToBytes(this string base64)
	{
		byte[] result = new byte[0];
		if (!string.IsNullOrEmpty(base64))
		{
			try
			{
				result = Convert.FromBase64String(base64);
			}
			catch
			{
			}
		}
		return result;
	}

	internal static string Base64ToString(this string base64, Encoding enc)
	{
		if (base64 == null)
		{
			return null;
		}
		if (enc == null)
		{
			return Encoding.Default.GetString(base64.Base64ToBytes());
		}
		return enc.GetString(base64.Base64ToBytes());
	}

	internal static bool HasMID(string packageId)
	{
		return ModLister.GetModWithIdentifier(packageId)?.Active ?? false;
	}
}
