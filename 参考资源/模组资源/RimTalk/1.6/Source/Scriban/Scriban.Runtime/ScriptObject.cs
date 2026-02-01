using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Scriban.Parsing;

namespace Scriban.Runtime;

public class ScriptObject : IDictionary<string, object>, ICollection<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable, IScriptObject, IDictionary, ICollection
{
	internal struct InternalValue
	{
		public object Value { get; }

		public bool IsReadOnly { get; set; }

		public InternalValue(object value, bool isReadOnly)
		{
			Value = value;
			IsReadOnly = isReadOnly;
		}

		public InternalValue(object value)
		{
			this = default(InternalValue);
			Value = value;
		}
	}

	internal Dictionary<string, InternalValue> Store { get; private set; }

	bool IDictionary.IsFixedSize => ((IDictionary)Store).IsFixedSize;

	public int Count => Store.Count;

	bool ICollection.IsSynchronized => ((ICollection)Store).IsSynchronized;

	object ICollection.SyncRoot => ((ICollection)Store).SyncRoot;

	public virtual bool IsReadOnly { get; set; }

	object IDictionary.this[object key]
	{
		get
		{
			return ((IDictionary)Store)[key];
		}
		set
		{
			((IDictionary)Store)[key] = value;
		}
	}

	public virtual object this[string key]
	{
		get
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			TryGetValue(null, default(SourceSpan), key, out var value);
			return value;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			this.AssertNotReadOnly();
			SetValue(null, default(SourceSpan), key, value, readOnly: false);
		}
	}

	public ICollection<string> Keys => Store.Keys;

	ICollection IDictionary.Values => ((IDictionary)Store).Values;

	ICollection IDictionary.Keys => ((IDictionary)Store).Keys;

	public ICollection<object> Values => Store.Values.Select((InternalValue val) => val.Value).ToList();

	bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

	public ScriptObject()
		: this(0)
	{
	}

	public ScriptObject(IEqualityComparer<string> keyComparer)
		: this(0, true, keyComparer)
	{
	}

	public ScriptObject(int capacity)
		: this(capacity, true, null)
	{
	}

	public ScriptObject(int capacity, IEqualityComparer<string> keyComparer)
		: this(capacity, true, keyComparer)
	{
	}

	public ScriptObject(int capacity, bool? autoImportStaticsFromThisType)
		: this(capacity, autoImportStaticsFromThisType, null)
	{
	}

	public ScriptObject(int capacity, bool? autoImportStaticsFromThisType, IEqualityComparer<string> keyComparer)
	{
		Store = new Dictionary<string, InternalValue>(capacity, keyComparer);
		if (GetType() != typeof(ScriptObject) && autoImportStaticsFromThisType == true)
		{
			this.Import(GetType());
		}
	}

	void IDictionary.Add(object key, object value)
	{
		((IDictionary)Store).Add(key, value);
	}

	public void Clear()
	{
		Store.Clear();
	}

	bool IDictionary.Contains(object key)
	{
		return ((IDictionary)Store).Contains(key);
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return ((IDictionary)Store).GetEnumerator();
	}

	void IDictionary.Remove(object key)
	{
		((IDictionary)Store).Remove(key);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)Store).CopyTo(array, index);
	}

	public IEnumerable<string> GetMembers()
	{
		return Store.Keys;
	}

	public virtual bool Contains(string member)
	{
		if (member == null)
		{
			throw new ArgumentNullException("member");
		}
		return Store.ContainsKey(member);
	}

	public virtual bool TryGetValue(TemplateContext context, SourceSpan span, string member, out object value)
	{
		InternalValue value2;
		bool result = Store.TryGetValue(member, out value2);
		value = value2.Value;
		return result;
	}

	public T GetSafeValue<T>(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		object obj = this[name];
		if (obj == null)
		{
			return default(T);
		}
		if (!(obj is T))
		{
			obj = (this[name] = default(T));
		}
		return (T)obj;
	}

	bool IDictionary<string, object>.TryGetValue(string key, out object value)
	{
		return TryGetValue(null, default(SourceSpan), key, out value);
	}

	public virtual bool CanWrite(string member)
	{
		Store.TryGetValue(member, out var value);
		return !value.IsReadOnly;
	}

	public virtual void SetValue(TemplateContext context, SourceSpan span, string member, object value, bool readOnly)
	{
		this.AssertNotReadOnly();
		Store[member] = new InternalValue(value, readOnly);
	}

	public void SetValue(string member, object value, bool readOnly)
	{
		SetValue(null, default(SourceSpan), member, value, readOnly);
	}

	public void Add(string key, object value)
	{
		SetValue(null, default(SourceSpan), key, value, readOnly: false);
	}

	public bool ContainsKey(string key)
	{
		return Contains(key);
	}

	public virtual bool Remove(string member)
	{
		this.AssertNotReadOnly();
		return Store.Remove(member);
	}

	public void SetReadOnly(string member, bool readOnly)
	{
		this.AssertNotReadOnly();
		if (Store.TryGetValue(member, out var value))
		{
			value.IsReadOnly = readOnly;
			Store[member] = value;
		}
	}

	public virtual string ToString(TemplateContext context, SourceSpan span)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{");
		bool flag = true;
		using (IEnumerator<KeyValuePair<string, object>> enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<string, object> current = enumerator.Current;
				if (!flag)
				{
					stringBuilder.Append(", ");
				}
				KeyValuePair<string, object> keyValuePair = current;
				stringBuilder.Append(keyValuePair.Key);
				stringBuilder.Append(": ");
				stringBuilder.Append(context.ToString(span, keyValuePair.Value));
				flag = false;
			}
		}
		stringBuilder.Append("}");
		return stringBuilder.ToString();
	}

	public virtual void CopyTo(ScriptObject dest)
	{
		if (dest == null)
		{
			throw new ArgumentNullException("dest");
		}
		foreach (KeyValuePair<string, InternalValue> item in Store)
		{
			dest.Store[item.Key] = item.Value;
		}
	}

	public virtual IScriptObject Clone(bool deep)
	{
		ScriptObject scriptObject = (ScriptObject)MemberwiseClone();
		scriptObject.Store = new Dictionary<string, InternalValue>(Store.Count);
		if (deep)
		{
			foreach (KeyValuePair<string, InternalValue> item in Store)
			{
				object obj = item.Value.Value;
				if (obj is ScriptObject)
				{
					obj = ((ScriptObject)obj).Clone(deep: true);
				}
				else if (obj is ScriptArray)
				{
					obj = ((ScriptArray)obj).Clone(deep: true);
				}
				scriptObject.Store[item.Key] = new InternalValue(obj, item.Value.IsReadOnly);
			}
		}
		else
		{
			foreach (KeyValuePair<string, InternalValue> item2 in Store)
			{
				scriptObject.Store[item2.Key] = item2.Value;
			}
		}
		return scriptObject;
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		return Store.Select((KeyValuePair<string, InternalValue> item) => new KeyValuePair<string, object>(item.Key, item.Value.Value)).ToList().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	[ScriptMemberIgnore]
	public static ScriptObject From(object obj)
	{
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.Import(obj);
		return scriptObject;
	}

	[ScriptMemberIgnore]
	public static bool IsImportable(object obj)
	{
		if (obj == null)
		{
			return true;
		}
		TypeInfo typeInfo = ((obj as Type) ?? obj.GetType()).GetTypeInfo();
		if (!(obj is string) && !typeInfo.IsPrimitive && !(typeInfo == typeof(decimal).GetTypeInfo()) && !typeInfo.IsEnum)
		{
			return !typeInfo.IsArray;
		}
		return false;
	}

	void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
	{
		throw new NotImplementedException();
	}

	bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
	{
		throw new NotImplementedException();
	}

	void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
	{
		throw new NotImplementedException();
	}

	bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
	{
		throw new NotImplementedException();
	}
}
