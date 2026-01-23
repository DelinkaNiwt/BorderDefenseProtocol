using System;
using System.IO;
using System.Xml.Linq;
using Verse;

namespace HugsLib.Core;

/// <summary>
/// Provides support for reading version information from Manifest.xml files.
/// These files are used in mods by Fluffy and a a few other authors.
/// </summary>
public class ManifestFile
{
	public const string ManifestFileDir = "About";

	public const string ManifestFileName = "Manifest.xml";

	public Version Version { get; private set; }

	/// <summary>
	/// Attempts to read and parse the manifest file for a mod.
	/// </summary>
	/// <returns>
	/// Returns null if reading or parsing fails for any reason.
	/// </returns>
	public static ManifestFile TryParse(ModContentPack pack, bool logError = true)
	{
		if (pack != null)
		{
			try
			{
				return Parse(pack);
			}
			catch (Exception arg)
			{
				if (logError)
				{
					HugsLibController.Logger.Error("Exception while parsing manifest file:\npackageId:" + pack.PackageIdPlayerFacing + ", " + $"path:{GetManifestFilePath(pack)}, exception:{arg}");
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Reads and parses the manifest file for a mod.
	/// </summary>
	/// <returns>
	/// Returns null if the file does not exist.
	/// </returns>
	public static ManifestFile Parse(ModContentPack pack)
	{
		if (pack == null)
		{
			throw new ArgumentNullException("pack");
		}
		string manifestFilePath = GetManifestFilePath(pack);
		if (File.Exists(manifestFilePath))
		{
			XDocument doc = XDocument.Load(manifestFilePath);
			return new ManifestFile(doc);
		}
		return null;
	}

	private static string GetManifestFilePath(ModContentPack pack)
	{
		return Path.Combine(pack.RootDir, Path.Combine("About", "Manifest.xml"));
	}

	private ManifestFile(XDocument doc)
	{
		ParseXmlDocument(doc);
	}

	private void ParseXmlDocument(XDocument doc)
	{
		if (doc.Root == null)
		{
			throw new Exception("Missing root node.");
		}
		XElement xElement = doc.Root.Element("version") ?? doc.Root.Element("Version");
		if (xElement != null)
		{
			try
			{
				Version = Version.Parse(xElement.Value);
			}
			catch (Exception innerException)
			{
				throw new Exception("Failed to parse version tag.", innerException);
			}
		}
	}
}
