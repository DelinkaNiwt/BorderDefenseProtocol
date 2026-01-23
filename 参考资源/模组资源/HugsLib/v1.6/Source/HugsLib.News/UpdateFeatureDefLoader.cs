using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace HugsLib.News;

/// <summary>
/// Handles the custom loading mechanics of <see cref="T:HugsLib.UpdateFeatureDef" />s.
/// </summary>
internal static class UpdateFeatureDefLoader
{
	public struct DefXMLNode
	{
		public ModContentPack ContentPack { get; }

		public XmlNode Node { get; }

		public LoadableXmlAsset SourceAsset { get; }

		public DefXMLNode(ModContentPack contentPack, XmlNode node, LoadableXmlAsset sourceAsset)
		{
			ContentPack = contentPack;
			Node = node;
			SourceAsset = sourceAsset;
		}

		public void Deconstruct(out ModContentPack pack, out XmlNode node, out LoadableXmlAsset asset)
		{
			pack = ContentPack;
			node = Node;
			asset = SourceAsset;
		}
	}

	public static void ResolveAndInjectUpdateFeatureDefs(IEnumerable<DefXMLNode> loadedNodes)
	{
		ResetUpdateFeatureDefTranslations();
		IEnumerable<UpdateFeatureDef> defs = ResolveUpdateFeatureDefsFromNodes(loadedNodes);
		ReinitUpdateFeatureDefDatabase(defs);
	}

	public static void ReloadAllUpdateFeatureDefs()
	{
		var (loadedNodes, errors) = LoadUpdateFeatureDefNodes();
		HandleDefLoadingErrors(errors);
		ResolveAndInjectUpdateFeatureDefs(loadedNodes);
		InjectTranslationsIntoUpdateFeatureDefs();
	}

	public static void HandleDefLoadingErrors(IEnumerable<string> errors)
	{
		foreach (string error in errors)
		{
			HugsLibController.Logger.Error(error);
		}
	}

	public static (IEnumerable<DefXMLNode> nodes, IEnumerable<string> loadingErrors) LoadUpdateFeatureDefNodes()
	{
		List<DefXMLNode> list = new List<DefXMLNode>();
		List<string> list2 = new List<string>(0);
		foreach (ModContentPack runningMod in LoadedModManager.RunningMods)
		{
			try
			{
				LoadableXmlAsset[] array = DirectXmlLoader.XmlAssetsInModFolder(runningMod, "News/");
				LoadableXmlAsset[] array2 = array;
				foreach (LoadableXmlAsset loadableXmlAsset in array2)
				{
					XmlElement xmlElement = loadableXmlAsset.xmlDoc?.DocumentElement;
					if (xmlElement == null)
					{
						continue;
					}
					foreach (XmlNode item in xmlElement.ChildNodes.OfType<XmlNode>())
					{
						list.Add(new DefXMLNode(runningMod, item, loadableXmlAsset));
					}
				}
			}
			catch (Exception arg)
			{
				list2.Add("Failed to load UpdateFeatureDefs for mod " + $"{runningMod.PackageIdPlayerFacing}: {arg}");
			}
		}
		return (nodes: list, loadingErrors: list2);
	}

	private static IEnumerable<UpdateFeatureDef> ResolveUpdateFeatureDefsFromNodes(IEnumerable<DefXMLNode> nodes)
	{
		DefXMLNode[] array = nodes.ToArray();
		XmlInheritance.Clear();
		DefXMLNode[] array2 = array;
		ModContentPack pack;
		XmlNode node;
		LoadableXmlAsset asset;
		foreach (DefXMLNode defXMLNode in array2)
		{
			defXMLNode.Deconstruct(out pack, out node, out asset);
			ModContentPack mod = pack;
			XmlNode xmlNode = node;
			if (xmlNode != null && xmlNode.NodeType == XmlNodeType.Element)
			{
				XmlInheritance.TryRegister(xmlNode, mod);
			}
		}
		XmlInheritance.Resolve();
		List<UpdateFeatureDef> list = new List<UpdateFeatureDef>();
		DefXMLNode[] array3 = array;
		foreach (DefXMLNode defXMLNode in array3)
		{
			defXMLNode.Deconstruct(out pack, out node, out asset);
			ModContentPack modContentPack = pack;
			XmlNode xmlNode2 = node;
			LoadableXmlAsset loadableXmlAsset = asset;
			try
			{
				if (DirectXmlLoader.DefFromNode(xmlNode2, loadableXmlAsset) is UpdateFeatureDef updateFeatureDef)
				{
					updateFeatureDef.modContentPack = modContentPack;
					updateFeatureDef.ResolveReferences();
					list.Add(updateFeatureDef);
				}
			}
			catch (Exception ex)
			{
				HugsLibController.Logger.Error("Failed to parse UpdateFeatureDef from mod " + modContentPack.PackageIdPlayerFacing + ":\n" + GetExceptionChainMessage(ex) + "\nContext: " + xmlNode2?.OuterXml.ToStringSafe() + "\nFile: " + loadableXmlAsset?.FullFilePath.ToStringSafe() + "\n" + $"Exception: {ex}");
			}
		}
		XmlInheritance.Clear();
		return list;
	}

	private static void ReinitUpdateFeatureDefDatabase(IEnumerable<UpdateFeatureDef> defs)
	{
		DefDatabase<UpdateFeatureDef>.Clear();
		DefDatabase<UpdateFeatureDef>.Add(defs);
	}

	private static void ResetUpdateFeatureDefTranslations()
	{
		if (LanguageDatabase.activeLanguage == null)
		{
			return;
		}
		foreach (DefInjectionPackage defInjection in LanguageDatabase.activeLanguage.defInjections)
		{
			if (!(defInjection.defType == typeof(UpdateFeatureDef)))
			{
				continue;
			}
			foreach (KeyValuePair<string, DefInjectionPackage.DefInjection> injection in defInjection.injections)
			{
				injection.Value.injected = false;
			}
			defInjection.loadErrors.Clear();
		}
	}

	private static void InjectTranslationsIntoUpdateFeatureDefs()
	{
		if (LanguageDatabase.activeLanguage == null)
		{
			return;
		}
		IEnumerable<DefInjectionPackage> enumerable = LanguageDatabase.activeLanguage.defInjections.Where((DefInjectionPackage i) => i.defType == typeof(UpdateFeatureDef));
		foreach (DefInjectionPackage item in enumerable)
		{
			try
			{
				item.InjectIntoDefs(errorOnDefNotFound: true);
			}
			catch (Exception arg)
			{
				HugsLibController.Logger.Warning(string.Format("Error while injecting translations into {0}: {1}", "UpdateFeatureDef", arg));
			}
		}
	}

	private static string GetExceptionChainMessage(Exception e)
	{
		string text = e.Message;
		while (e.InnerException != null)
		{
			e = e.InnerException;
			text = text + " -> " + e.Message;
		}
		return text;
	}
}
