using UnityEngine;

namespace NiceInventoryTab;

internal static class ColorUtils
{
	public static Color fromHEX(int hex_no_alpha)
	{
		int num = (hex_no_alpha >> 16) & 0xFF;
		int num2 = (hex_no_alpha >> 8) & 0xFF;
		int num3 = hex_no_alpha & 0xFF;
		return new Color((float)num / 255f, (float)num2 / 255f, (float)num3 / 255f);
	}

	public static Color fromHEX(int hex_no_alpha, float alpha)
	{
		int num = (hex_no_alpha >> 16) & 0xFF;
		int num2 = (hex_no_alpha >> 8) & 0xFF;
		int num3 = hex_no_alpha & 0xFF;
		return new Color((float)num / 255f, (float)num2 / 255f, (float)num3 / 255f, alpha);
	}

	public static Color Darker(Color col, float v = 0.5f)
	{
		return Color.Lerp(col, Color.black, 0.5f);
	}

	public static Color ChangeAlpha(Color col, float a)
	{
		col.a = a;
		return col;
	}
}
