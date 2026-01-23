using UnityEngine;
using Verse;

namespace HugsLib.Utils;

/// <summary>
/// A tool to identify the platform the game is running on.
/// </summary>
public static class PlatformUtility
{
	public static PlatformType GetCurrentPlatform()
	{
		if (UnityData.platform == RuntimePlatform.OSXPlayer || UnityData.platform == RuntimePlatform.OSXEditor)
		{
			return PlatformType.MacOSX;
		}
		if (UnityData.platform == RuntimePlatform.WindowsPlayer || UnityData.platform == RuntimePlatform.WindowsEditor)
		{
			return PlatformType.Windows;
		}
		if (UnityData.platform == RuntimePlatform.LinuxPlayer)
		{
			return PlatformType.Linux;
		}
		return PlatformType.Unknown;
	}
}
