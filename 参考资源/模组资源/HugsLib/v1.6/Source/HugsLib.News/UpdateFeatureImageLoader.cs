using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace HugsLib.News;

[StaticConstructorOnStartup]
internal static class UpdateFeatureImageLoader
{
	private const string UpdateFeatureImageBaseFolder = "News/";

	private static readonly string[] PossibleTextureFileExtensions = new string[4] { ".png", ".jpg", ".jpeg", ".psd" };

	private static readonly Texture2D missingTexturePlaceholder = ContentFinder<Texture2D>.Get(BaseContent.BadTexPath);

	public static IEnumerable<KeyValuePair<string, Texture2D>> LoadImagesForMod(ModContentPack modContent, IEnumerable<string> filenamesNoExtension)
	{
		return filenamesNoExtension.Select((string filename) => new KeyValuePair<string, Texture2D>(filename, GetImage(modContent, filename)));
	}

	private static Texture2D GetImage(ModContentPack modContent, string relativeFilePathNoExtension)
	{
		try
		{
			Texture2D texture2D = TryResolveTextureRelativeToNewsFolder(modContent, relativeFilePathNoExtension);
			if (texture2D != null)
			{
				return texture2D;
			}
			Texture2D texture2D2 = ContentFinder<Texture2D>.Get(relativeFilePathNoExtension, reportFailure: false);
			if (texture2D2 != null)
			{
				return texture2D2;
			}
		}
		catch (Exception ex)
		{
			HugsLibController.Logger.Warning("Exception while loading texture: " + ex);
		}
		HugsLibController.Logger.Warning("Failed to resolve update feature texture mod:" + modContent.PackageIdPlayerFacing + " file:" + relativeFilePathNoExtension + ", using placeholder");
		return missingTexturePlaceholder;
	}

	private static Texture2D TryResolveTextureRelativeToNewsFolder(ModContentPack modContent, string relativeFilePathNoExtension)
	{
		string text = Path.Combine(modContent.RootDir, "News/");
		if (Directory.Exists(text))
		{
			string text2 = Path.Combine(text, relativeFilePathNoExtension);
			string[] possibleTextureFileExtensions = PossibleTextureFileExtensions;
			foreach (string text3 in possibleTextureFileExtensions)
			{
				FileInfo fileInfo = new FileInfo(text2 + text3);
				if (fileInfo.Exists)
				{
					return LoadTextureFromFile(fileInfo);
				}
			}
		}
		return null;
	}

	private static Texture2D LoadTextureFromFile(FileInfo file)
	{
		try
		{
			byte[] array = File.ReadAllBytes(file.FullName);
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.Alpha8, mipChain: true);
			ImageConversion.LoadImage(texture2D, array);
			texture2D.name = Path.GetFileNameWithoutExtension(file.Name);
			texture2D.Compress(highQuality: true);
			texture2D.filterMode = FilterMode.Bilinear;
			texture2D.anisoLevel = 2;
			texture2D.Apply(updateMipmaps: true, makeNoLongerReadable: true);
			return texture2D;
		}
		catch (Exception innerException)
		{
			throw new IOException("Failed to load texture at path \"" + file.FullName + "\"", innerException);
		}
	}
}
