using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RimTalk.Util;
using Verse;

namespace RimTalk.Prompt;

public static class PresetSerializer
{
	public static string ExportToJson(PromptPreset preset)
	{
		if (preset == null)
		{
			return null;
		}
		try
		{
			PresetDto dto = PresetDto.FromPreset(preset);
			return JsonUtil.SerializeToJson(dto);
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to export preset to JSON: " + ex.Message);
			return null;
		}
	}

	public static PromptPreset ImportFromJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}
		try
		{
			PresetDto dto = JsonUtil.DeserializeFromJson<PresetDto>(json);
			if (dto == null)
			{
				return null;
			}
			PromptPreset preset = dto.ToPreset();
			Logger.Debug($"Successfully imported preset: {preset.Name} with {preset.Entries.Count} entries");
			return preset;
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to import preset: " + ex.Message);
			return null;
		}
	}

	public static string GetExportDirectory()
	{
		string path = Path.Combine(GenFilePaths.ConfigFolderPath, "RimTalk", "Presets");
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		return path;
	}

	public static bool ExportToFile(PromptPreset preset, string filename = null)
	{
		try
		{
			string json = ExportToJson(preset);
			if (json == null)
			{
				return false;
			}
			if (string.IsNullOrEmpty(filename))
			{
				filename = SanitizeFilename(preset.Name);
			}
			string path = Path.Combine(GetExportDirectory(), filename + ".json");
			File.WriteAllText(path, json, Encoding.UTF8);
			Logger.Debug("Exported preset to: " + path);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to export preset: " + ex.Message);
			return false;
		}
	}

	public static PromptPreset ImportFromFile(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				Logger.Warning("Preset file not found: " + path);
				return null;
			}
			string json = File.ReadAllText(path, Encoding.UTF8);
			return ImportFromJson(json);
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to import preset: " + ex.Message);
			return null;
		}
	}

	public static List<string> GetAvailablePresetFiles()
	{
		string dir = GetExportDirectory();
		if (!Directory.Exists(dir))
		{
			return new List<string>();
		}
		return (from f in Directory.GetFiles(dir, "*.json")
			orderby Path.GetFileName(f)
			select f).ToList();
	}

	private static string SanitizeFilename(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "preset";
		}
		char[] invalidChars = Path.GetInvalidFileNameChars();
		StringBuilder sb = new StringBuilder();
		foreach (char c in name)
		{
			if (!invalidChars.Contains(c))
			{
				sb.Append(c);
			}
			else
			{
				sb.Append('_');
			}
		}
		return sb.ToString();
	}
}
