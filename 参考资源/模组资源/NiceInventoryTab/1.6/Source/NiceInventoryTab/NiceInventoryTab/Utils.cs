using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class Utils
{
	public static bool InDevMode
	{
		get
		{
			if (Prefs.DevMode)
			{
				return Current.ProgramState == ProgramState.Playing;
			}
			return false;
		}
	}

	public static float Snap(float value, float step)
	{
		return Mathf.Round(value / step) * step;
	}

	public static void Animate(ref float Value, float DesiredValue, float Rate = 0.3f)
	{
		Value += (DesiredValue - Value) * Rate;
	}

	public static float CalcWidth(string label)
	{
		return Text.CalcSize(label).x;
	}

	public static (Rect left, Rect right) SplitRectByLeftPart(Rect org, float leftPart_pix, float gap = 0f)
	{
		Rect item = new Rect(org.x, org.y, leftPart_pix, org.height);
		return new ValueTuple<Rect, Rect>(item2: new Rect(item.xMax + gap, org.y, org.xMax - (item.xMax + gap), org.height), item1: item);
	}

	public static (Rect left, Rect right) SplitRectByRightPart(Rect org, float rightPart_pix, float gap = 0f)
	{
		Rect item = new Rect(org.xMax - rightPart_pix, org.y, rightPart_pix, org.height);
		return (left: new Rect(org.x, org.y, item.x - gap - org.x, org.height), right: item);
	}

	public static (Rect top, Rect bottom) SplitRectByTopPart(Rect org, float topPart_pix, float gap = 0f)
	{
		Rect item = new Rect(org.x, org.y, org.width, topPart_pix);
		return new ValueTuple<Rect, Rect>(item2: new Rect(org.x, item.yMax + gap, org.width, org.yMax - (item.yMax + gap)), item1: item);
	}

	public static (Rect top, Rect bottom) SplitRectByBottomPart(Rect org, float bottomPart_pix, float gap = 0f)
	{
		Rect item = new Rect(org.x, org.yMax - bottomPart_pix, org.width, bottomPart_pix);
		return (top: new Rect(org.x, org.y, org.width, item.y - gap - org.y), bottom: item);
	}

	public static (Rect left, Rect right) SplitRect(Rect org, float ratio, float gap = 0f)
	{
		float num = gap / 2f;
		Rect item = new Rect(org.x, org.y, org.width * ratio - num, org.height);
		return new ValueTuple<Rect, Rect>(item2: new Rect(item.xMax + gap, org.y, org.xMax - (item.xMax + gap), org.height), item1: item);
	}

	public static (Rect top, Rect bottom) SplitRectVertical(Rect org, float ratio, float gap = 0f)
	{
		float num = gap / 2f;
		Rect item = new Rect(org.x, org.y, org.width, org.height * ratio - num);
		return new ValueTuple<Rect, Rect>(item2: new Rect(org.x, item.yMax + num, org.width, org.yMax - (item.yMax + num)), item1: item);
	}

	public static Rect RectCentered(Vector2 v, float size)
	{
		return RectCentered(v.x, v.y, size);
	}

	public static Rect RectCentered(float x, float y, float size)
	{
		return new Rect(x - size / 2f, y - size / 2f, size, size);
	}

	public static Rect RectCentered(float x, float y, float width, float height)
	{
		return new Rect(x - width / 2f, y - height / 2f, width, height);
	}

	public static Rect RectMove(Rect r, float x = 0f, float y = 0f)
	{
		return new Rect(r.x + x, r.y + y, r.width, r.height);
	}

	public static Rect RectMoveCenter(Rect r, float final_x, float final_y)
	{
		float width = r.width;
		float height = r.height;
		float x = final_x - width / 2f;
		float y = final_y - height / 2f;
		return new Rect(x, y, width, height);
	}

	public static Rect CombineRects(Rect a, Rect b)
	{
		float xmin = Mathf.Min(a.xMin, b.xMin);
		float ymin = Mathf.Min(a.yMin, b.yMin);
		float xmax = Mathf.Max(a.xMax, b.xMax);
		float ymax = Mathf.Max(a.yMax, b.yMax);
		return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
	}

	public static void StickValue(ref float value, float reference, float gap)
	{
		if (Mathf.Abs(value - reference) < gap)
		{
			value = reference;
		}
	}

	public static string TruncateToFit(string text, float maxWidth, string end = null)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		float num = (end.NullOrEmpty() ? 0f : Text.CalcSize(end).x);
		float num2 = maxWidth - num;
		if (num2 <= 0f)
		{
			return end;
		}
		if (Text.CalcSize(text).x <= maxWidth)
		{
			return text;
		}
		int num3 = 0;
		int num4 = text.Length;
		while (num3 < num4)
		{
			int num5 = (num3 + num4 + 1) / 2;
			if (Text.CalcSize(text.Substring(0, num5)).x <= num2)
			{
				num3 = num5;
			}
			else
			{
				num4 = num5 - 1;
			}
		}
		return text.Substring(0, num3) + end;
	}

	public static Type ReflectType(Assembly ass, string className)
	{
		return ass.GetType(className);
	}

	public static FieldInfo ReflectField(Assembly ass, string className, string fieldName, BindingFlags bf = BindingFlags.Instance | BindingFlags.Public)
	{
		Type type = ReflectType(ass, className);
		if (type == null)
		{
			return null;
		}
		return type.GetField(fieldName, bf);
	}

	public static FieldInfo ReflectField(Assembly ass, Type type, string fieldName, BindingFlags bf = BindingFlags.Instance | BindingFlags.Public)
	{
		if (type == null)
		{
			return null;
		}
		return type.GetField(fieldName, bf);
	}

	public static T ReflectValue<T>(Assembly ass, string className, string fieldName, object instance, BindingFlags bf = BindingFlags.Instance | BindingFlags.Public)
	{
		FieldInfo fieldInfo = ReflectField(ass, className, fieldName, bf);
		if (fieldInfo == null)
		{
			return default(T);
		}
		return (T)fieldInfo.GetValue(instance);
	}

	public static T ReflectPropertyValue<T>(Assembly ass, string className, string propertyName, BindingFlags bf = BindingFlags.Instance | BindingFlags.Public)
	{
		Type type = ReflectType(ass, className);
		if (type == null)
		{
			return default(T);
		}
		PropertyInfo property = type.GetProperty(propertyName, bf);
		if (property == null)
		{
			return default(T);
		}
		return (T)property.GetValue(bf.HasFlag(BindingFlags.Static) ? null : Activator.CreateInstance(type));
	}

	public static MethodInfo ReflectMethod(Assembly ass, string className, string methodName, Type[] parameterTypes = null, BindingFlags bf = BindingFlags.Instance | BindingFlags.Public)
	{
		Type type = ReflectType(ass, className);
		if (type == null)
		{
			return null;
		}
		if (parameterTypes == null)
		{
			return type.GetMethod(methodName, bf);
		}
		return type.GetMethod(methodName, bf, null, parameterTypes, null);
	}

	public static object InvokeMethod(Assembly ass, string className, string methodName, object instance, object[] parameters = null, BindingFlags bf = BindingFlags.Instance | BindingFlags.Public)
	{
		Type[] parameterTypes = parameters?.Select((object p) => p?.GetType() ?? typeof(object)).ToArray() ?? Type.EmptyTypes;
		MethodInfo methodInfo = ReflectMethod(ass, className, methodName, parameterTypes, bf);
		if (methodInfo == null)
		{
			return null;
		}
		return methodInfo.Invoke(instance, parameters);
	}
}
