using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Core;

/// <summary>
/// A base for managers that save data in xml format, to be stored in the save data folder
/// </summary>
public abstract class PersistentDataManager
{
	protected string OverrideFilePath { get; set; }

	internal IModLogger DataManagerLogger { get; set; } = HugsLibController.Logger;

	protected abstract string FileName { get; }

	protected virtual string FolderName => "HugsLib";

	protected virtual bool SuppressLoadSaveExceptions => true;

	protected virtual bool DisplayLoadSaveWarnings => true;

	public static bool IsValidElementName(string tagName)
	{
		try
		{
			XmlConvert.VerifyName(tagName);
			return true;
		}
		catch
		{
			return false;
		}
	}

	protected abstract void LoadFromXml(XDocument xml);

	protected abstract void WriteXml(XDocument xml);

	protected void LoadData()
	{
		string settingsFilePath = GetSettingsFilePath(FileName);
		if (!File.Exists(settingsFilePath))
		{
			return;
		}
		try
		{
			XDocument xml = XDocument.Load(settingsFilePath);
			LoadFromXml(xml);
		}
		catch (Exception ex)
		{
			if (DisplayLoadSaveWarnings)
			{
				DataManagerLogger.Warning("Exception loading xml from " + settingsFilePath + ". Loading defaults instead. Exception was: " + ex);
			}
			if (!SuppressLoadSaveExceptions)
			{
				throw;
			}
		}
	}

	protected void SaveData()
	{
		string settingsFilePath = GetSettingsFilePath(FileName);
		try
		{
			XDocument xDocument = new XDocument();
			WriteXml(xDocument);
			xDocument.Save(settingsFilePath);
		}
		catch (Exception ex)
		{
			if (DisplayLoadSaveWarnings)
			{
				DataManagerLogger.Warning("Failed to save xml to " + settingsFilePath + ". Exception was: " + ex);
			}
			if (!SuppressLoadSaveExceptions)
			{
				throw;
			}
		}
	}

	private string GetSettingsFilePath(string fileName)
	{
		if (OverrideFilePath != null)
		{
			return OverrideFilePath;
		}
		string text = Path.Combine(GenFilePaths.SaveDataFolderPath, FolderName);
		DirectoryInfo directoryInfo = new DirectoryInfo(text);
		if (!directoryInfo.Exists)
		{
			directoryInfo.Create();
		}
		return Path.Combine(text, fileName);
	}
}
