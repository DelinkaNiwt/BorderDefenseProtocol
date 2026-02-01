using System;
using System.Collections.Generic;

namespace NiceInventoryTab;

public class FloatRef
{
	public float Value;

	public float MinValue;

	private static readonly List<WeakReference<FloatRef>> RegisteredFloatRef = new List<WeakReference<FloatRef>>();

	public FloatRef(float value = 0f)
	{
		Value = value;
		RegisteredFloatRef.Add(new WeakReference<FloatRef>(this));
	}

	public static implicit operator float(FloatRef r)
	{
		return r?.Value ?? 0f;
	}

	public static implicit operator FloatRef(float v)
	{
		return new FloatRef(v);
	}

	public static void ClearValues()
	{
		for (int num = RegisteredFloatRef.Count - 1; num >= 0; num--)
		{
			if (RegisteredFloatRef[num].TryGetTarget(out var target))
			{
				target.Reset();
			}
			else
			{
				RegisteredFloatRef.RemoveAt(num);
			}
		}
	}

	public float Comp(float v)
	{
		if (v > Value)
		{
			Value = v;
			return Value;
		}
		return Value;
	}

	public bool CompCheck(float v)
	{
		if (v > Value)
		{
			Value = v;
			return true;
		}
		return false;
	}

	public void Reset()
	{
		Value = MinValue;
	}

	~FloatRef()
	{
	}
}
