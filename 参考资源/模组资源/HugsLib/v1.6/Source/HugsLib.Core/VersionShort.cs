using System;

namespace HugsLib.Core;

/// <summary>
/// A shorter, invariable alternative to System.Version in the format of major.minor.patch
/// Also known as a semantic version number.
/// System.Version can be implicitly cast to this type.
/// VersionShort is no longer used by HugsLib internally, and the type is retained for backwards compatibility.
/// </summary>
public class VersionShort : IComparable, IComparable<VersionShort>, IEquatable<VersionShort>
{
	public const char Separator = '.';

	public readonly int major;

	public readonly int minor;

	public readonly int patch;

	public static VersionShort Parse(string version)
	{
		int[] array = StringToParts(version);
		return new VersionShort(array[0], array[1], array[2]);
	}

	public static VersionShort TryParse(string version)
	{
		try
		{
			return Parse(version);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static implicit operator VersionShort(Version version)
	{
		return new VersionShort(version.Major, version.Minor, version.Build);
	}

	public static bool operator ==(VersionShort v1, VersionShort v2)
	{
		return v1?.Equals(v2) ?? ((object)v2 == null);
	}

	public static bool operator !=(VersionShort v1, VersionShort v2)
	{
		return !(v1 == v2);
	}

	public static bool operator <(VersionShort v1, VersionShort v2)
	{
		if (v1 == null)
		{
			throw new ArgumentNullException("v1");
		}
		return v1.CompareTo(v2) < 0;
	}

	public static bool operator <=(VersionShort v1, VersionShort v2)
	{
		if (v1 == null)
		{
			throw new ArgumentNullException("v1");
		}
		return v1.CompareTo(v2) <= 0;
	}

	public static bool operator >(VersionShort v1, VersionShort v2)
	{
		return v2 < v1;
	}

	public static bool operator >=(VersionShort v1, VersionShort v2)
	{
		return v2 <= v1;
	}

	public static int[] StringToParts(string version)
	{
		if (string.IsNullOrEmpty(version))
		{
			throw new FormatException("Parameter is empty");
		}
		string[] array = version.Split('.');
		if (array.Length < 2 || array.Length > 3)
		{
			throw new FormatException("Version string requires at least 2 and at most 3 parts");
		}
		int[] array2 = new int[3];
		for (int i = 0; i < array.Length; i++)
		{
			if (!int.TryParse(array[i], out var result) || result < 0)
			{
				throw new FormatException("Version contains invalid number");
			}
			if (i > 2)
			{
				break;
			}
			array2[i] = result;
		}
		return array2;
	}

	public VersionShort(int major = 0, int minor = 0, int patch = 0)
	{
		this.major = major;
		this.minor = minor;
		this.patch = patch;
		EnsureValuesAreValid();
	}

	public Version ToVersion()
	{
		return new Version(major, minor, 0, patch);
	}

	public override string ToString()
	{
		return string.Concat(major, '.', minor, '.', patch);
	}

	public override int GetHashCode()
	{
		int num = 1009;
		num = num * 9176 + major;
		num = num * 9176 + minor;
		return num * 9176 + patch;
	}

	public override bool Equals(object obj)
	{
		VersionShort other = obj as VersionShort;
		return Equals(other);
	}

	public bool Equals(VersionShort other)
	{
		if (other == null)
		{
			return false;
		}
		return major == other.major && minor == other.minor && patch == other.patch;
	}

	public int CompareTo(VersionShort ver)
	{
		if (major != ver.major)
		{
			return (major > ver.major) ? 1 : (-1);
		}
		if (minor != ver.minor)
		{
			return (minor > ver.minor) ? 1 : (-1);
		}
		if (patch == ver.patch)
		{
			return 0;
		}
		return (patch > ver.patch) ? 1 : (-1);
	}

	public int CompareTo(object obj)
	{
		if (obj == null)
		{
			return 1;
		}
		VersionShort versionShort = obj as VersionShort;
		if (versionShort == null)
		{
			throw new ArgumentException("Argument must be VersionShort");
		}
		return CompareTo(versionShort);
	}

	private void EnsureValuesAreValid()
	{
		if ((major < 0) | (minor < 0) | (patch < 0))
		{
			throw new FormatException("Invalid version value: " + this);
		}
	}
}
