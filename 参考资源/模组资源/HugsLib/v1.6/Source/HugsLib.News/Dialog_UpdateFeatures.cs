using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.News;

/// <summary>
/// Displays a list to update feature defs with basic image and formatting support. See <see cref="T:HugsLib.UpdateFeatureDef" /> for proper syntax.
/// </summary>
public class Dialog_UpdateFeatures : Window
{
	private class FeatureEntry
	{
		public readonly UpdateFeatureDef def;

		public readonly List<DescriptionSegment> segments;

		public FeatureEntry(UpdateFeatureDef def, List<DescriptionSegment> segments)
		{
			this.def = def;
			this.segments = segments;
		}
	}

	private class DescriptionSegment
	{
		public enum SegmentType
		{
			Text,
			Image,
			Caption
		}

		public readonly SegmentType type;

		public string[] imageNames;

		public float expectedHeight;

		private float expectedWidth;

		public string text;

		private List<Texture2D> cachedTextures;

		public DescriptionSegment(SegmentType type)
		{
			this.type = type;
		}

		public float CalculateHeight(GUIStyle style, float width, Dictionary<string, Texture2D> images)
		{
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Expected O, but got Unknown
			if (type == SegmentType.Image && imageNames != null)
			{
				if (cachedTextures == null)
				{
					cachedTextures = CacheOwnTextures(images);
				}
				return expectedHeight;
			}
			if (type == SegmentType.Caption && text != null)
			{
				return 0f;
			}
			if (type == SegmentType.Text && text != null)
			{
				return expectedHeight = style.CalcHeight(new GUIContent(text), width) + 4f;
			}
			return 0f;
		}

		public void Draw(Rect rect, ref float curY, Dictionary<string, Texture2D> images, DescriptionSegment previousSegment, bool skipDrawing)
		{
			if (type == SegmentType.Image && imageNames != null)
			{
				if (!skipDrawing)
				{
					if (cachedTextures == null)
					{
						cachedTextures = CacheOwnTextures(images);
					}
					float num = rect.x;
					for (int i = 0; i < cachedTextures.Count; i++)
					{
						Texture2D texture2D = cachedTextures[i];
						Rect outerRect = new Rect(num, curY + 6f, texture2D.width, texture2D.height);
						Widgets.DrawTextureFitted(outerRect, texture2D, 1f);
						num += (float)texture2D.width + 6f;
					}
				}
				curY += expectedHeight;
			}
			else if (type == SegmentType.Caption && text != null && previousSegment != null)
			{
				float num2 = previousSegment.expectedWidth + 6f;
				Rect rect2 = new Rect(num2, curY - previousSegment.expectedHeight, rect.width - num2, previousSegment.expectedHeight);
				GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
				Widgets.Label(rect2, text);
				GenUI.ResetLabelAlign();
			}
			else if (type == SegmentType.Text && text != null)
			{
				if (!skipDrawing)
				{
					Rect rect3 = new Rect(rect.x, curY + 2f, rect.width, expectedHeight);
					Widgets.Label(rect3, text);
				}
				curY += expectedHeight;
			}
		}

		private List<Texture2D> CacheOwnTextures(Dictionary<string, Texture2D> images)
		{
			List<Texture2D> list = new List<Texture2D>(imageNames.Length);
			expectedHeight = (expectedWidth = 0f);
			for (int i = 0; i < imageNames.Length; i++)
			{
				if (images.TryGetValue(imageNames[i], out var value) && !(value == null))
				{
					list.Add(value);
					if ((float)value.height > expectedHeight)
					{
						expectedHeight = value.height;
					}
					expectedWidth += value.width;
				}
			}
			expectedHeight += 12f;
			expectedWidth += (float)list.Count * 6f;
			return list;
		}
	}

	private const float HeaderLabelHeight = 40f;

	private const float EntryTitleLabelHeight = 40f;

	private const float EntryTitleLabelPadding = 4f;

	private const float EntryTitleLinkPadding = 7f;

	private const float EntryTitleHeight = 44f;

	private const float EntryContentIndent = 4f;

	private const float EntryFooterHeight = 16f;

	private const float ScrollBarWidthMargin = 18f;

	private const int EntryTitleFontSize = 18;

	private const float SegmentImageMargin = 6f;

	private const float SegmentTextMargin = 2f;

	private readonly UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders;

	private readonly Color TitleLineColor = new Color(0.3f, 0.3f, 0.3f);

	private readonly Color LinkTextColor = new Color(0.7f, 0.7f, 1f);

	private readonly Dictionary<string, Texture2D> resolvedImages = new Dictionary<string, Texture2D>();

	private readonly string ignoreToggleTip = "HugsLib_features_ignoreTooltip".Translate();

	private readonly float linkTextWidth;

	private List<FeatureEntry> entries;

	private float totalContentHeight = -1f;

	private Vector2 scrollPosition;

	private bool anyImagesPending;

	public override Vector2 InitialSize => new Vector2(650f, 700f);

	public Dialog_UpdateFeatures(IEnumerable<UpdateFeatureDef> featureDefs, UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders)
	{
		this.ignoredNewsProviders = ignoredNewsProviders;
		closeOnCancel = true;
		doCloseButton = false;
		doCloseX = true;
		forcePause = true;
		draggable = true;
		absorbInputAroundWindow = false;
		resizeable = false;
		linkTextWidth = GetLinkTextWidth() + 14f;
		InstallUpdateFeatureDefs(featureDefs);
	}

	public override void Close(bool doCloseSound = true)
	{
		base.Close(doCloseSound);
		DestroyLoadedImages();
	}

	public override void DoWindowContents(Rect inRect)
	{
		float y = Window.CloseButSize.y;
		float num = y + (Margin - 10f);
		Rect rect = new Rect(0f, 0f, inRect.width, inRect.height - num).ContractedBy(10f);
		GUI.BeginGroup(rect);
		Rect rect2 = new Rect(0f, 0f, rect.width, 40f);
		Text.Font = GameFont.Medium;
		GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
		Widgets.Label(rect2, "HugsLib_features_title".Translate());
		if (Mouse.IsOver(rect2))
		{
			Widgets.DrawHighlight(rect2);
			TooltipHandler.TipRegion(rect2, "HugsLib_features_description".Translate());
		}
		GenUI.ResetLabelAlign();
		if (!anyImagesPending)
		{
			Text.Font = GameFont.Small;
			Rect outRect = new Rect(0f, rect2.height, rect.width, rect.height - rect2.height);
			if (!(totalContentHeight > outRect.height))
			{
				outRect.x += 9f;
			}
			float num2 = outRect.width - 18f;
			if (totalContentHeight < 0f)
			{
				CalculateContentHeight(num2);
			}
			Rect viewRect = new Rect(0f, 0f, num2, totalContentHeight);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			float curY = 0f;
			for (int i = 0; i < entries.Count; i++)
			{
				FeatureEntry featureEntry = entries[i];
				bool skipDrawing = curY - scrollPosition.y + 44f < 0f || curY - scrollPosition.y > outRect.height;
				DrawEntryTitle(featureEntry, viewRect.width, ref curY, skipDrawing);
				Rect rect3 = new Rect(4f, 0f, viewRect.width - 4f, viewRect.height);
				DescriptionSegment previousSegment = null;
				for (int j = 0; j < featureEntry.segments.Count; j++)
				{
					DescriptionSegment descriptionSegment = featureEntry.segments[j];
					skipDrawing = curY - scrollPosition.y + descriptionSegment.expectedHeight < 0f || curY - scrollPosition.y > outRect.height;
					descriptionSegment.Draw(rect3, ref curY, resolvedImages, previousSegment, skipDrawing);
					previousSegment = descriptionSegment;
				}
				curY += 16f;
			}
			Widgets.EndScrollView();
		}
		GUI.EndGroup();
		DrawBottomButtonRow(inRect.BottomPartPixels(y));
	}

	protected void InstallUpdateFeatureDefs(IEnumerable<UpdateFeatureDef> featureDefs)
	{
		DestroyLoadedImages();
		GenerateDrawableEntries(featureDefs.ToList());
		totalContentHeight = -1f;
	}

	protected void ResetScrollPosition()
	{
		scrollPosition = Vector2.zero;
	}

	protected virtual void DrawBottomButtonRow(Rect inRect)
	{
		Rect inRect2 = new Rect(inRect.width / 2f - Window.CloseButSize.x / 2f, inRect.y, Window.CloseButSize.x, Window.CloseButSize.y);
		DrawCloseButton(inRect2);
	}

	protected void DrawCloseButton(Rect inRect)
	{
		if (Widgets.ButtonText(inRect, "CloseButton".Translate()))
		{
			Close();
		}
	}

	private void DrawEntryTitle(FeatureEntry entry, float width, ref float curY, bool skipDrawing)
	{
		if (!skipDrawing)
		{
			Vector2 togglePos = new Vector2(4f, curY + 8f);
			DoIgnoreNewsProviderToggle(togglePos, entry);
			Rect rect = new Rect(togglePos.x + 24f, curY, width, 40f).ContractedBy(4f);
			Text.Font = GameFont.Medium;
			string arg = entry.def.titleOverride ?? ((string)"HugsLib_features_update".Translate(entry.def.modNameReadable, entry.def.assemblyVersion));
			Widgets.Label(rect, $"<size={18}>{arg}</size>");
			Text.Font = GameFont.Small;
			DrawEntryTitleWidgets(new Rect(0f, curY, width, 40f), entry.def);
		}
		curY += 40f;
		if (!skipDrawing)
		{
			Color color = GUI.color;
			GUI.color = TitleLineColor;
			Widgets.DrawLineHorizontal(0f, curY, width);
			GUI.color = color;
		}
		curY += 4f;
	}

	protected virtual void DrawEntryTitleWidgets(Rect titleRect, UpdateFeatureDef forDef)
	{
		DrawEntryLinkWidget(titleRect, forDef);
	}

	protected float DrawEntryLinkWidget(Rect titleRect, UpdateFeatureDef forDef)
	{
		if (forDef.linkUrl != null)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Rect rect = new Rect(titleRect.width - linkTextWidth, titleRect.y, linkTextWidth, titleRect.height);
			Color color = GUI.color;
			GUI.color = LinkTextColor;
			Widgets.Label(rect, "HugsLib_features_link".Translate());
			GUI.color = color;
			GenUI.ResetLabelAlign();
			if (Widgets.ButtonInvisible(rect))
			{
				Application.OpenURL(forDef.linkUrl);
			}
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
				TooltipHandler.TipRegion(rect, "HugsLib_features_linkDesc".Translate().Replace("{0}", forDef.linkUrl));
			}
			return linkTextWidth;
		}
		return 0f;
	}

	private void DoIgnoreNewsProviderToggle(Vector2 togglePos, FeatureEntry entry)
	{
		string ownerId = entry.def.OwningModId;
		bool isOn;
		bool flag = (isOn = !ignoredNewsProviders.Contains(ownerId));
		Widgets.Checkbox(togglePos, ref isOn);
		if (flag != isOn)
		{
			if (isOn || HugsLibUtility.ShiftIsHeld)
			{
				ToggleIgnoredState();
			}
			else
			{
				Find.WindowStack.Add(new HugsLib.Utils.Dialog_Confirm("HugsLib_features_confirmIgnore".Translate(entry.def.modNameReadable), ToggleIgnoredState, destructive: false, "HugsLib_features_confirmIgnoreTitle".Translate()));
			}
		}
		Rect rect = new Rect(togglePos.x, togglePos.y, 24f, 24f);
		TooltipHandler.TipRegion(rect, ignoreToggleTip);
		void ToggleIgnoredState()
		{
			ignoredNewsProviders.SetIgnored(ownerId, !isOn);
		}
	}

	private void CalculateContentHeight(float textWidth)
	{
		GUIStyle labelStyle = GetLabelStyle();
		totalContentHeight = 0f;
		foreach (FeatureEntry entry in entries)
		{
			totalContentHeight += 44f;
			foreach (DescriptionSegment segment in entry.segments)
			{
				totalContentHeight += segment.CalculateHeight(labelStyle, textWidth, resolvedImages);
			}
		}
		totalContentHeight += 16f * (float)entries.Count;
	}

	private void GenerateDrawableEntries(List<UpdateFeatureDef> defs)
	{
		entries = new List<FeatureEntry>(defs.Count);
		List<(ModContentPack, string)> list = new List<(ModContentPack, string)>();
		foreach (UpdateFeatureDef def in defs)
		{
			entries.Add(new FeatureEntry(def, ParseEntryContent(def.content, def.trimWhitespace, out var requiredImages)));
			foreach (string item in requiredImages)
			{
				list.Add((def.modContentPack, item));
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		anyImagesPending = true;
		IEnumerable<IGrouping<ModContentPack, string>> requiredImagesGroupedByMod = from pair in list
			group pair.fileName by pair.pack;
		HugsLibController.Instance.DoLater.DoNextUpdate(delegate
		{
			foreach (IGrouping<ModContentPack, string> item2 in requiredImagesGroupedByMod)
			{
				ResolveImagesForMod(item2.Key, item2);
			}
			anyImagesPending = false;
		});
	}

	private void ResolveImagesForMod(ModContentPack mod, IEnumerable<string> imageFileNames)
	{
		foreach (KeyValuePair<string, Texture2D> item in UpdateFeatureImageLoader.LoadImagesForMod(mod, imageFileNames))
		{
			resolvedImages[item.Key] = item.Value;
		}
	}

	private void DestroyLoadedImages()
	{
		foreach (Texture2D value in resolvedImages.Values)
		{
			UnityEngine.Object.Destroy(value);
		}
		resolvedImages.Clear();
	}

	private List<DescriptionSegment> ParseEntryContent(string content, bool trimWhitespace, out IEnumerable<string> requiredImages)
	{
		List<string> list = (List<string>)(requiredImages = new List<string>());
		try
		{
			content = content.Replace("\\n", "\n");
			string[] array = content.Split('|');
			List<DescriptionSegment> list2 = new List<DescriptionSegment>();
			string[] array2 = array;
			foreach (string text in array2)
			{
				string text2 = text;
				if (trimWhitespace)
				{
					text2 = text2.Trim();
				}
				string[] array3 = null;
				string text3 = null;
				DescriptionSegment.SegmentType type;
				if (text2.StartsWith("img:"))
				{
					type = DescriptionSegment.SegmentType.Image;
					string[] array4 = text2.Split(':');
					if (array4[1].Length == 0)
					{
						continue;
					}
					array3 = (from s in array4[1].Split(',')
						where s.Length > 0
						select s).ToArray();
					for (int num = 0; num < array3.Length; num++)
					{
						list.Add(array3[num]);
					}
				}
				else if (text2.StartsWith("caption:"))
				{
					if (list2.Count == 0 || list2[list2.Count - 1].type != DescriptionSegment.SegmentType.Image)
					{
						HugsLibController.Logger.Warning("Improperly formatted feature content. Caption must follow img. Content:" + content);
						continue;
					}
					type = DescriptionSegment.SegmentType.Caption;
					text3 = ((text2.Length <= "caption:".Length) ? "" : text2.Substring("caption:".Length));
				}
				else
				{
					type = DescriptionSegment.SegmentType.Text;
					text3 = text2;
				}
				if (text3 != null && trimWhitespace)
				{
					text3 = text3.Trim();
				}
				DescriptionSegment item = new DescriptionSegment(type)
				{
					imageNames = array3,
					text = text3
				};
				list2.Add(item);
			}
			return list2;
		}
		catch (Exception ex)
		{
			HugsLibController.Logger.Warning("Failed to parse UpdateFeatureDef content: {0} \n Exception was: {1}", content, ex);
			return new List<DescriptionSegment>();
		}
	}

	private GUIStyle GetLabelStyle()
	{
		GUIStyle val = Text.fontStyles[1];
		val.alignment = TextAnchor.UpperLeft;
		val.wordWrap = true;
		return val;
	}

	private static float GetLinkTextWidth()
	{
		GameFont font = Text.Font;
		Text.Font = GameFont.Small;
		float x = Text.CalcSize("HugsLib_features_link".Translate()).x;
		Text.Font = font;
		return x;
	}
}
