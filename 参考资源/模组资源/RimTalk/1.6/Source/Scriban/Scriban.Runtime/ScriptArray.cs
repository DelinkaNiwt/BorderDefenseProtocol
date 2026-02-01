using System;
using System.Collections;
using System.Collections.Generic;
using Scriban.Parsing;

namespace Scriban.Runtime;

public class ScriptArray<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection, IScriptObject where T : class
{
	private List<T> _values;

	private bool _isReadOnly;

	private ScriptObject _script;

	public virtual bool IsReadOnly
	{
		get
		{
			return _isReadOnly;
		}
		set
		{
			if (_script != null)
			{
				_script.IsReadOnly = value;
			}
			_isReadOnly = value;
		}
	}

	public ScriptObject ScriptObject
	{
		get
		{
			ScriptObject scriptObject = _script;
			if (scriptObject == null)
			{
				ScriptObject obj = new ScriptObject
				{
					IsReadOnly = IsReadOnly
				};
				ScriptObject scriptObject2 = obj;
				_script = obj;
				scriptObject = scriptObject2;
			}
			return scriptObject;
		}
	}

	public int Count => _values.Count;

	public virtual T this[int index]
	{
		get
		{
			if (index >= 0 && index < _values.Count)
			{
				return _values[index];
			}
			return null;
		}
		set
		{
			if (index >= 0)
			{
				this.AssertNotReadOnly();
				for (int i = _values.Count; i <= index; i++)
				{
					_values.Add(null);
				}
				_values[index] = value;
			}
		}
	}

	object IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			this[index] = (T)value;
		}
	}

	bool IList.IsFixedSize => ((IList)_values).IsFixedSize;

	bool ICollection.IsSynchronized => ((ICollection)_values).IsSynchronized;

	object ICollection.SyncRoot => ((ICollection)_values).SyncRoot;

	bool IList.IsReadOnly => IsReadOnly;

	bool ICollection<T>.IsReadOnly => IsReadOnly;

	public ScriptArray()
	{
		_values = new List<T>();
	}

	public ScriptArray(int capacity)
	{
		_values = new List<T>(capacity);
	}

	public ScriptArray(IEnumerable<T> values)
	{
		_values = new List<T>(values);
	}

	public ScriptArray(IEnumerable values)
	{
		_values = new List<T>();
		foreach (object value in values)
		{
			_values.Add((T)value);
		}
	}

	public virtual IScriptObject Clone(bool deep)
	{
		ScriptArray scriptArray = (ScriptArray)MemberwiseClone();
		scriptArray._values = new List<object>(_values.Count);
		scriptArray._script = null;
		if (deep)
		{
			foreach (object value in scriptArray._values)
			{
				object item = value;
				if (value is IScriptObject)
				{
					item = ((IScriptObject)value).Clone(deep: true);
				}
				scriptArray._values.Add(item);
			}
			if (_script != null)
			{
				scriptArray._script = (ScriptObject)_script.Clone(deep: true);
			}
		}
		else
		{
			foreach (object value2 in scriptArray._values)
			{
				scriptArray._values.Add(value2);
			}
			if (_script != null)
			{
				scriptArray._script = (ScriptObject)_script.Clone(deep: false);
			}
		}
		return scriptArray;
	}

	public virtual void Add(T item)
	{
		this.AssertNotReadOnly();
		_values.Add(item);
	}

	public void AddRange(IEnumerable<T> items)
	{
		if (items == null)
		{
			throw new ArgumentNullException("items");
		}
		foreach (T item in items)
		{
			Add(item);
		}
	}

	int IList.Add(object value)
	{
		Add((T)value);
		return 0;
	}

	bool IList.Contains(object value)
	{
		return ((IList)_values).Contains(value);
	}

	public virtual void Clear()
	{
		this.AssertNotReadOnly();
		_values.Clear();
	}

	int IList.IndexOf(object value)
	{
		return ((IList)_values).IndexOf(value);
	}

	void IList.Insert(int index, object value)
	{
		Insert(index, (T)value);
	}

	public virtual bool Contains(T item)
	{
		return _values.Contains(item);
	}

	public virtual void CopyTo(T[] array, int arrayIndex)
	{
		_values.CopyTo(array, arrayIndex);
	}

	public virtual int IndexOf(T item)
	{
		return _values.IndexOf(item);
	}

	public virtual void Insert(int index, T item)
	{
		this.AssertNotReadOnly();
		for (int i = _values.Count; i < index; i++)
		{
			_values.Add(null);
		}
		_values.Insert(index, item);
	}

	void IList.Remove(object value)
	{
		Remove((T)value);
	}

	public virtual void RemoveAt(int index)
	{
		this.AssertNotReadOnly();
		if (index >= 0 && index < _values.Count)
		{
			_values.RemoveAt(index);
		}
	}

	public virtual bool Remove(T item)
	{
		this.AssertNotReadOnly();
		return _values.Remove(item);
	}

	public List<T>.Enumerator GetEnumerator()
	{
		return _values.GetEnumerator();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return _values.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _values.GetEnumerator();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_values).CopyTo(array, index);
	}

	public IEnumerable<string> GetMembers()
	{
		yield return "size";
		if (_script == null)
		{
			yield break;
		}
		foreach (string member in _script.GetMembers())
		{
			yield return member;
		}
	}

	public virtual bool Contains(string member)
	{
		return ScriptObject.Contains(member);
	}

	public virtual bool TryGetValue(TemplateContext context, SourceSpan span, string member, out object value)
	{
		if (member == "size")
		{
			value = Count;
			return true;
		}
		return ScriptObject.TryGetValue(context, span, member, out value);
	}

	public virtual bool CanWrite(string member)
	{
		if (member == "size")
		{
			return false;
		}
		return ScriptObject.CanWrite(member);
	}

	public virtual void SetValue(TemplateContext context, SourceSpan span, string member, object value, bool readOnly)
	{
		ScriptObject.SetValue(context, span, member, value, readOnly);
	}

	public virtual bool Remove(string member)
	{
		return ScriptObject.Remove(member);
	}

	public virtual void SetReadOnly(string member, bool readOnly)
	{
		ScriptObject.SetReadOnly(member, readOnly);
	}
}
public class ScriptArray : ScriptArray<object>
{
	public ScriptArray()
	{
	}

	public ScriptArray(int capacity)
		: base(capacity)
	{
	}

	public ScriptArray(IEnumerable values)
		: base(values)
	{
	}

	public ScriptArray(IEnumerable<object> values)
		: base(values)
	{
	}
}
