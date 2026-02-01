using System;
using System.IO;
using Verse;

namespace CharacterEditor;

internal static class FileIO
{
	internal static string PATH_DESKTOP => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

	internal static string PATH_PAWNEX => PATH_DESKTOP + Path.DirectorySeparatorChar + "pawnslots.txt";

	internal static bool ExistsDir(string path)
	{
		return Directory.Exists(path);
	}

	internal static void CheckOrCreateDir(string path)
	{
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
	}

	internal static string[] GetDirFolderList(string path, string searchPattern, bool rekursiv = true)
	{
		if (!Directory.Exists(path))
		{
			return null;
		}
		try
		{
			return Directory.GetDirectories(path, searchPattern, rekursiv ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
		catch
		{
			return null;
		}
	}

	internal static string[] GetDirFileList(string path, string searchPattern, bool rekursiv = true)
	{
		if (!Directory.Exists(path))
		{
			return null;
		}
		try
		{
			return Directory.GetFiles(path, searchPattern, rekursiv ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
		catch
		{
			return null;
		}
	}

	internal static bool WriteFile(string filepath, byte[] bytes)
	{
		try
		{
			using (FileStream fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
			{
				fileStream.Write(bytes, 0, bytes.Length);
				fileStream.Close();
			}
			return true;
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message);
		}
		return false;
	}

	internal static bool Exists(string filepath)
	{
		return File.Exists(filepath);
	}

	internal static byte[] ReadFile(string filepath)
	{
		FileStream fileStream = null;
		try
		{
			fileStream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
			byte[] array = new byte[(int)fileStream.Length];
			fileStream.Read(array, 0, (int)fileStream.Length);
			fileStream.Close();
			return array;
		}
		catch (Exception ex)
		{
			fileStream?.Close();
			Log.Error(ex.Message);
		}
		return new byte[0];
	}
}
