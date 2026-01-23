using System;

namespace HugsLib.Settings;

/// <summary>
/// A set of useful value constraints for use with SettingHandle
/// </summary>
public static class Validators
{
	public static bool EnumValidator<T>(string value) where T : struct
	{
		return Enum.IsDefined(typeof(T), value);
	}

	public static SettingHandle.ValueIsValid IntRangeValidator(int min, int max)
	{
		int result;
		return (string str) => int.TryParse(str, out result) && result >= min && result <= max;
	}

	public static SettingHandle.ValueIsValid FloatRangeValidator(float min, float max)
	{
		float result;
		return (string str) => float.TryParse(str, out result) && result >= min && result <= max;
	}
}
