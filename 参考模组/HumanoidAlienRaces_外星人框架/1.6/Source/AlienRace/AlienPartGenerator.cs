using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using AlienRace.ApparelGraphics;
using AlienRace.ExtendedGraphics;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

public class AlienPartGenerator
{
	public class ExtendedGraphicTop : AbstractExtendedGraphic
	{
		public static DummyExtendedGraphicsPawnWrapper drawOverrideDummy;

		public HashSet<Type> conditionTypes = new HashSet<Type>();

		public bool debug = true;

		public bool linkVariantIndexWithPrevious;

		public Vector2 drawSize = Vector2.one;

		public Vector2 drawSizePortrait = Vector2.zero;

		public int variantCountMax;

		private const string REWIND_PATH = "void";

		[Unsaved(false)]
		public ResolveData resolveData;

		public bool Debug
		{
			get
			{
				if (debug)
				{
					if (path.NullOrEmpty())
					{
						return GetSubGraphics().Any();
					}
					return true;
				}
				return false;
			}
		}

		public int VariantCountMax
		{
			get
			{
				return variantCountMax;
			}
			set
			{
				variantCountMax = Mathf.Max(VariantCountMax, value);
			}
		}

		public IExtendedGraphic GetBestGraphic(ExtendedGraphicsPawnWrapper pawn, ResolveData data)
		{
			Pair<int, IExtendedGraphic> bestGraphic = new Pair<int, IExtendedGraphic>(0, this);
			Stack<Pair<int, IEnumerator<IExtendedGraphic>>> stack = new Stack<Pair<int, IEnumerator<IExtendedGraphic>>>();
			stack.Push(new Pair<int, IEnumerator<IExtendedGraphic>>(1, GetSubGraphics(pawn, data).GetEnumerator()));
			while (stack.Count > 0 && (bestGraphic.Second == this || bestGraphic.First < stack.Peek().First))
			{
				Pair<int, IEnumerator<IExtendedGraphic>> currentGraphicSet = stack.Pop();
				while (currentGraphicSet.Second.MoveNext())
				{
					IExtendedGraphic current = currentGraphicSet.Second.Current;
					if (current == null || !current.IsApplicable(pawn, ref data))
					{
						continue;
					}
					if (current.GetPath() == "void")
					{
						stack.Push(currentGraphicSet);
						continue;
					}
					IEnumerable<IExtendedGraphic> subGraphics = current.GetSubGraphics(pawn, data);
					if (subGraphics.Any())
					{
						currentGraphicSet = new Pair<int, IEnumerator<IExtendedGraphic>>(currentGraphicSet.First + 1, subGraphics.GetEnumerator());
					}
					else
					{
						stack.Push(currentGraphicSet);
					}
					if (!current.GetPath().NullOrEmpty() && current.GetVariantCount() > 0)
					{
						bestGraphic = new Pair<int, IExtendedGraphic>(currentGraphicSet.First, current);
						if (!subGraphics.Any())
						{
							break;
						}
					}
				}
			}
			return bestGraphic.Second;
		}

		public virtual string GetPath(Pawn pawn, ref int sharedIndex, int? savedIndex = 0, string pathAppendix = null)
		{
			IExtendedGraphic bestGraphic = GetBestGraphic((pawn != null) ? new ExtendedGraphicsPawnWrapper(pawn) : drawOverrideDummy, resolveData);
			int variantCounting = bestGraphic.GetVariantCount();
			if (variantCounting <= 0)
			{
				variantCounting = 1;
			}
			int valueOrDefault = savedIndex.GetValueOrDefault();
			if (!savedIndex.HasValue)
			{
				valueOrDefault = (linkVariantIndexWithPrevious ? (sharedIndex % VariantCountMax) : Rand.Range(0, VariantCountMax));
				savedIndex = valueOrDefault;
			}
			sharedIndex = savedIndex.Value;
			int actualIndex = sharedIndex % variantCounting;
			bool zero;
			string returnPath = bestGraphic.GetPathFromVariant(ref actualIndex, out zero) ?? string.Empty;
			return returnPath + pathAppendix + (zero ? "" : actualIndex.ToString());
		}

		public override bool IsApplicable(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
		{
			return true;
		}
	}

	public class BodyAddon : ExtendedGraphicTop
	{
		private string name;

		public string defaultOffset = "Center";

		[Unsaved(false)]
		public DirectionalOffset defaultOffsets;

		public DirectionalOffset offsets = new DirectionalOffset();

		public DirectionalOffset femaleOffsets;

		public bool? flipWest;

		public float angle;

		public bool inFrontOfBody;

		public bool layerInvert = true;

		public List<Condition> conditions = new List<Condition>();

		public bool alignWithHead;

		public bool drawRotated = true;

		public bool scaleWithPawnDrawsize;

		public List<RenderSkipFlagDef> useSkipFlags = new List<RenderSkipFlagDef>();

		private string colorChannel;

		public Color? colorOverrideOne;

		public Color? colorOverrideTwo;

		public float colorPostFactor = 1f;

		public bool userCustomizable = true;

		public bool allowColorOverride;

		private ShaderTypeDef shaderType;

		private ShaderTypeDef shaderTypeStatue;

		public string Name => name ?? (name = Path.GetFileName(path));

		public string ColorChannel
		{
			get
			{
				return colorChannel ?? (colorChannel = "skin");
			}
			set
			{
				colorChannel = value ?? "skin";
			}
		}

		public ShaderTypeDef ShaderType
		{
			get
			{
				return shaderType ?? (shaderType = ShaderTypeDefOf.Cutout);
			}
			set
			{
				shaderType = value ?? ShaderTypeDefOf.Cutout;
			}
		}

		public ShaderTypeDef ShaderTypeStatue
		{
			get
			{
				return shaderTypeStatue ?? (shaderTypeStatue = ShaderTypeDefOf.Cutout);
			}
			set
			{
				shaderTypeStatue = value ?? ShaderTypeDefOf.Cutout;
			}
		}

		public virtual bool CanDrawAddon(Pawn pawn)
		{
			return CanDrawAddon(new ExtendedGraphicsPawnWrapper(pawn));
		}

		private bool CanDrawAddon(ExtendedGraphicsPawnWrapper pawn)
		{
			return conditions.TrueForAll((Condition c) => c.Satisfied(pawn, ref resolveData));
		}

		public virtual bool CanDrawAddonStatic(Pawn pawn)
		{
			return CanDrawAddonStatic(new ExtendedGraphicsPawnWrapper(pawn));
		}

		private bool CanDrawAddonStatic(ExtendedGraphicsPawnWrapper pawn)
		{
			return conditions.TrueForAll((Condition c) => !c.Static || c.Satisfied(pawn, ref resolveData));
		}

		public virtual Graphic GetGraphic(Pawn pawn, AlienComp alienComp, ref int sharedIndex, int? savedIndex = null, bool precheckCompare = false, Graphic preGraphic = null)
		{
			ExposableValueTuple<Color, Color> channel = alienComp?.GetChannel(ColorChannel) ?? new ExposableValueTuple<Color, Color>(Color.white, Color.white);
			Color obj;
			if (!(ColorChannel == "skin"))
			{
				obj = channel.first;
			}
			else
			{
				Pawn_StoryTracker story = pawn.story;
				obj = ((story != null && story.skinColorOverride.HasValue) ? pawn.story.skinColorOverride.Value : channel.first);
			}
			Color first = obj;
			Color second = channel.second;
			if (colorOverrideOne.HasValue)
			{
				first = colorOverrideOne.Value;
			}
			if (colorOverrideTwo.HasValue)
			{
				second = colorOverrideTwo.Value;
			}
			if (Math.Abs(colorPostFactor - 1f) > float.Epsilon)
			{
				first *= colorPostFactor;
				second *= colorPostFactor;
			}
			if (ColorChannel == "skin" && pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Rotting)
			{
				first = PawnRenderUtility.GetRottenColor(first);
			}
			first = AlienRenderTreePatches.CheckOverrideColor(pawn, first);
			string returnPath = GetPath(pawn, ref sharedIndex, savedIndex);
			if (!returnPath.NullOrEmpty() && (!precheckCompare || preGraphic == null || returnPath != preGraphic.path || first != preGraphic.Color || second != preGraphic.ColorTwo))
			{
				Graphic_Multi_RotationFromData graphic = GraphicDatabase.Get<Graphic_Multi_RotationFromData>(returnPath, AlienRenderTreePatches.IsStatuePawn(pawn) ? ShaderTypeStatue.Shader : ((ContentFinder<Texture2D>.Get(returnPath + "_southm", reportFailure: false) == null) ? ShaderType.Shader : ShaderDatabase.CutoutComplex), Vector2.one, first, second, new GraphicData
				{
					drawRotated = !drawRotated
				}) as Graphic_Multi_RotationFromData;
				graphic.westFlipped = flipWest;
				return graphic;
			}
			return null;
		}
	}

	public class WoundAnchorReplacement
	{
		public string originalTag = string.Empty;

		public BodyPartGroupDef originalGroup;

		public BodyTypeDef.WoundAnchor replacement;

		public DirectionalOffset offsets;

		public bool ValidReplacement(BodyTypeDef.WoundAnchor original)
		{
			if (original.rotation != replacement.rotation)
			{
				return false;
			}
			if (!originalTag.NullOrEmpty() && !original.tag.NullOrEmpty() && originalTag == original.tag)
			{
				return true;
			}
			if (originalGroup != null && original.group != null && originalGroup == original.group)
			{
				return true;
			}
			return false;
		}
	}

	public class OffsetNamed
	{
		public string name = "";

		public DirectionalOffset offsets;
	}

	public class ColorChannelGenerator
	{
		public string name = "";

		public List<ColorChannelGeneratorCategory> entries = new List<ColorChannelGeneratorCategory>();

		[UsedImplicitly]
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			foreach (XmlNode xmlNode in xmlRoot.ChildNodes)
			{
				switch (xmlNode.Name)
				{
				case "name":
					name = xmlNode.InnerText.Trim();
					break;
				case "first":
					if (entries.NullOrEmpty())
					{
						entries.Add(new ColorChannelGeneratorCategory
						{
							weight = 100f
						});
					}
					entries[0].first = DirectXmlToObject.ObjectFromXml<ColorGenerator>(xmlNode, doPostLoad: false);
					break;
				case "second":
					if (entries.NullOrEmpty())
					{
						entries.Add(new ColorChannelGeneratorCategory
						{
							weight = 100f
						});
					}
					entries[0].second = DirectXmlToObject.ObjectFromXml<ColorGenerator>(xmlNode, doPostLoad: false);
					break;
				case "entries":
					entries = DirectXmlToObject.ObjectFromXml<List<ColorChannelGeneratorCategory>>(xmlNode, doPostLoad: false);
					break;
				}
			}
		}
	}

	public class ColorChannelGeneratorCategory
	{
		public float weight = float.Epsilon;

		public ColorGenerator first;

		public ColorGenerator second;
	}

	public class AlienComp : ThingComp
	{
		public class ColorChannelLinkData : IExposable
		{
			public class ColorChannelLinkTargetData : IExposable
			{
				public string targetChannel;

				public int categoryIndex;

				public void ExposeData()
				{
					Scribe_Values.Look(ref targetChannel, "targetChannel");
					Scribe_Values.Look(ref categoryIndex, "categoryIndex", 0);
				}
			}

			public string originalChannel;

			public HashSet<ColorChannelLinkTargetData> targetsChannelOne = new HashSet<ColorChannelLinkTargetData>();

			public HashSet<ColorChannelLinkTargetData> targetsChannelTwo = new HashSet<ColorChannelLinkTargetData>();

			public HashSet<ColorChannelLinkTargetData> GetTargetDataFor(bool first)
			{
				if (!first)
				{
					return targetsChannelTwo;
				}
				return targetsChannelOne;
			}

			public void ExposeData()
			{
				Scribe_Values.Look(ref originalChannel, "originalChannel");
				Scribe_Collections.Look(ref targetsChannelOne, "targetsChannelOne");
				Scribe_Collections.Look(ref targetsChannelTwo, "targetsChannelTwo");
			}
		}

		private const string ScribeNodeName = "AlienRaces_AlienComp";

		public PawnKindDef originalKindDef;

		public bool fixGenderPostSpawn;

		public Vector2 customDrawSize = Vector2.one;

		public Vector2 customHeadDrawSize = Vector2.one;

		public Vector2 customPortraitDrawSize = Vector2.one;

		public Vector2 customPortraitHeadDrawSize = Vector2.one;

		public int bodyVariant = -1;

		public int headVariant = -1;

		public int headMaskVariant = -1;

		public int bodyMaskVariant = -1;

		public List<Graphic> addonGraphics;

		public List<int> addonVariants;

		public List<ExposableValueTuple<Color?, Color?>> addonColors = new List<ExposableValueTuple<Color?, Color?>>();

		public int lastAlienMeatIngestedTick;

		private Dictionary<string, ExposableValueTuple<Color, Color>> colorChannels;

		private Dictionary<string, ColorChannelLinkData> colorChannelLinks = new Dictionary<string, ColorChannelLinkData>();

		private bool saveIsAfter1_4;

		private List<AlienPawnRenderNodeProperties_BodyAddon> nodeProps;

		public readonly HashSet<Type> regenerateTypes = new HashSet<Type>();

		public Dictionary<string, ColorChannelLinkData> ColorChannelLinks => colorChannelLinks;

		private Pawn Pawn => (Pawn)parent;

		private ThingDef_AlienRace AlienProps => (ThingDef_AlienRace)Pawn.def;

		public Dictionary<string, ExposableValueTuple<Color, Color>> ColorChannels
		{
			get
			{
				if (colorChannels == null || !colorChannels.Any())
				{
					AlienPartGenerator apg = AlienProps.alienRace.generalSettings.alienPartGenerator;
					colorChannels = new Dictionary<string, ExposableValueTuple<Color, Color>>();
					colorChannelLinks = new Dictionary<string, ColorChannelLinkData>();
					colorChannels.Add("base", new ExposableValueTuple<Color, Color>(Color.white, Color.white));
					colorChannels.Add("hair", new ExposableValueTuple<Color, Color>(Color.clear, Color.clear));
					colorChannels.Add("skin", new ExposableValueTuple<Color, Color>(Color.clear, Color.clear));
					colorChannels.Add("skinBase", new ExposableValueTuple<Color, Color>(Color.clear, Color.clear));
					colorChannels.Add("tattoo", new ExposableValueTuple<Color, Color>(Color.clear, Color.clear));
					colorChannels.Add("favorite", new ExposableValueTuple<Color, Color>(Color.clear, Color.clear));
					colorChannels.Add("ideo", new ExposableValueTuple<Color, Color>(Color.clear, Color.clear));
					colorChannels.Add("mech", new ExposableValueTuple<Color, Color>(Color.clear, Color.clear));
					foreach (ColorChannelGenerator channel in apg.colorChannels)
					{
						if (!colorChannels.ContainsKey(channel.name))
						{
							colorChannels.Add(channel.name, new ExposableValueTuple<Color, Color>(Color.white, Color.white));
						}
						colorChannels[channel.name] = GenerateChannel(channel, colorChannels[channel.name]);
					}
					try
					{
						if (!AlienProps.alienRace.raceRestriction.blackEndoCategories.Contains(EndogeneCategory.Melanin) && Pawn.story.SkinColorBase != Color.clear)
						{
							OverwriteColorChannel("skin", Pawn.story.SkinColorBase);
						}
					}
					catch (InvalidOperationException)
					{
					}
					ExposableValueTuple<Color, Color> skinColors = colorChannels["skin"];
					OverwriteColorChannel("skinBase", skinColors.first, skinColors.second);
					Pawn.story.SkinColorBase = skinColors.first;
					if (colorChannels["hair"].first == Color.clear)
					{
						OverwriteColorChannel("hair", Pawn.story.HairColor);
					}
					if (colorChannels["tattoo"].first == Color.clear)
					{
						Color tattooColor = skinColors.first;
						tattooColor.a *= 0.8f;
						Color tattooColorSecond = skinColors.second;
						tattooColorSecond.a *= 0.8f;
						OverwriteColorChannel("tattoo", tattooColor, tattooColorSecond);
					}
					Corpse corpse = Pawn.Corpse;
					if (corpse != null && corpse.GetRotStage() == RotStage.Rotting)
					{
						OverwriteColorChannel("skin", PawnRenderUtility.GetRottenColor(colorChannels["skin"].first));
					}
					Pawn.story.HairColor = colorChannels["hair"].first;
					RegenerateColorChannelLink("skin");
					if (AlienProps.alienRace.generalSettings.alienPartGenerator.oldHairColorGen != null && Rand.Value < AlienProps.alienRace.generalSettings.alienPartGenerator.oldHairAgeCurve.Evaluate(Pawn.ageTracker.AgeBiologicalYearsFloat))
					{
						Color oldAgeColor = GenerateColor(AlienProps.alienRace.generalSettings.alienPartGenerator.oldHairColorGen);
						OverwriteColorChannel("hair", oldAgeColor);
						Pawn.story.HairColor = colorChannels["hair"].first;
					}
				}
				return colorChannels;
			}
			set
			{
				colorChannels = value;
			}
		}

		public ExposableValueTuple<Color, Color> GenerateChannel(ColorChannelGenerator channel, ExposableValueTuple<Color, Color> colors = null)
		{
			if (colors == null)
			{
				colors = new ExposableValueTuple<Color, Color>();
			}
			ColorChannelGeneratorCategory categoryEntry = channel.entries.RandomElementByWeight((ColorChannelGeneratorCategory ccgc) => ccgc.weight);
			if (categoryEntry.first != null)
			{
				colors.first = GenerateColor(channel, categoryEntry, first: true);
			}
			if (categoryEntry.second != null)
			{
				colors.second = GenerateColor(channel, categoryEntry, first: false);
			}
			return colors;
		}

		public Color GenerateColor(ColorChannelGenerator channel, ColorChannelGeneratorCategory category, bool first)
		{
			ColorGenerator gen = (first ? category.first : category.second);
			if (gen is ColorGenerator_CustomAlienChannel ac)
			{
				ac.GetInfo(out var channelName, out var firstColor);
				if (!ColorChannelLinks.ContainsKey(channelName))
				{
					ColorChannelLinks.Add(channelName, new ColorChannelLinkData
					{
						originalChannel = channelName
					});
				}
				HashSet<ColorChannelLinkData.ColorChannelLinkTargetData> linkTargetData = ColorChannelLinks[channelName].GetTargetDataFor(first);
				if (linkTargetData.All((ColorChannelLinkData.ColorChannelLinkTargetData ccltd) => ccltd.targetChannel != channel.name))
				{
					linkTargetData.Add(new ColorChannelLinkData.ColorChannelLinkTargetData
					{
						targetChannel = channel.name,
						categoryIndex = channel.entries.IndexOf(category)
					});
				}
				if (!firstColor)
				{
					return ColorChannels[channelName].second;
				}
				return ColorChannels[channelName].first;
			}
			return GenerateColor(gen);
		}

		public Color GenerateColor(ColorGenerator gen)
		{
			if (!(gen is ColorGenerator_SkinColorMelanin cm))
			{
				if (gen is ChannelColorGenerator_PawnBased pb)
				{
					return pb.NewRandomizedColor(Pawn);
				}
				return gen.NewRandomizedColor();
			}
			return cm.naturalMelanin ? ((Pawn)parent).story.SkinColorBase : gen.NewRandomizedColor();
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			AlienPartGenerator apg = ((ThingDef_AlienRace)parent.def).alienRace.generalSettings.alienPartGenerator;
			customDrawSize = apg.customDrawSize;
			customHeadDrawSize = apg.customHeadDrawSize;
			customPortraitDrawSize = apg.customPortraitDrawSize;
			customPortraitHeadDrawSize = apg.customPortraitHeadDrawSize;
			originalKindDef = Pawn.kindDef;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				XmlNode parentNode = Scribe.loader.curXmlParent;
				saveIsAfter1_4 = parentNode != null && parentNode["AlienRaces_AlienComp"] != null;
			}
			if ((Scribe.mode == LoadSaveMode.Saving || saveIsAfter1_4) && Scribe.EnterNode("AlienRaces_AlienComp"))
			{
				try
				{
					ExposeDataInternal();
					return;
				}
				finally
				{
					Scribe.ExitNode();
				}
			}
			ExposeDataInternal();
			colorChannelLinks = new Dictionary<string, ColorChannelLinkData>();
			foreach (ColorChannelGenerator ccg in AlienProps.alienRace.generalSettings.alienPartGenerator.colorChannels)
			{
				foreach (ColorChannelGeneratorCategory ccgc in ccg.entries)
				{
					if (ccgc.first is ColorGenerator_CustomAlienChannel)
					{
						GenerateColor(ccg, ccgc, first: true);
					}
					if (ccgc.second is ColorGenerator_CustomAlienChannel)
					{
						GenerateColor(ccg, ccgc, first: false);
					}
				}
			}
		}

		private void ExposeDataInternal()
		{
			Scribe_Values.Look(ref fixGenderPostSpawn, "fixAlienGenderPostSpawn", defaultValue: false);
			Scribe_Collections.Look(ref addonVariants, "addonVariants", LookMode.Undefined);
			Scribe_Collections.Look(ref addonColors, "addonColors", LookMode.Deep);
			Scribe_Collections.Look(ref colorChannels, "colorChannels");
			Scribe_Collections.Look(ref colorChannelLinks, "colorChannelLinks", LookMode.Value, LookMode.Deep);
			Scribe_Values.Look(ref headVariant, "headVariant", -1);
			Scribe_Values.Look(ref bodyVariant, "bodyVariant", -1);
			Scribe_Values.Look(ref headMaskVariant, "headMaskVariant", -1);
			Scribe_Values.Look(ref bodyMaskVariant, "bodyMaskVariant", -1);
			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs && Pawn != null)
			{
				Pawn.story.SkinColorBase = GetChannel("skin").first;
			}
			colorChannelLinks = ColorChannelLinks ?? new Dictionary<string, ColorChannelLinkData>();
		}

		public ExposableValueTuple<Color, Color> GetChannel(string channel)
		{
			if (ColorChannels.TryGetValue(channel, out var colorChannel))
			{
				return colorChannel;
			}
			AlienPartGenerator apg = AlienProps.alienRace.generalSettings.alienPartGenerator;
			foreach (ColorChannelGenerator apgChannel in apg.colorChannels)
			{
				if (apgChannel.name == channel)
				{
					ColorChannels.Add(channel, GenerateChannel(apgChannel));
					return ColorChannels[channel];
				}
			}
			return new ExposableValueTuple<Color, Color>(Color.white, Color.white);
		}

		public void RegenerateColorChannelLinks()
		{
			foreach (string key in ColorChannelLinks.Keys)
			{
				RegenerateColorChannelLink(key);
			}
		}

		public void RegenerateColorChannelLink(string channel)
		{
			ThingDef_AlienRace alienProps = (ThingDef_AlienRace)parent.def;
			AlienPartGenerator apg = alienProps.alienRace.generalSettings.alienPartGenerator;
			if (!ColorChannelLinks.TryGetValue(channel, out var colorChannelLink))
			{
				return;
			}
			foreach (ColorChannelLinkData.ColorChannelLinkTargetData targetData in colorChannelLink.targetsChannelOne)
			{
				ColorChannelGenerator apgChannel = apg.colorChannels.FirstOrDefault((ColorChannelGenerator ccg) => ccg.name == targetData.targetChannel);
				if (apgChannel != null)
				{
					ColorChannels[targetData.targetChannel].first = GenerateColor(apgChannel, apgChannel.entries[targetData.categoryIndex], first: true);
				}
			}
			foreach (ColorChannelLinkData.ColorChannelLinkTargetData targetData2 in colorChannelLink.targetsChannelTwo)
			{
				ColorChannelGenerator apgChannel2 = apg.colorChannels.FirstOrDefault((ColorChannelGenerator ccg) => ccg.name == targetData2.targetChannel);
				if (apgChannel2 != null)
				{
					ColorChannels[targetData2.targetChannel].second = GenerateColor(apgChannel2, apgChannel2.entries[targetData2.categoryIndex], first: false);
				}
			}
		}

		public void OverwriteColorChannel(string channel, Color? first = null, Color? second = null)
		{
			if (!ColorChannels.ContainsKey(channel))
			{
				ColorChannels.Add(channel, new ExposableValueTuple<Color, Color>(Color.clear, Color.clear));
			}
			if (first.HasValue)
			{
				ColorChannels[channel].first = first.Value;
			}
			if (second.HasValue)
			{
				ColorChannels[channel].second = second.Value;
			}
			RegenerateColorChannelLink(channel);
		}

		public static void CopyAlienData(Pawn original, Pawn clone)
		{
			AlienComp originalComp = original.TryGetComp<AlienComp>();
			AlienComp cloneComp = clone.TryGetComp<AlienComp>();
			CopyAlienData(originalComp, cloneComp);
			clone.Drawer.renderer.SetAllGraphicsDirty();
		}

		public static void CopyAlienData(AlienComp originalComp, AlienComp cloneComp)
		{
			originalComp.Pawn?.Drawer?.renderer?.EnsureGraphicsInitialized();
			if (originalComp.Pawn?.Drawer?.renderer?.renderTree?.Resolved != true)
			{
				return;
			}
			try
			{
				string key;
				foreach (KeyValuePair<string, ExposableValueTuple<Color, Color>> colorChannel in originalComp.ColorChannels)
				{
					colorChannel.Deconstruct(out key, out var value);
					string channel = key;
					ExposableValueTuple<Color, Color> colors = value;
					cloneComp.OverwriteColorChannel(channel, colors.first, colors.second);
				}
				cloneComp.addonVariants = originalComp.addonVariants.ListFullCopy();
				cloneComp.addonColors = originalComp.addonColors.Select((ExposableValueTuple<Color?, Color?> vt) => new ExposableValueTuple<Color?, Color?>(vt.first, vt.second)).ToList();
				cloneComp.colorChannelLinks = new Dictionary<string, ColorChannelLinkData>();
				foreach (KeyValuePair<string, ColorChannelLinkData> colorChannelLink in originalComp.ColorChannelLinks)
				{
					colorChannelLink.Deconstruct(out key, out var value2);
					string key2 = key;
					ColorChannelLinkData originalData = value2;
					ColorChannelLinkData cloneData = new ColorChannelLinkData
					{
						originalChannel = originalData.originalChannel,
						targetsChannelOne = new HashSet<ColorChannelLinkData.ColorChannelLinkTargetData>(),
						targetsChannelTwo = new HashSet<ColorChannelLinkData.ColorChannelLinkTargetData>()
					};
					foreach (ColorChannelLinkData.ColorChannelLinkTargetData targetData in originalData.targetsChannelOne)
					{
						cloneData.targetsChannelOne.Add(new ColorChannelLinkData.ColorChannelLinkTargetData
						{
							categoryIndex = targetData.categoryIndex,
							targetChannel = targetData.targetChannel
						});
					}
					foreach (ColorChannelLinkData.ColorChannelLinkTargetData targetData2 in originalData.targetsChannelTwo)
					{
						cloneData.targetsChannelTwo.Add(new ColorChannelLinkData.ColorChannelLinkTargetData
						{
							categoryIndex = targetData2.categoryIndex,
							targetChannel = targetData2.targetChannel
						});
					}
					cloneComp.ColorChannelLinks.Add(key2, cloneData);
				}
				cloneComp.bodyVariant = originalComp.bodyVariant;
				cloneComp.bodyMaskVariant = originalComp.bodyMaskVariant;
				cloneComp.headVariant = originalComp.headVariant;
				cloneComp.headMaskVariant = originalComp.headMaskVariant;
				cloneComp.lastAlienMeatIngestedTick = originalComp.lastAlienMeatIngestedTick;
			}
			catch (Exception arg)
			{
				Log.Error($"Error copying alien data from {originalComp.Pawn?.Name}: {arg}");
			}
		}

		public override List<PawnRenderNode> CompRenderNodes()
		{
			List<PawnRenderNode> nodes = new List<PawnRenderNode>();
			List<AlienPawnRenderNodeProperties_BodyAddon> nodePropsTemp = nodeProps ?? new List<AlienPawnRenderNodeProperties_BodyAddon>();
			addonGraphics = new List<Graphic>();
			AlienComp alienComp = this;
			if (alienComp.addonVariants == null)
			{
				alienComp.addonVariants = new List<int>();
			}
			alienComp = this;
			if (alienComp.addonColors == null)
			{
				alienComp.addonColors = new List<ExposableValueTuple<Color?, Color?>>();
			}
			int sharedIndex = 0;
			using IEnumerator<BodyAddon> bodyAddons = AlienProps.alienRace.generalSettings.alienPartGenerator.bodyAddons.Concat(Utilities.UniversalBodyAddons).GetEnumerator();
			int addonIndex = 0;
			while (bodyAddons.MoveNext())
			{
				BodyAddon addon = bodyAddons.Current;
				if (nodeProps == null)
				{
					AlienPawnRenderNodeProperties_BodyAddon alienPawnRenderNodeProperties_BodyAddon = new AlienPawnRenderNodeProperties_BodyAddon();
					alienPawnRenderNodeProperties_BodyAddon.addon = addon;
					alienPawnRenderNodeProperties_BodyAddon.addonIndex = addonIndex;
					alienPawnRenderNodeProperties_BodyAddon.parentTagDef = (addon.alignWithHead ? PawnRenderNodeTagDefOf.Head : PawnRenderNodeTagDefOf.Body);
					alienPawnRenderNodeProperties_BodyAddon.pawnType = PawnRenderNodeProperties.RenderNodePawnType.HumanlikeOnly;
					alienPawnRenderNodeProperties_BodyAddon.workerClass = typeof(AlienPawnRenderNodeWorker_BodyAddon);
					alienPawnRenderNodeProperties_BodyAddon.nodeClass = typeof(AlienPawnRenderNode_BodyAddon);
					alienPawnRenderNodeProperties_BodyAddon.drawData = DrawData.NewWithData(new DrawData.RotationalData
					{
						rotationOffset = addon.angle
					}, new DrawData.RotationalData
					{
						rotationOffset = 0f - addon.angle,
						rotation = Rot4.East
					}, new DrawData.RotationalData
					{
						rotationOffset = 0f,
						rotation = Rot4.North
					});
					alienPawnRenderNodeProperties_BodyAddon.useGraphic = true;
					alienPawnRenderNodeProperties_BodyAddon.alienComp = this;
					alienPawnRenderNodeProperties_BodyAddon.debugLabel = addon.Name;
					AlienPawnRenderNodeProperties_BodyAddon node = alienPawnRenderNodeProperties_BodyAddon;
					RegenerateAddonGraphic(node, addonIndex, ref sharedIndex, force: true);
					nodePropsTemp.Add(node);
				}
				if (addon.CanDrawAddonStatic(Pawn))
				{
					AlienPawnRenderNodeProperties_BodyAddon nodeProp = nodePropsTemp[addonIndex];
					PawnRenderNode pawnRenderNode = (PawnRenderNode)Activator.CreateInstance(nodeProp.nodeClass, Pawn, nodeProp, Pawn.Drawer.renderer.renderTree);
					nodeProp.node = pawnRenderNode as AlienPawnRenderNode_BodyAddon;
					RegenerateAddonGraphic(nodeProp, addonIndex, ref sharedIndex);
					nodes.Add(pawnRenderNode);
				}
				addonIndex++;
			}
			if (nodeProps == null)
			{
				nodeProps = nodePropsTemp;
			}
			return nodes;
		}

		public static void RegenerateAddonsForced(Pawn pawn)
		{
			pawn.GetComp<AlienComp>()?.RegenerateAddonsForced();
		}

		public void RegenerateAddonsForced()
		{
			if (!Pawn.Drawer.renderer.renderTree.Resolved || !Pawn.Spawned || nodeProps == null)
			{
				return;
			}
			using (AlienProps.alienRace.generalSettings.alienPartGenerator.bodyAddons.Concat(Utilities.UniversalBodyAddons).GetEnumerator())
			{
				int sharedIndex = 0;
				for (int i = 0; i < nodeProps.Count; i++)
				{
					RegenerateAddonGraphic(nodeProps[i], i, ref sharedIndex, force: true);
				}
			}
		}

		public static void RegenerateAddonGraphicsWithCondition(Pawn pawn, HashSet<Type> types)
		{
			pawn.GetComp<AlienComp>()?.RegenerateAddonGraphicsWithCondition(types);
		}

		public void RegenerateAddonGraphicsWithCondition(HashSet<Type> types)
		{
			if (Pawn.Drawer.renderer.renderTree.Resolved)
			{
				if (!regenerateTypes.Any())
				{
					Application.onBeforeRender += Update;
				}
				regenerateTypes.AddRange(types);
			}
			void Update()
			{
				using IEnumerator<BodyAddon> bodyAddons = AlienProps.alienRace.generalSettings.alienPartGenerator.bodyAddons.Concat(Utilities.UniversalBodyAddons).GetEnumerator();
				int addonIndex = 0;
				int sharedIndex = 0;
				while (bodyAddons.MoveNext())
				{
					BodyAddon addon = bodyAddons.Current;
					if (addon.conditionTypes.Intersect(regenerateTypes).Any())
					{
						RegenerateAddonGraphic(nodeProps[addonIndex], addonIndex, ref sharedIndex);
					}
					addonIndex++;
				}
				regenerateTypes.Clear();
				Application.onBeforeRender -= Update;
			}
		}

		private void RegenerateAddonGraphic(AlienPawnRenderNodeProperties_BodyAddon addonProps, int addonIndex, ref int sharedIndex, bool force = false)
		{
			bool colorInsertActive = false;
			if (addonColors.Count > addonIndex)
			{
				ExposableValueTuple<Color?, Color?> addonColor = addonColors[addonIndex];
				if (addonColor.first.HasValue)
				{
					addonProps.addon.colorOverrideOne = addonColor.first;
					colorInsertActive = true;
				}
				if (addonColor.second.HasValue)
				{
					addonProps.addon.colorOverrideTwo = addonColor.second;
					colorInsertActive = true;
				}
			}
			Graphic g = addonProps.addon.GetGraphic(Pawn, this, ref sharedIndex, (addonVariants.Count > addonIndex) ? new int?(addonVariants[addonIndex]) : ((int?)null), !force, addonProps.graphic);
			if (g == null)
			{
				if (colorInsertActive)
				{
					addonProps.addon.colorOverrideOne = null;
					addonProps.addon.colorOverrideTwo = null;
				}
				return;
			}
			addonGraphics.Add(g);
			if (addonVariants.Count <= addonIndex)
			{
				addonVariants.Add(sharedIndex);
			}
			if (addonColors.Count <= addonIndex)
			{
				addonColors.Add(new ExposableValueTuple<Color?, Color?>(null, null));
			}
			else if (colorInsertActive)
			{
				addonProps.addon.colorOverrideOne = null;
				addonProps.addon.colorOverrideTwo = null;
			}
			addonProps.graphic = g;
			addonProps.node?.UpdateGraphic();
		}

		public void UpdateColors()
		{
			if (Pawn.Drawer.renderer.renderTree.Resolved || Pawn.Spawned)
			{
				OverwriteColorChannel("hair", Pawn.story.HairColor);
				OverwriteColorChannel("skin", Pawn.story.SkinColor);
				OverwriteColorChannel("skinBase", Pawn.story.SkinColorBase);
				OverwriteColorChannel("favorite", Pawn.story.favoriteColor?.color);
				Color? second = ((ColorChannels["favorite"].second != Color.clear) ? ((Color?)null) : Pawn.story.favoriteColor?.color);
				OverwriteColorChannel("favorite", null, second);
				OverwriteColorChannel("ideo", Pawn.Ideo?.Color, Pawn.Ideo?.ApparelColor);
				OverwriteColorChannel("mech", Pawn.Faction?.AllegianceColor);
				if (Pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Rotting)
				{
					OverwriteColorChannel("skin", PawnRenderUtility.GetRottenColor(Pawn.story.SkinColor));
				}
			}
		}

		public override void Notify_DefsHotReloaded()
		{
			base.Notify_DefsHotReloaded();
			LongEventHandler.QueueLongEvent(delegate
			{
				ReresolveGraphic(Pawn);
			}, $"Regenerate {Pawn.NameFullColored}", doAsynchronously: false, null);
		}

		[DebugAction("AlienRace", "Regenerate all colorchannels", false, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void RegenerateColorchannels()
		{
			foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawns)
			{
				AlienComp comp = pawn.TryGetComp<AlienComp>();
				if (comp != null)
				{
					comp.colorChannels = null;
				}
			}
		}

		[DebugAction("AlienRace", "Reresolve graphics", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns)]
		private static void ReresolveGraphic(Pawn p)
		{
			if (p != null)
			{
				FleckMaker.ThrowSmoke(p.Position.ToVector3(), p.Map, 5f);
				p.Drawer?.renderer?.SetAllGraphicsDirty();
			}
		}
	}

	public class ExposableValueTuple<TK, TV> : IExposable, IEquatable<ExposableValueTuple<TK, TV>>, ICloneable
	{
		public TK first;

		public TV second;

		public ExposableValueTuple()
		{
		}

		public ExposableValueTuple(TK first, TV second)
		{
			this.first = first;
			this.second = second;
		}

		public bool Equals(ExposableValueTuple<TK, TV> other)
		{
			if (other != null)
			{
				ref TK reference = ref first;
				object obj = other.first;
				if (reference.Equals(obj))
				{
					ref TV reference2 = ref second;
					object obj2 = other.second;
					return reference2.Equals(obj2);
				}
			}
			return false;
		}

		public override int GetHashCode()
		{
			return first.GetHashCode() + second.GetHashCode();
		}

		public object Clone()
		{
			return new ExposableValueTuple<TK, TV>(first, second);
		}

		public void ExposeData()
		{
			if (typeof(TK).GetInterface("IExposable") != null)
			{
				Scribe_Deep.Look(ref first, "first");
			}
			else if (typeof(Def).IsAssignableFrom(typeof(TK)))
			{
				if (Scribe.mode == LoadSaveMode.Saving)
				{
					string firstName = ((first is Def firstDef) ? firstDef.defName : "null");
					Scribe_Values.Look(ref firstName, "first", "null");
				}
				else if (Scribe.mode == LoadSaveMode.LoadingVars)
				{
					first = ScribeExtractor.DefFromNodeUnsafe<TK>(Scribe.loader.curXmlParent["first"]);
				}
			}
			else
			{
				Scribe_Values.Look(ref first, "first");
			}
			Scribe_Values.Look(ref second, "second");
		}
	}

	public class ExtendedConditionGraphic : AbstractExtendedGraphic
	{
		public List<Condition> conditions = new List<Condition>();

		[UsedImplicitly]
		public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			if (Condition.XmlNameParseKeys.ContainsKey(xmlRoot.LocalName))
			{
				XmlDocument xmlDoc = new XmlDocument();
				StringBuilder xmlRaw = new StringBuilder("<root>");
				if (xmlRoot.Value == null && xmlRoot.FirstChild.Name != "path")
				{
					xmlRaw.Append("<path>" + xmlRoot.FirstChild.Value + "</path>");
				}
				xmlRaw.Append("<conditions><" + xmlRoot.LocalName + ">" + xmlRoot.Attributes["For"].Value + "</" + xmlRoot.LocalName + "></conditions></root>");
				xmlDoc.LoadXml(xmlRaw.ToString());
				if (xmlRoot.Value != null)
				{
					xmlRoot.Value = string.Empty;
				}
				foreach (XmlNode childNode in xmlDoc.DocumentElement.ChildNodes)
				{
					xmlRoot.AppendChild(xmlRoot.OwnerDocument.ImportNode(childNode, deep: true));
				}
			}
			SetInstanceVariablesFromChildNodesOf(xmlRoot);
		}

		public override bool IsApplicable(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
		{
			ResolveData resolveData = data;
			bool applicable = conditions.TrueForAll((Condition c) => c.Satisfied(pawn, ref resolveData));
			if (applicable)
			{
				data = resolveData;
			}
			return applicable;
		}
	}

	public class DirectionalOffset
	{
		public RotationOffset south = new RotationOffset();

		public RotationOffset north = new RotationOffset();

		public RotationOffset east = new RotationOffset();

		public RotationOffset west;

		public RotationOffset GetOffset(Rot4 rotation)
		{
			if (!(rotation == Rot4.South))
			{
				if (!(rotation == Rot4.North))
				{
					if (!(rotation == Rot4.East))
					{
						return west;
					}
					return east;
				}
				return north;
			}
			return south;
		}
	}

	public class RotationOffset
	{
		public float layerOffset;

		public Vector2 offset;

		public List<BodyTypeOffset> portraitBodyTypes;

		public List<BodyTypeOffset> bodyTypes;

		public List<HeadTypeOffsets> portraitHeadTypes;

		public List<HeadTypeOffsets> headTypes;

		public Vector3 GetOffset(bool portrait, BodyTypeDef bodyType, HeadTypeDef headType)
		{
			Vector2 bodyOffset = (portrait ? (portraitBodyTypes ?? bodyTypes) : bodyTypes)?.FirstOrDefault((BodyTypeOffset to) => to.bodyType == bodyType)?.offset ?? Vector2.zero;
			Vector2 headOffset = (portrait ? (portraitHeadTypes ?? headTypes) : headTypes)?.FirstOrDefault((HeadTypeOffsets to) => to.headType == headType)?.offset ?? Vector2.zero;
			return new Vector3(offset.x + bodyOffset.x + headOffset.x, layerOffset, offset.y + bodyOffset.y + headOffset.y);
		}
	}

	public class BodyTypeOffset
	{
		public BodyTypeDef bodyType;

		public Vector2 offset = Vector2.zero;

		[UsedImplicitly]
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "bodyType", xmlRoot.Name);
			offset = (Vector2)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(Vector2));
		}
	}

	public class HeadTypeOffsets
	{
		public HeadTypeDef headType;

		public Vector2 offset = Vector2.zero;

		[UsedImplicitly]
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "headType", xmlRoot.Name);
			offset = (Vector2)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(Vector2));
		}
	}

	public List<HeadTypeDef> headTypes;

	public List<BodyTypeDef> bodyTypes = new List<BodyTypeDef>();

	[LoadDefFromField("Male")]
	public BodyTypeDef defaultMaleBodyType;

	[LoadDefFromField("Female")]
	public BodyTypeDef defaultFemaleBodyType;

	public FloatRange oldHairAgeRange = new FloatRange(-1f, -1f);

	public SimpleCurve oldHairAgeCurve = new SimpleCurve();

	public ColorGenerator oldHairColorGen = new ColorGenerator_Options
	{
		options = new List<ColorOption>
		{
			new ColorOption
			{
				only = new Color(0.65f, 0.65f, 0.65f)
			},
			new ColorOption
			{
				only = new Color(0.7f, 0.7f, 0.7f)
			},
			new ColorOption
			{
				only = new Color(0.75f, 0.75f, 0.75f)
			},
			new ColorOption
			{
				only = new Color(0.8f, 0.8f, 0.8f)
			},
			new ColorOption
			{
				only = new Color(0.85f, 0.85f, 0.85f)
			}
		}
	};

	public List<ColorChannelGenerator> colorChannels = new List<ColorChannelGenerator>();

	[Unsaved(false)]
	public Dictionary<string, OffsetNamed> offsetDefaultsDictionary;

	public List<OffsetNamed> offsetDefaults = new List<OffsetNamed>();

	public List<WoundAnchorReplacement> anchorReplacements = new List<WoundAnchorReplacement>();

	public Vector2 headOffset = Vector2.zero;

	public DirectionalOffset headOffsetDirectional = new DirectionalOffset();

	public Vector2 headFemaleOffset = Vector2.negativeInfinity;

	public DirectionalOffset headFemaleOffsetDirectional;

	public float borderScale = 1f;

	public int atlasScale = 1;

	public Vector2 customDrawSize = Vector2.one;

	public Vector2 customPortraitDrawSize = Vector2.one;

	public Vector2 customHeadDrawSize = Vector2.zero;

	public Vector2 customPortraitHeadDrawSize = Vector2.zero;

	public Vector2 customFemaleDrawSize = Vector2.zero;

	public Vector2 customFemalePortraitDrawSize = Vector2.zero;

	public Vector2 customFemaleHeadDrawSize = Vector2.zero;

	public Vector2 customFemalePortraitHeadDrawSize = Vector2.zero;

	public BodyPartDef headBodyPartDef;

	public List<BodyAddon> bodyAddons = new List<BodyAddon>();

	public ThingDef_AlienRace alienProps;

	public static readonly HashSet<AlienPartGenerator> graphicsQueue = new HashSet<AlienPartGenerator>();

	private static readonly IGraphicsLoader graphicsLoader = new DefaultGraphicsLoader();

	public List<HeadTypeDef> HeadTypes => headTypes ?? CachedData.DefaultHeadTypeDefs;

	public Color SkinColor(Pawn alien, bool first = true)
	{
		if (alien.Drawer.renderer.StatueColor.HasValue)
		{
			return alien.Drawer.renderer.StatueColor.Value;
		}
		AlienComp alienComp = alien.TryGetComp<AlienComp>();
		ExposableValueTuple<Color, Color> skinColors = alienComp.GetChannel("skin");
		if (!first)
		{
			return skinColors.second;
		}
		return skinColors.first;
	}

	public void GenericOffsets()
	{
		GenerateOffsetDefaults();
	}

	private void GenerateOffsetDefaults()
	{
		offsetDefaults.Add(new OffsetNamed
		{
			name = "Center",
			offsets = new DirectionalOffset()
		});
		offsetDefaults.Add(new OffsetNamed
		{
			name = "Tail",
			offsets = new DirectionalOffset
			{
				south = new RotationOffset
				{
					offset = new Vector2(0.42f, -0.22f)
				},
				north = new RotationOffset
				{
					offset = new Vector2(0f, -0.55f)
				},
				east = new RotationOffset
				{
					offset = new Vector2(0.42f, -0.22f)
				},
				west = new RotationOffset
				{
					offset = new Vector2(0.42f, -0.22f)
				}
			}
		});
		offsetDefaults.Add(new OffsetNamed
		{
			name = "Head",
			offsets = new DirectionalOffset
			{
				south = new RotationOffset
				{
					offset = new Vector2(0f, 0.5f)
				},
				north = new RotationOffset
				{
					offset = new Vector2(0f, 0.35f)
				},
				east = new RotationOffset
				{
					offset = new Vector2(-0.07f, 0.5f)
				},
				west = new RotationOffset
				{
					offset = new Vector2(-0.07f, 0.5f)
				}
			}
		});
	}

	public void GenerateMeshsAndMeshPools()
	{
		if (oldHairAgeCurve.PointsCount <= 0)
		{
			float minAge = ((oldHairAgeRange.min <= 0f) ? (alienProps.race.lifeExpectancy / 2f) : oldHairAgeRange.TrueMin);
			float maxAge = ((oldHairAgeRange.max <= 0f) ? (alienProps.race.lifeExpectancy * 0.95f) : oldHairAgeRange.TrueMax);
			oldHairAgeCurve.Add(0f, 0f);
			oldHairAgeCurve.Add(minAge, 0f);
			float ageDiff = maxAge - minAge;
			float step = ageDiff / 5f;
			for (float i = minAge + step; i < maxAge; i += ageDiff / step)
			{
				oldHairAgeCurve.Add(i, GenMath.SmootherStep(minAge, maxAge, i));
			}
			oldHairAgeCurve.Add(maxAge, 1f);
		}
		GenerateOffsetDefaults();
		if (!alienProps.alienRace.graphicPaths.head.GetSubGraphics().Any())
		{
			ExtendedGraphicTop headGraphic = alienProps.alienRace.graphicPaths.head;
			string headPath = headGraphic.path;
			foreach (HeadTypeDef headType in DefDatabase<HeadTypeDef>.AllDefs)
			{
				string headTypePath = Path.GetFileName(headType.graphicPath);
				int ind = headTypePath.IndexOf('_');
				Gender result;
				bool genderIncluded = headType.gender != Gender.None && ind >= 0 && Enum.TryParse<Gender>(headTypePath.Substring(0, ind), out result);
				headTypePath = (genderIncluded ? headTypePath.Substring(ind + 1) : headTypePath);
				ExtendedConditionGraphic headtypeGraphic = new ExtendedConditionGraphic
				{
					conditions = new List<Condition>(1)
					{
						new ConditionHeadType
						{
							headType = headType
						}
					},
					path = (headPath.NullOrEmpty() ? string.Empty : (headPath + headTypePath)),
					pathsFallback = new List<string>(1) { headType.graphicPath }
				};
				Gender firstGender = ((!genderIncluded) ? Gender.Male : headType.gender);
				headtypeGraphic.extendedGraphics.Add(new ExtendedConditionGraphic
				{
					conditions = new List<Condition>(1)
					{
						new ConditionGender
						{
							gender = firstGender
						}
					},
					path = headPath + firstGender.ToString() + "_" + headTypePath
				});
				if (!genderIncluded)
				{
					List<AbstractExtendedGraphic> extendedGraphics = headtypeGraphic.extendedGraphics;
					ExtendedConditionGraphic obj = new ExtendedConditionGraphic
					{
						conditions = new List<Condition>(1)
						{
							new ConditionGender
							{
								gender = Gender.Female
							}
						}
					};
					result = Gender.Female;
					obj.path = headPath + result.ToString() + headTypePath;
					extendedGraphics.Add(obj);
				}
				headGraphic.extendedGraphics.Add(headtypeGraphic);
			}
		}
		alienProps.alienRace.graphicPaths.head.resolveData.head = true;
		if (!alienProps.alienRace.graphicPaths.body.GetSubGraphics().Any())
		{
			ExtendedGraphicTop bodyGraphic = alienProps.alienRace.graphicPaths.body;
			string bodyPath = bodyGraphic.path;
			foreach (CreepJoinerFormKindDef formKindDef in DefDatabase<CreepJoinerFormKindDef>.AllDefsListForReading)
			{
				ExtendedConditionGraphic formGraphic = new ExtendedConditionGraphic
				{
					conditions = new List<Condition>(1)
					{
						new ConditionCreepJoinerFormKind
						{
							form = formKindDef
						}
					},
					path = $"{bodyPath}_{formKindDef}"
				};
				foreach (BodyTypeGraphicData bodyTypeData in formKindDef.bodyTypeGraphicPaths)
				{
					formGraphic.extendedGraphics.Add(new ExtendedConditionGraphic
					{
						conditions = new List<Condition>(1)
						{
							new ConditionBodyType
							{
								bodyType = bodyTypeData.bodyType
							}
						},
						path = $"{bodyPath}_{formKindDef}_{bodyTypeData.bodyType}",
						pathsFallback = new List<string>(1) { bodyTypeData.texturePath }
					});
				}
				bodyGraphic.extendedGraphics.Add(formGraphic);
			}
			foreach (MutantDef mutantDef in DefDatabase<MutantDef>.AllDefsListForReading)
			{
				ExtendedConditionGraphic mutantGraphic = new ExtendedConditionGraphic
				{
					conditions = new List<Condition>(1)
					{
						new ConditionMutant
						{
							mutant = mutantDef
						}
					},
					path = $"{bodyPath}_{mutantDef}"
				};
				foreach (BodyTypeGraphicData bodyTypeData2 in mutantDef.bodyTypeGraphicPaths)
				{
					mutantGraphic.extendedGraphics.Add(new ExtendedConditionGraphic
					{
						conditions = new List<Condition>(1)
						{
							new ConditionBodyType
							{
								bodyType = bodyTypeData2.bodyType
							}
						},
						path = $"{bodyPath}_{mutantDef}_{bodyTypeData2.bodyType}",
						pathsFallback = new List<string>(1) { bodyTypeData2.texturePath }
					});
				}
				bodyGraphic.extendedGraphics.Add(mutantGraphic);
			}
			foreach (BodyTypeDef bodyTypeRaw in bodyTypes)
			{
				BodyTypeDef bodyType = ((bodyTypeRaw == BodyTypeDefOf.Baby) ? BodyTypeDefOf.Child : bodyTypeRaw);
				bodyGraphic.extendedGraphics.Add(new ExtendedConditionGraphic
				{
					conditions = new List<Condition>(1)
					{
						new ConditionBodyType
						{
							bodyType = bodyTypeRaw
						}
					},
					path = bodyPath + "Naked_" + bodyType.defName,
					extendedGraphics = new List<AbstractExtendedGraphic>(2)
					{
						new ExtendedConditionGraphic
						{
							conditions = new List<Condition>(1)
							{
								new ConditionGender
								{
									gender = Gender.Male
								}
							},
							path = $"{bodyPath}{Gender.Male}_Naked_{bodyType.defName}"
						},
						new ExtendedConditionGraphic
						{
							conditions = new List<Condition>(1)
							{
								new ConditionGender
								{
									gender = Gender.Female
								}
							},
							path = $"{bodyPath}{Gender.Female}_Naked_{bodyType.defName}"
						}
					}
				});
			}
		}
		foreach (ExtendedGraphicTop graphicTop in alienProps.alienRace.graphicPaths.apparel.individualPaths.Values)
		{
			if (graphicTop.GetSubGraphics().Any())
			{
				continue;
			}
			string path = graphicTop.path;
			foreach (BodyTypeDef bodyType2 in bodyTypes)
			{
				graphicTop.extendedGraphics.Add(new ExtendedConditionGraphic
				{
					conditions = new List<Condition>(1)
					{
						new ConditionBodyType
						{
							bodyType = bodyType2
						}
					},
					path = path + "_" + bodyType2.defName,
					extendedGraphics = new List<AbstractExtendedGraphic>(2)
					{
						new ExtendedConditionGraphic
						{
							conditions = new List<Condition>(1)
							{
								new ConditionGender
								{
									gender = Gender.Male
								}
							},
							path = $"{path}_{Gender.Male}_{bodyType2.defName}"
						},
						new ExtendedConditionGraphic
						{
							conditions = new List<Condition>(1)
							{
								new ConditionGender
								{
									gender = Gender.Female
								}
							},
							path = $"{path}_{Gender.Female}_{bodyType2.defName}"
						}
					}
				});
			}
		}
		foreach (ApparelReplacementOption fallback in alienProps.alienRace.graphicPaths.apparel.fallbacks)
		{
			foreach (ExtendedGraphicTop graphicTop2 in fallback.wornGraphicPaths.Concat(fallback.wornGraphicPath))
			{
				if (graphicTop2.GetSubGraphics().Any())
				{
					continue;
				}
				string path2 = graphicTop2.path;
				foreach (BodyTypeDef bodyType3 in bodyTypes)
				{
					graphicTop2.extendedGraphics.Add(new ExtendedConditionGraphic
					{
						conditions = new List<Condition>(1)
						{
							new ConditionBodyType
							{
								bodyType = bodyType3
							}
						},
						path = path2 + "_" + bodyType3.defName,
						extendedGraphics = new List<AbstractExtendedGraphic>(2)
						{
							new ExtendedConditionGraphic
							{
								conditions = new List<Condition>(1)
								{
									new ConditionGender
									{
										gender = Gender.Male
									}
								},
								path = $"{path2}_{Gender.Male}_{bodyType3.defName}"
							},
							new ExtendedConditionGraphic
							{
								conditions = new List<Condition>(1)
								{
									new ConditionGender
									{
										gender = Gender.Female
									}
								},
								path = $"{path2}_{Gender.Female}_{bodyType3.defName}"
							}
						}
					});
				}
			}
		}
		foreach (ApparelReplacementOption overrides in alienProps.alienRace.graphicPaths.apparel.overrides)
		{
			foreach (ExtendedGraphicTop graphicTop3 in overrides.wornGraphicPaths.Concat(overrides.wornGraphicPath))
			{
				if (graphicTop3.GetSubGraphics().Any())
				{
					continue;
				}
				string path3 = graphicTop3.path;
				foreach (BodyTypeDef bodyType4 in bodyTypes)
				{
					graphicTop3.extendedGraphics.Add(new ExtendedConditionGraphic
					{
						conditions = new List<Condition>(1)
						{
							new ConditionBodyType
							{
								bodyType = bodyType4
							}
						},
						path = path3 + "_" + bodyType4.defName,
						extendedGraphics = new List<AbstractExtendedGraphic>(2)
						{
							new ExtendedConditionGraphic
							{
								conditions = new List<Condition>(1)
								{
									new ConditionGender
									{
										gender = Gender.Male
									}
								},
								path = $"{path3}_{Gender.Male}_{bodyType4.defName}"
							},
							new ExtendedConditionGraphic
							{
								conditions = new List<Condition>(1)
								{
									new ConditionGender
									{
										gender = Gender.Female
									}
								},
								path = $"{path3}_{Gender.Female}_{bodyType4.defName}"
							}
						}
					});
				}
			}
		}
		alienProps.alienRace.graphicPaths.apparel.pathPrefix.Init();
		if (!alienProps.alienRace.graphicPaths.apparel.pathPrefix.GetPath().NullOrEmpty())
		{
			alienProps.alienRace.graphicPaths.apparel.pathPrefix.IncrementVariantCount();
		}
		Stack<IEnumerable<IExtendedGraphic>> stack = new Stack<IEnumerable<IExtendedGraphic>>();
		stack.Push(alienProps.alienRace.graphicPaths.apparel.pathPrefix.GetSubGraphics());
		while (stack.Count > 0)
		{
			IEnumerable<IExtendedGraphic> currentGraphicSet = stack.Pop();
			foreach (IExtendedGraphic current in currentGraphicSet)
			{
				if (current != null)
				{
					current.Init();
					if (!current.GetPath().NullOrEmpty())
					{
						current.IncrementVariantCount();
					}
					stack.Push(current.GetSubGraphics());
				}
			}
		}
		alienProps.alienRace.graphicPaths.skull.resolveData.head = true;
		alienProps.alienRace.graphicPaths.stump.resolveData.head = true;
		alienProps.alienRace.graphicPaths.headMasks.resolveData.head = true;
		if (!graphicsQueue.Any())
		{
			Application.onBeforeRender += LoadGraphicsHook;
		}
		graphicsQueue.Add(this);
		offsetDefaultsDictionary = new Dictionary<string, OffsetNamed>();
		foreach (OffsetNamed offsetDefault in offsetDefaults)
		{
			offsetDefaultsDictionary[offsetDefault.name] = offsetDefault;
		}
		foreach (BodyAddon bodyAddon in bodyAddons)
		{
			bodyAddon.defaultOffsets = offsetDefaultsDictionary[bodyAddon.defaultOffset].offsets;
		}
	}

	public static void LoadGraphicsHook()
	{
		ModContentHolder<Texture2D> contentHolder = AlienRaceMod.instance.Content.GetContentHolder<Texture2D>();
		bool? obj;
		if (contentHolder == null)
		{
			obj = null;
		}
		else
		{
			Dictionary<string, Texture2D> contentList = contentHolder.contentList;
			obj = ((contentList != null) ? new bool?(!contentList.Any()) : ((bool?)null));
		}
		if (obj ?? true)
		{
			return;
		}
		foreach (AlienPartGenerator apg in graphicsQueue)
		{
			graphicsLoader.LoadAllGraphics(apg.alienProps.defName, apg.alienProps.alienRace.graphicPaths.head, apg.alienProps.alienRace.graphicPaths.body, apg.alienProps.alienRace.graphicPaths.skeleton, apg.alienProps.alienRace.graphicPaths.skull, apg.alienProps.alienRace.graphicPaths.stump, apg.alienProps.alienRace.graphicPaths.bodyMasks, apg.alienProps.alienRace.graphicPaths.headMasks);
			graphicsLoader.LoadAllGraphics(apg.alienProps.defName, apg.alienProps.alienRace.graphicPaths.apparel.individualPaths.Values.Concat(apg.alienProps.alienRace.graphicPaths.apparel.fallbacks.SelectMany((ApparelReplacementOption afo) => afo.wornGraphicPaths.Concat(afo.wornGraphicPath))).Concat(apg.alienProps.alienRace.graphicPaths.apparel.overrides.SelectMany((ApparelReplacementOption afo) => afo.wornGraphicPaths.Concat(afo.wornGraphicPath))).ToArray());
			graphicsLoader.LoadAllGraphics(apg.alienProps.defName + " Addons", apg.bodyAddons.Cast<ExtendedGraphicTop>().ToArray());
		}
		graphicsQueue.Clear();
		Application.onBeforeRender -= LoadGraphicsHook;
	}
}
