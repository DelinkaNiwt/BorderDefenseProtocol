using System;
using System.IO;
using System.Xml.Linq;
using Verse;

namespace HugsLib.Core;

/// <summary>
/// Represents the information stored in the About/Version.xml file. 
/// Since we cannot update the version of the library assembly, we have to store the version externally.
/// </summary>
public class VersionFile
{
	public const string VersionFileDir = "About";

	public const string VersionFileName = "Version.xml";

	public Version OverrideVersion { get; private set; }

	public Version RequiredLibraryVersion { get; private set; }

	public static VersionFile TryParseVersionFile(ModContentPack pack)
	{
		string text = Path.Combine(pack.RootDir, Path.Combine("About", "Version.xml"));
		if (!File.Exists(text))
		{
			return null;
		}
		try
		{
			XDocument doc = XDocument.Load(text);
			return new VersionFile(doc);
		}
		catch (Exception ex)
		{
			HugsLibController.Logger.Error("Exception while parsing version file at path: " + text + " Exception was: " + ex);
		}
		return null;
	}

	private VersionFile(XDocument doc)
	{
		ParseXmlDocument(doc);
	}

	private void ParseXmlDocument(XDocument doc)
	{
		if (doc.Root == null)
		{
			throw new Exception("Missing root node");
		}
		XElement xElement = doc.Root.Element("overrideVersion");
		if (xElement != null)
		{
			OverrideVersion = new Version(xElement.Value);
		}
		XElement xElement2 = doc.Root.Element("requiredLibraryVersion");
		if (xElement2 != null)
		{
			RequiredLibraryVersion = new Version(xElement2.Value);
		}
	}
}
