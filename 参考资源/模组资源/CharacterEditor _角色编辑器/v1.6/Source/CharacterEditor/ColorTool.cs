using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterEditor;

internal static class ColorTool
{
	internal static readonly Color colWhite = new Color(1f, 1f, 1f);

	internal static readonly Color colLightGray = new Color(0.82f, 0.824f, 0.831f);

	internal static readonly Color colGray = new Color(0.714f, 0.718f, 0.733f);

	internal static readonly Color colDarkGray = new Color(0.506f, 0.51f, 0.525f);

	internal static readonly Color colGraphite = new Color(0.345f, 0.345f, 0.353f);

	internal static readonly Color colDimGray = new Color(0.245f, 0.245f, 0.245f);

	internal static readonly Color colDarkDimGray = new Color(0.175f, 0.175f, 0.175f);

	internal static readonly Color colAsche = new Color(0.115f, 0.115f, 0.115f);

	internal static readonly Color colBlack = new Color(0f, 0f, 0f);

	internal static readonly Color colNavyBlue = new Color(0f, 0.082f, 0.267f);

	internal static readonly Color colDarkBlue = new Color(0.137f, 0.235f, 0.486f);

	internal static readonly Color colRoyalBlue = new Color(0.157f, 0.376f, 0.678f);

	internal static readonly Color colBlue = new Color(0.004f, 0.42f, 0.718f);

	internal static readonly Color colPureBlue = new Color(0f, 0f, 1f);

	internal static readonly Color colLightBlue = new Color(0.129f, 0.569f, 0.816f);

	internal static readonly Color colSkyBlue = new Color(0.58f, 0.757f, 0.91f);

	internal static readonly Color colMaroon = new Color(0.373f, 0f, 0.125f);

	internal static readonly Color colBurgundy = new Color(0.478f, 0.153f, 0.255f);

	internal static readonly Color colDarkRed = new Color(0.545f, 0f, 0f);

	internal static readonly Color colRed = new Color(0.624f, 0.039f, 0.055f);

	internal static readonly Color colPureRed = new Color(1f, 0f, 0f);

	internal static readonly Color colLightRed = new Color(0.784f, 0.106f, 0.216f);

	internal static readonly Color colHotPink = new Color(0.863f, 0.345f, 0.631f);

	internal static readonly Color colPink = new Color(0.969f, 0.678f, 0.808f);

	internal static readonly Color colDarkPurple = new Color(0.251f, 0.157f, 0.384f);

	internal static readonly Color colPurple = new Color(0.341f, 0.176f, 0.561f);

	internal static readonly Color colLightPurple = new Color(0.631f, 0.576f, 0.784f);

	internal static readonly Color colTeal = new Color(0.11f, 0.576f, 0.592f);

	internal static readonly Color colTurquoise = new Color(0.027f, 0.51f, 0.58f);

	internal static readonly Color colDarkBrown = new Color(0.282f, 0.2f, 0.125f);

	internal static readonly Color colBrown = new Color(0.388f, 0.204f, 0.102f);

	internal static readonly Color colLightBrown = new Color(0.58f, 0.353f, 0.196f);

	internal static readonly Color colTawny = new Color(0.784f, 0.329f, 0.098f);

	internal static readonly Color colBlaze = new Color(0.941f, 0.29f, 0.141f);

	internal static readonly Color colOrange = new Color(0.949f, 0.369f, 0.133f);

	internal static readonly Color colLightOrange = new Color(0.973f, 0.58f, 0.133f);

	internal static readonly Color colGold = new Color(0.824f, 0.624f, 0.055f);

	internal static readonly Color colYellowGold = new Color(1f, 0.761f, 0.051f);

	internal static readonly Color colYellow = new Color(1f, 0.859f, 0.004f);

	internal static readonly Color colDarkYellow = new Color(0.953f, 0.886f, 0.227f);

	internal static readonly Color colChartreuse = new Color(0.922f, 0.91f, 0.067f);

	internal static readonly Color colLightYellow = new Color(1f, 0.91f, 0.51f);

	internal static readonly Color colDarkGreen = new Color(0f, 0.345f, 0.149f);

	internal static readonly Color colGreen = new Color(0.137f, 0.663f, 0.29f);

	internal static readonly Color colPureGreen = new Color(0f, 1f, 0f);

	internal static readonly Color colLimeGreen = new Color(0.682f, 0.82f, 0.208f);

	internal static readonly Color colLightGreen = new Color(0.541f, 0.769f, 0.537f);

	internal static readonly Color colDarkOlive = new Color(0.255f, 0.282f, 0.149f);

	internal static readonly Color colOlive = new Color(0.451f, 0.463f, 0.294f);

	internal static readonly Color colOliveDrab = new Color(0.357f, 0.337f, 0.263f);

	internal static readonly Color colFoilageGreen = new Color(0.482f, 0.498f, 0.443f);

	internal static readonly Color colTan = new Color(0.718f, 0.631f, 0.486f);

	internal static readonly Color colBeige = new Color(0.827f, 0.741f, 0.545f);

	internal static readonly Color colKhaki = new Color(0.933f, 0.835f, 0.678f);

	internal static readonly Color colPeach = new Color(0.996f, 0.859f, 0.733f);

	internal static float offsetCX = 0f;

	internal static List<Color> lcolors;

	internal static int IMAX = 1;

	internal static int IMAXB = 255;

	internal static float FMAX = 1f;

	internal static float FMAXB = 255f;

	internal static double DMAX = 1.0;

	internal static double DMAXB = 255.0;

	internal static List<Color> ListOfColors
	{
		get
		{
			if (lcolors == null)
			{
				lcolors = new List<Color>
				{
					GetDerivedColor(colWhite, offsetCX),
					GetDerivedColor(colLightGray, offsetCX),
					GetDerivedColor(colGray, offsetCX),
					GetDerivedColor(colDarkGray, offsetCX),
					GetDerivedColor(colGraphite, offsetCX),
					GetDerivedColor(colBlack, offsetCX),
					GetDerivedColor(colNavyBlue, offsetCX),
					GetDerivedColor(colDarkBlue, offsetCX),
					GetDerivedColor(colRoyalBlue, offsetCX),
					GetDerivedColor(colBlue, offsetCX),
					GetDerivedColor(colPureBlue, offsetCX),
					GetDerivedColor(colLightBlue, offsetCX),
					GetDerivedColor(colSkyBlue, offsetCX),
					GetDerivedColor(colMaroon, offsetCX),
					GetDerivedColor(colBurgundy, offsetCX),
					GetDerivedColor(colDarkRed, offsetCX),
					GetDerivedColor(colRed, offsetCX),
					GetDerivedColor(colPureRed, offsetCX),
					GetDerivedColor(colLightRed, offsetCX),
					GetDerivedColor(colHotPink, offsetCX),
					GetDerivedColor(colPink, offsetCX),
					GetDerivedColor(colDarkPurple, offsetCX),
					GetDerivedColor(colPurple, offsetCX),
					GetDerivedColor(colLightPurple, offsetCX),
					GetDerivedColor(colTeal, offsetCX),
					GetDerivedColor(colTurquoise, offsetCX),
					GetDerivedColor(colDarkBrown, offsetCX),
					GetDerivedColor(colBrown, offsetCX),
					GetDerivedColor(colLightBrown, offsetCX),
					GetDerivedColor(colTawny, offsetCX),
					GetDerivedColor(colBlaze, offsetCX),
					GetDerivedColor(colOrange, offsetCX),
					GetDerivedColor(colLightOrange, offsetCX),
					GetDerivedColor(colGold, offsetCX),
					GetDerivedColor(colYellowGold, offsetCX),
					GetDerivedColor(colYellow, offsetCX),
					GetDerivedColor(colDarkYellow, offsetCX),
					GetDerivedColor(colChartreuse, offsetCX),
					GetDerivedColor(colLightYellow, offsetCX),
					GetDerivedColor(colDarkGreen, offsetCX),
					GetDerivedColor(colGreen, offsetCX),
					GetDerivedColor(colPureGreen, offsetCX),
					GetDerivedColor(colLimeGreen, offsetCX),
					GetDerivedColor(colLightGreen, offsetCX),
					GetDerivedColor(colDarkOlive, offsetCX),
					GetDerivedColor(colOlive, offsetCX),
					GetDerivedColor(colOliveDrab, offsetCX),
					GetDerivedColor(colFoilageGreen, offsetCX),
					GetDerivedColor(colTan, offsetCX),
					GetDerivedColor(colBeige, offsetCX),
					GetDerivedColor(colKhaki, offsetCX),
					GetDerivedColor(colPeach, offsetCX)
				};
			}
			return lcolors;
		}
	}

	internal static Color RandomColor => GetRandomColor(0f, FMAX);

	internal static Color RandomAlphaColor => GetRandomColor(0f, FMAX, andAlpha: true);

	internal static string ColorHexString(this Color c)
	{
		double num = DMAX / DMAXB;
		int num2 = (int)((double)c.r / num);
		int num3 = (int)((double)c.g / num);
		int num4 = (int)((double)c.b / num);
		int num5 = (int)((double)c.a / num);
		string text = num2.ToString("X2");
		text = text + "-" + num3.ToString("X2");
		text = text + "-" + num4.ToString("X2");
		return text + "-" + num5.ToString("X2");
	}

	internal static string NullableColorHexString(this Color? c)
	{
		return (!c.HasValue) ? "" : c.Value.ColorHexString();
	}

	internal static Color? HexStringToColorNullable(this string hex)
	{
		if (string.IsNullOrEmpty(hex))
		{
			return null;
		}
		Color value = default(Color);
		string[] array = hex.SplitNo("-");
		if (array.Length >= 4)
		{
			try
			{
				double num = DMAX / DMAXB;
				int num2 = Convert.ToInt32(array[0], 16);
				int num3 = Convert.ToInt32(array[1], 16);
				int num4 = Convert.ToInt32(array[2], 16);
				int num5 = Convert.ToInt32(array[3], 16);
				value.r = (float)(num * (double)num2);
				value.g = (float)(num * (double)num3);
				value.b = (float)(num * (double)num4);
				value.a = (float)(num * (double)num5);
			}
			catch
			{
			}
		}
		return value;
	}

	internal static Color HexStringToColor(this string hex)
	{
		if (string.IsNullOrEmpty(hex))
		{
			return Color.white;
		}
		Color result = default(Color);
		string[] array = hex.SplitNo("-");
		if (array.Length >= 4)
		{
			try
			{
				double num = DMAX / DMAXB;
				int num2 = Convert.ToInt32(array[0], 16);
				int num3 = Convert.ToInt32(array[1], 16);
				int num4 = Convert.ToInt32(array[2], 16);
				int num5 = Convert.ToInt32(array[3], 16);
				result.r = (float)(num * (double)num2);
				result.g = (float)(num * (double)num3);
				result.b = (float)(num * (double)num4);
				result.a = (float)(num * (double)num5);
			}
			catch
			{
			}
		}
		return result;
	}

	internal static Color GetDerivedColor(Color color, float offset)
	{
		float num = color.r - offset;
		float num2 = color.g - offset;
		float num3 = color.b - offset;
		num = ((num < 0f) ? 0f : num);
		num2 = ((num2 < 0f) ? 0f : num2);
		num3 = ((num3 < 0f) ? 0f : num3);
		num = ((num > (float)IMAX) ? ((float)IMAX) : num);
		num2 = ((num2 > (float)IMAX) ? ((float)IMAX) : num2);
		num3 = ((num3 > (float)IMAX) ? ((float)IMAX) : num3);
		return new Color(num, num2, num3, color.a);
	}

	internal static Color GetRandomColor(float minbright = 0f, float maxbright = 1f, bool andAlpha = false)
	{
		if (maxbright < minbright)
		{
			maxbright = minbright;
		}
		double num = DMAX / DMAXB;
		int num2 = (int)((double)minbright / num);
		int num3 = (int)((double)maxbright / num);
		int num4 = (int)(0.5 / num);
		int num5 = (int)(1.0 / num);
		if (num2 > num3)
		{
			num2 = num3 - 1;
		}
		if (num2 < 0)
		{
			num2 = 0;
		}
		int num6 = ((num2 == num3) ? num2 : CEditor.zufallswert.Next(num2, num3));
		int num7 = ((num2 == num3) ? num2 : CEditor.zufallswert.Next(num2, num3));
		int num8 = ((num2 == num3) ? num2 : CEditor.zufallswert.Next(num2, num3));
		int num9 = ((num4 == num5) ? num4 : CEditor.zufallswert.Next(num4, num5));
		float num10 = (float)(num * (double)num9);
		return new Color((float)(num * (double)num6), (float)(num * (double)num7), (float)(num * (double)num8), andAlpha ? num10 : 1f);
	}
}
