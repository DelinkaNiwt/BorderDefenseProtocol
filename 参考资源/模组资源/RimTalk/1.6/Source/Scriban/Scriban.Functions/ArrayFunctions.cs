using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Scriban.Functions;

public class ArrayFunctions : ScriptObject
{
	private class CycleKey : IEquatable<CycleKey>
	{
		public readonly string Group;

		public CycleKey(string group)
		{
			Group = group;
		}

		public bool Equals(CycleKey other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			return string.Equals(Group, other.Group);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (obj.GetType() != GetType())
			{
				return false;
			}
			return Equals((CycleKey)obj);
		}

		public override int GetHashCode()
		{
			if (Group == null)
			{
				return 0;
			}
			return Group.GetHashCode();
		}

		public override string ToString()
		{
			return "cycle " + Group;
		}
	}

	public static IList Add(IList list, object value)
	{
		if (list == null)
		{
			return new ScriptArray { value };
		}
		list = new ScriptArray(list) { value };
		return list;
	}

	public static IEnumerable AddRange(IEnumerable list1, IEnumerable list2)
	{
		return Concat(list1, list2);
	}

	public static ScriptArray Compact(IEnumerable list)
	{
		if (list == null)
		{
			return null;
		}
		ScriptArray scriptArray = new ScriptArray();
		foreach (object item in list)
		{
			if (item != null)
			{
				scriptArray.Add(item);
			}
		}
		return scriptArray;
	}

	public static IEnumerable Concat(IEnumerable list1, IEnumerable list2)
	{
		if (list2 == null && list1 == null)
		{
			return null;
		}
		if (list2 == null)
		{
			return list1;
		}
		if (list1 == null)
		{
			return list2;
		}
		ScriptArray scriptArray = new ScriptArray(list1);
		foreach (object item in list2)
		{
			scriptArray.Add(item);
		}
		return scriptArray;
	}

	public static object Cycle(TemplateContext context, SourceSpan span, IList list, object group = null)
	{
		if (list == null)
		{
			return null;
		}
		CycleKey key = new CycleKey((group == null) ? Join(context, span, list, ",") : context.ToString(span, group));
		Dictionary<object, object> tags = context.Tags;
		if (!tags.TryGetValue(key, out var value) || !(value is int))
		{
			value = 0;
		}
		int num = (int)value;
		num = ((list.Count != 0) ? (num % list.Count) : 0);
		object result = null;
		if (list.Count > 0)
		{
			result = list[num];
			num++;
		}
		tags[key] = num;
		return result;
	}

	public static object First(IEnumerable list)
	{
		if (list == null)
		{
			return null;
		}
		if (list is IList list2)
		{
			if (list2.Count <= 0)
			{
				return null;
			}
			return list2[0];
		}
		IEnumerator enumerator = list.GetEnumerator();
		try
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
		return null;
	}

	public static IList InsertAt(IList list, int index, object value)
	{
		if (index < 0)
		{
			index = 0;
		}
		list = ((list == null) ? new ScriptArray() : new ScriptArray(list));
		for (int i = list.Count; i < index; i++)
		{
			list.Add(null);
		}
		list.Insert(index, value);
		return list;
	}

	public static string Join(TemplateContext context, SourceSpan span, IEnumerable list, string delimiter)
	{
		if (list == null)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		foreach (object item in list)
		{
			if (flag)
			{
				stringBuilder.Append(delimiter);
			}
			stringBuilder.Append(context.ToString(span, item));
			flag = true;
		}
		return stringBuilder.ToString();
	}

	public static object Last(IEnumerable list)
	{
		if (list == null)
		{
			return null;
		}
		if (list is IList list2)
		{
			if (list2.Count <= 0)
			{
				return null;
			}
			return list2[list2.Count - 1];
		}
		return list.Cast<object>().LastOrDefault();
	}

	public static ScriptArray Limit(IEnumerable list, int count)
	{
		if (list == null)
		{
			return null;
		}
		ScriptArray scriptArray = new ScriptArray();
		foreach (object item in list)
		{
			count--;
			if (count < 0)
			{
				break;
			}
			scriptArray.Add(item);
		}
		return scriptArray;
	}

	public static IEnumerable Map(TemplateContext context, SourceSpan span, object list, string member)
	{
		if (list == null || member == null)
		{
			yield break;
		}
		List<object> list2 = (list as IEnumerable)?.Cast<object>().ToList() ?? new List<object>(1) { list };
		if (list2.Count == 0)
		{
			yield break;
		}
		foreach (object item in list2)
		{
			IObjectAccessor memberAccessor = context.GetMemberAccessor(item);
			if (memberAccessor.HasMember(context, span, item, member))
			{
				memberAccessor.TryGetValue(context, span, item, member, out var value);
				yield return value;
			}
		}
	}

	public static ScriptArray Offset(IEnumerable list, int index)
	{
		if (list == null)
		{
			return null;
		}
		ScriptArray scriptArray = new ScriptArray();
		foreach (object item in list)
		{
			if (index <= 0)
			{
				scriptArray.Add(item);
			}
			else
			{
				index--;
			}
		}
		return scriptArray;
	}

	public static IList RemoveAt(IList list, int index)
	{
		if (list == null)
		{
			return new ScriptArray();
		}
		list = new ScriptArray(list);
		if (index < 0)
		{
			index = list.Count + index;
		}
		if (index >= 0 && index < list.Count)
		{
			list.RemoveAt(index);
		}
		return list;
	}

	public static IEnumerable Reverse(IEnumerable list)
	{
		if (list == null)
		{
			return Enumerable.Empty<object>();
		}
		return list.Cast<object>().Reverse();
	}

	public static int Size(IEnumerable list)
	{
		if (list == null)
		{
			return 0;
		}
		if (list is ICollection collection)
		{
			return collection.Count;
		}
		return list.Cast<object>().Count();
	}

	public static IEnumerable Sort(TemplateContext context, SourceSpan span, object list, string member = null)
	{
		if (list == null)
		{
			return Enumerable.Empty<object>();
		}
		if (!(list is IEnumerable source))
		{
			return new ScriptArray(1) { list };
		}
		List<object> list2 = source.Cast<object>().ToList();
		if (list2.Count == 0)
		{
			return list2;
		}
		if (string.IsNullOrEmpty(member))
		{
			list2.Sort();
		}
		else
		{
			list2.Sort(delegate(object a, object b)
			{
				IObjectAccessor memberAccessor = context.GetMemberAccessor(a);
				IObjectAccessor memberAccessor2 = context.GetMemberAccessor(b);
				object value = null;
				object value2 = null;
				if (!memberAccessor.TryGetValue(context, span, a, member, out value))
				{
					context.TryGetMember?.Invoke(context, span, a, member, out value);
				}
				if (!memberAccessor2.TryGetValue(context, span, b, member, out value2))
				{
					context.TryGetMember?.Invoke(context, span, b, member, out value2);
				}
				return Comparer<object>.Default.Compare(value, value2);
			});
		}
		return list2;
	}

	public static IEnumerable Uniq(IEnumerable list)
	{
		return list?.Cast<object>().Distinct();
	}

	public static bool Contains(IEnumerable list, object item)
	{
		foreach (object item2 in list)
		{
			if (item2 == item || (item2 != null && item2.Equals(item)))
			{
				return true;
			}
		}
		return false;
	}
}
