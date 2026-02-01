using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class ColorCurve
{
	public class Point
	{
		public float key;

		public Color value;
	}

	private Color[] cachedValues = new Color[61];

	public List<Point> points;

	private bool initialized;

	private void Initialize()
	{
		if (points.NullOrEmpty())
		{
			for (int i = 0; i < 61; i++)
			{
				cachedValues[i] = Color.white;
			}
		}
		else
		{
			for (int j = 0; j < 61; j++)
			{
				float key = (float)j / 60f;
				cachedValues[j] = EvaluatePoint(key);
			}
		}
		initialized = true;
	}

	private Color EvaluatePoint(float key)
	{
		Point point = null;
		for (int i = 0; i < points.Count; i++)
		{
			Point point2 = points[i];
			if (key <= point2.key)
			{
				if (point == null)
				{
					return point2.value;
				}
				return Color.Lerp(point.value, point2.value, (key - point.key) / (point2.key - point.key));
			}
			point = point2;
		}
		return point.value;
	}

	public int GetIndex(float key)
	{
		int num = Mathf.FloorToInt(key * 60f);
		if (num > 60)
		{
			return 60;
		}
		if (num < 0)
		{
			return 0;
		}
		return num;
	}

	public Color Evaluate(float key)
	{
		if (!initialized)
		{
			Initialize();
		}
		return cachedValues[GetIndex(key)];
	}
}
