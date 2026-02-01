using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

[StaticConstructorOnStartup]
public static class AnimationUtility
{
	public static readonly Func<float, float> Linear;

	public static readonly Func<float, float> Floor;

	public static readonly Func<float, float> Ceil;

	public static readonly Func<float, float> Round;

	public static readonly Func<float, float> FadeOutLinear;

	public static readonly Func<float, float> FadeOutQuad;

	public static readonly Func<float, float> FadeOutCubic;

	public static readonly Func<float, float> Sine;

	public static readonly Func<float, float> Cosine;

	public static readonly Func<float, float> Tangent;

	public static readonly Func<float, float> InverseSine;

	public static readonly Func<float, float> UnsignedSine;

	public static readonly Func<float, float> EaseInSine;

	public static readonly Func<float, float> EaseOutSine;

	public static readonly Func<float, float> EaseInOutSine;

	public static readonly Func<float, float> EaseInQuad;

	public static readonly Func<float, float> EaseOutQuad;

	public static readonly Func<float, float> EaseInOutQuad;

	public static readonly Func<float, float> EaseOutInQuad;

	public static readonly Func<float, float> EaseInCubic;

	public static readonly Func<float, float> EaseOutCubic;

	public static readonly Func<float, float> EaseInOutCubic;

	public static readonly Func<float, float> EaseOutInCubic;

	public static readonly Func<float, float> Burst;

	public static readonly Func<float, float> InverseBurst;

	public static readonly Func<float, float> ReverseBurst;

	private static Dictionary<string, Func<float, float>> functionsByName;

	public static void RegisterFunction(string name, Func<float, float> function)
	{
		if (name.NullOrEmpty())
		{
			Log.Error("(NCL Projectiles) Error: Received an attempt to register an animation function with a null or empty name");
			return;
		}
		if (functionsByName.ContainsKey(name))
		{
			Log.Warning("(NCL Projectiles) Warning: Ignoring an attempt to override an animation function with key " + name + ".");
		}
		functionsByName[name] = function;
	}

	public static Func<float, float> GetFunctionByName(string name, Func<float, float> defaultValue = null)
	{
		if (name.NullOrEmpty())
		{
			return defaultValue;
		}
		if (name == "None")
		{
			return null;
		}
		if (functionsByName.TryGetValue(name, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	static AnimationUtility()
	{
		Linear = (float x) => x;
		Floor = (float x) => 0f;
		Ceil = (float x) => 1f;
		Round = (float x) => (x >= 0.5f) ? 1f : 0f;
		FadeOutLinear = (float x) => 1f - x;
		FadeOutQuad = (float x) => 1f - x * x;
		FadeOutCubic = (float x) => 1f - x * x * x;
		Sine = (float x) => Mathf.Sin(x * (float)Math.PI);
		Cosine = (float x) => Mathf.Cos(x * (float)Math.PI);
		Tangent = (float x) => Mathf.Tan(x * (float)Math.PI);
		InverseSine = (float x) => 1f - Sine(x);
		UnsignedSine = (float x) => Mathf.Sin(2f * x * (float)Math.PI);
		EaseInSine = (float x) => 1f - Mathf.Cos(x * (float)Math.PI / 2f);
		EaseOutSine = (float x) => Mathf.Sin(x * (float)Math.PI / 2f);
		EaseInOutSine = (float x) => (0f - (Mathf.Cos((float)Math.PI * x) - 1f)) / 2f;
		EaseInQuad = (float x) => x * x;
		EaseOutQuad = (float x) => 1f - (1f - x) * (1f - x);
		EaseInOutQuad = (float x) => ((double)x >= 0.5) ? (1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f) : (2f * x * x);
		EaseOutInQuad = (float x) => ((double)x >= 0.5) ? (0.5f + 0.5f * EaseInQuad(2f * (x - 0.5f))) : (0.5f * EaseOutQuad(2f * x));
		EaseInCubic = (float x) => x * x * x;
		EaseOutCubic = (float x) => 1f - Mathf.Pow(1f - x, 3f);
		EaseInOutCubic = (float x) => ((double)x >= 0.5) ? (1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f) : (4f * x * x * x);
		EaseOutInCubic = (float x) => ((double)x >= 0.5) ? (0.5f + 0.5f * EaseInCubic(2f * (x - 0.5f))) : (0.5f * EaseOutCubic(2f * x));
		Burst = (float x) => Sine(EaseOutCubic(x));
		InverseBurst = (float x) => 1f - Burst(x);
		ReverseBurst = (float x) => Sine(EaseInCubic(x));
		functionsByName = new Dictionary<string, Func<float, float>>();
		RegisterFunction("Linear", Linear);
		RegisterFunction("Floor", Floor);
		RegisterFunction("Ceil", Ceil);
		RegisterFunction("Round", Round);
		RegisterFunction("FadeOutLinear", FadeOutLinear);
		RegisterFunction("FadeOutQuad", FadeOutQuad);
		RegisterFunction("FadeOutCubic", FadeOutCubic);
		RegisterFunction("Sine", Sine);
		RegisterFunction("Cosine", Cosine);
		RegisterFunction("Tangent", Tangent);
		RegisterFunction("InverseSine", InverseSine);
		RegisterFunction("UnsignedSine", UnsignedSine);
		RegisterFunction("EaseInSine", EaseInSine);
		RegisterFunction("EaseOutSine", EaseOutSine);
		RegisterFunction("EaseInOutSine", EaseInOutSine);
		RegisterFunction("EaseInQuad", EaseInQuad);
		RegisterFunction("EaseOutQuad", EaseOutQuad);
		RegisterFunction("EaseInOutQuad", EaseInOutQuad);
		RegisterFunction("EaseOutInQuad", EaseOutInQuad);
		RegisterFunction("EaseInCubic", EaseInCubic);
		RegisterFunction("EaseOutCubic", EaseOutCubic);
		RegisterFunction("EaseInOutCubic", EaseInOutCubic);
		RegisterFunction("EaseOutInCubic", EaseOutInCubic);
		RegisterFunction("Burst", Burst);
		RegisterFunction("InverseBurst", InverseBurst);
		RegisterFunction("ReverseBurst", ReverseBurst);
	}
}
