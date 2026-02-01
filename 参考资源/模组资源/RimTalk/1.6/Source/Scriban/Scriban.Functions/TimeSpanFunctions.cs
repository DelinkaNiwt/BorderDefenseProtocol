using System;
using Scriban.Runtime;

namespace Scriban.Functions;

public class TimeSpanFunctions : ScriptObject
{
	public static TimeSpan Zero => TimeSpan.Zero;

	public static TimeSpan FromDays(double days)
	{
		return TimeSpan.FromDays(days);
	}

	public static TimeSpan FromHours(double hours)
	{
		return TimeSpan.FromHours(hours);
	}

	public static TimeSpan FromMinutes(double minutes)
	{
		return TimeSpan.FromMinutes(minutes);
	}

	public static TimeSpan FromSeconds(double seconds)
	{
		return TimeSpan.FromSeconds(seconds);
	}

	public static TimeSpan FromMilliseconds(double millis)
	{
		return TimeSpan.FromMilliseconds(millis);
	}

	public static TimeSpan Parse(string text)
	{
		return TimeSpan.Parse(text);
	}
}
