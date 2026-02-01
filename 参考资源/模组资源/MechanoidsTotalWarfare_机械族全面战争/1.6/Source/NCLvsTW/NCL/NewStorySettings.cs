using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class NewStorySettings : Page
{
	private enum PageType
	{
		Main,
		Page1,
		Page2,
		Page3
	}

	private float width = 600f;

	private float height = 700f;

	private Vector2 scrollPos = Vector2.zero;

	private List<Texture2D> carouselImages = new List<Texture2D>();

	private int currentImageIndex = 0;

	private float lastImageChangeTime;

	private const float IMAGE_CHANGE_INTERVAL = 3f;

	private const float CAROUSEL_HEIGHT = 160f;

	private Texture2D leftArrowTexture;

	private Texture2D rightArrowTexture;

	private Texture2D backgroundImage;

	private const float MAIN_TEXT_AREA_HEIGHT = 280f;

	private const float SUB_TEXT_AREA_HEIGHT = 460f;

	private readonly int windowId = 11459;

	private PageType currentPage = PageType.Main;

	private Dictionary<PageType, string> pageContents;

	private bool isVersionUpdate = false;

	private string currentVersion = "";

	private Dictionary<PageType, Texture2D> backgroundImages = new Dictionary<PageType, Texture2D>();

	public NewStorySettings(bool isVersionUpdate = false, string version = "")
	{
		doCloseButton = true;
		closeOnCancel = true;
		this.isVersionUpdate = isVersionUpdate;
		currentVersion = version;
		lastImageChangeTime = Time.realtimeSinceStartup;
		InitializeImages();
		InitializePageContents();
	}

	private void InitializePageContents()
	{
		string updatePrefix = (isVersionUpdate ? ((string)"NCL_Content_UpdatePrefix".Translate(currentVersion)) : "");
		pageContents = new Dictionary<PageType, string>
		{
			{
				PageType.Main,
				updatePrefix + "NCL_Content_Main".Translate(currentVersion)
			},
			{
				PageType.Page1,
				updatePrefix + "NCL_Content_Page1".Translate(currentVersion)
			},
			{
				PageType.Page2,
				updatePrefix + "NCL_Content_Page2".Translate(currentVersion)
			},
			{
				PageType.Page3,
				updatePrefix + "NCL_Content_Page3".Translate(currentVersion)
			}
		};
	}

	private void InitializeImages()
	{
		try
		{
			backgroundImages[PageType.Main] = ContentFinder<Texture2D>.Get("ModIcon/BackGround", reportFailure: false);
			if (backgroundImages[PageType.Main] == null)
			{
				backgroundImages[PageType.Main] = CreateDefaultTexture(600, 700, new Color(0.25f, 0.25f, 0.25f));
			}
			backgroundImages[PageType.Page1] = ContentFinder<Texture2D>.Get("ModIcon/BackGround", reportFailure: false);
			if (backgroundImages[PageType.Page1] == null)
			{
				backgroundImages[PageType.Page1] = CreateDefaultTexture(600, 700, new Color(0.3f, 0.2f, 0.2f));
			}
			backgroundImages[PageType.Page2] = ContentFinder<Texture2D>.Get("ModIcon/BackGround", reportFailure: false);
			if (backgroundImages[PageType.Page2] == null)
			{
				backgroundImages[PageType.Page2] = CreateDefaultTexture(600, 700, new Color(0.2f, 0.3f, 0.2f));
			}
			backgroundImages[PageType.Page3] = ContentFinder<Texture2D>.Get("ModIcon/BackGround", reportFailure: false);
			if (backgroundImages[PageType.Page3] == null)
			{
				backgroundImages[PageType.Page3] = CreateDefaultTexture(600, 700, new Color(0.2f, 0.2f, 0.3f));
			}
			Texture2D image1 = ContentFinder<Texture2D>.Get("ModIcon/ModAbout", reportFailure: false);
			Texture2D image2 = ContentFinder<Texture2D>.Get("ModIcon/ModAbout", reportFailure: false);
			Texture2D image3 = ContentFinder<Texture2D>.Get("ModIcon/ModUpdate", reportFailure: false);
			carouselImages.Add(image1 ?? CreateDefaultTexture(400, 160, Color.white));
			carouselImages.Add(image2 ?? CreateDefaultTexture(400, 160, Color.white));
			carouselImages.Add(image3 ?? CreateDefaultTexture(400, 160, Color.white));
			leftArrowTexture = ContentFinder<Texture2D>.Get("ModIcon/AboutLeft", reportFailure: false) ?? CreateDefaultTexture(30, 30, Color.white);
			rightArrowTexture = ContentFinder<Texture2D>.Get("ModIcon/AboutRight", reportFailure: false) ?? CreateDefaultTexture(30, 30, Color.white);
		}
		catch (Exception ex)
		{
			Log.Error("加载图片资源时出错: " + ex.Message);
			foreach (PageType pageType in Enum.GetValues(typeof(PageType)))
			{
				Color bgColor = pageType switch
				{
					PageType.Main => new Color(0.25f, 0.25f, 0.25f), 
					PageType.Page1 => new Color(0.3f, 0.2f, 0.2f), 
					PageType.Page2 => new Color(0.2f, 0.3f, 0.2f), 
					PageType.Page3 => new Color(0.2f, 0.2f, 0.3f), 
					_ => new Color(0.25f, 0.25f, 0.25f), 
				};
				backgroundImages[pageType] = CreateDefaultTexture(600, 700, bgColor);
			}
			if (carouselImages.Count == 0)
			{
				carouselImages.Add(CreateDefaultTexture(400, 160, Color.white));
				carouselImages.Add(CreateDefaultTexture(400, 160, Color.white));
				carouselImages.Add(CreateDefaultTexture(400, 160, Color.white));
			}
			leftArrowTexture = CreateDefaultTexture(30, 30, Color.white);
			rightArrowTexture = CreateDefaultTexture(30, 30, Color.white);
		}
	}

	private Texture2D CreateDefaultTexture(int width, int height, Color color)
	{
		Texture2D tex = new Texture2D(width, height);
		Color[] pixels = new Color[width * height];
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = color;
		}
		tex.SetPixels(pixels);
		tex.Apply();
		return tex;
	}

	protected override void SetInitialSizeAndPosition()
	{
		windowRect = new Rect(((float)UI.screenWidth - width) / 2f, ((float)UI.screenHeight - height) / 2f, width, height);
		windowRect = windowRect.Rounded();
	}

	public override void PreOpen()
	{
		base.PreOpen();
		SetInitialSizeAndPosition();
	}

	public override void WindowOnGUI()
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		if (currentPage == PageType.Main)
		{
			UpdateCarousel();
		}
		Color oldColor = GUI.color;
		GUI.color = new Color(1f, 1f, 1f, 0f);
		Rect inRect = windowRect.AtZero();
		GUI.Window(windowId, windowRect, new WindowFunction(DoWindowContentsWrapper), "", GUI.skin.window);
		GUI.color = oldColor;
		if ((int)Event.current.type == 7 && backgroundImages.ContainsKey(currentPage))
		{
			GUI.DrawTexture(windowRect, (Texture)backgroundImages[currentPage]);
		}
	}

	private void DoWindowContentsWrapper(int id)
	{
		Rect inRect = new Rect(0f, 0f, windowRect.width, windowRect.height).ContractedBy(18f);
		DoWindowContents(inRect);
		GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 30f));
	}

	public override void DoWindowContents(Rect wrect)
	{
		GUI.enabled = true;
		Listing_Standard options = new Listing_Standard();
		options.Begin(wrect);
		if (currentPage == PageType.Main)
		{
			Rect carouselRect = options.GetRect(160f);
			DrawCarousel(carouselRect);
			options.Gap(10f);
		}
		Rect subtitleRect = options.GetRect(40f);
		DrawStyledText(subtitleRect, "NCL_Subtitle".Translate());
		options.Gap(15f);
		string titleText = "";
		if (isVersionUpdate)
		{
			titleText = currentPage switch
			{
				PageType.Main => "NCL_Title_UpdateWithPage".Translate(currentVersion, "NCL_Title_Main".Translate()), 
				PageType.Page1 => "NCL_Title_UpdateWithPage".Translate(currentVersion, "NCL_Title_Page1".Translate()), 
				PageType.Page2 => "NCL_Title_UpdateWithPage".Translate(currentVersion, "NCL_Title_Page2".Translate()), 
				PageType.Page3 => "NCL_Title_UpdateWithPage".Translate(currentVersion, "NCL_Title_Page3".Translate()), 
				_ => "NCL_Title_UpdateWithPage".Translate(currentVersion, "NCL_Title_Main".Translate()), 
			};
		}
		else
		{
			switch (currentPage)
			{
			case PageType.Main:
				titleText = "NCL_Title_Main".Translate();
				break;
			case PageType.Page1:
				titleText = "NCL_Title_Page1".Translate();
				break;
			case PageType.Page2:
				titleText = "NCL_Title_Page2".Translate();
				break;
			case PageType.Page3:
				titleText = "NCL_Title_Page3".Translate();
				break;
			}
		}
		Rect titleRect = options.GetRect(40f);
		DrawStyledText(titleRect, titleText, GameFont.Small, TextAnchor.MiddleCenter, isVersionUpdate ? Color.yellow : Color.white);
		float textAreaHeight = ((currentPage == PageType.Main) ? 280f : 460f);
		Rect outRect = options.GetRect(textAreaHeight);
		Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, 600f);
		Widgets.DrawBoxSolid(outRect, new Color(0f, 0f, 0f, 0.2f));
		Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
		Listing_Standard textContent = new Listing_Standard();
		textContent.Begin(viewRect);
		textContent.Label(pageContents[currentPage]);
		textContent.End();
		Widgets.EndScrollView();
		options.Gap(15f);
		if (currentPage != PageType.Main)
		{
			Rect returnButtonRect = options.GetRect(30f);
			returnButtonRect.width = 120f;
			returnButtonRect.x = (wrect.width - returnButtonRect.width) / 2f;
			Texture2D returnButtonTexture = ContentFinder<Texture2D>.Get("ModIcon/Taptaptap", reportFailure: false);
			if (DrawCustomButton(returnButtonRect, "NCL_Button_Return".Translate(), returnButtonTexture))
			{
				currentPage = PageType.Main;
				scrollPos = Vector2.zero;
			}
			options.Gap(10f);
		}
		if (currentPage == PageType.Main)
		{
			Rect checkboxBgRect = options.GetRect(Text.LineHeight + 10f);
			checkboxBgRect.width = 310f;
			Widgets.DrawBoxSolid(checkboxBgRect, new Color(0f, 0f, 0f, 0.2f));
			Rect checkboxRect = checkboxBgRect.ContractedBy(5f);
			bool checkboxValue = ModSettingsAbout.GG_Disable_Settings_Window;
			Rect boxRect = new Rect(checkboxRect.x, checkboxRect.y, 24f, 24f);
			Rect labelRect = new Rect(checkboxRect.x + 28f, checkboxRect.y, checkboxRect.width - 28f, checkboxRect.height);
			if (Widgets.ButtonInvisible(boxRect) || Widgets.ButtonInvisible(labelRect))
			{
				checkboxValue = (ModSettingsAbout.GG_Disable_Settings_Window = !checkboxValue);
				Log.Message($"更改禁用窗口设置为: {checkboxValue}");
			}
			Widgets.CheckboxDraw(boxRect.x, boxRect.y, checkboxValue, disabled: true);
			string labelText = (isVersionUpdate ? "NCL_Checkbox_DisableWindowWithUpdate".Translate() : "NCL_Checkbox_DisableWindow".Translate());
			Widgets.Label(labelRect, labelText);
		}
		options.Gap(15f);
		if (currentPage == PageType.Main)
		{
			Rect buttonsAreaRect = options.GetRect(24f);
			float buttonWidth = (buttonsAreaRect.width - 40f - 50f) / 3f;
			Rect button1Rect = new Rect(buttonsAreaRect.x, buttonsAreaRect.y, buttonWidth, 40f);
			if (DrawCustomButton(button1Rect, "NCL_Button_Page1".Translate(), ContentFinder<Texture2D>.Get("ModIcon/Taptaptap", reportFailure: false)))
			{
				currentPage = PageType.Page1;
				scrollPos = Vector2.zero;
			}
			Rect button2Rect = new Rect(buttonsAreaRect.x + buttonWidth + 20f, buttonsAreaRect.y, buttonWidth, 40f);
			if (DrawCustomButton(button2Rect, "NCL_Button_Page2".Translate(), ContentFinder<Texture2D>.Get("ModIcon/Taptaptap", reportFailure: false)))
			{
				currentPage = PageType.Page2;
				scrollPos = Vector2.zero;
			}
			Rect button3Rect = new Rect(buttonsAreaRect.x + (buttonWidth + 20f) * 2f, buttonsAreaRect.y, buttonWidth, 40f);
			if (DrawCustomButton(button3Rect, "NCL_Button_Page3".Translate(), ContentFinder<Texture2D>.Get("ModIcon/Taptaptap", reportFailure: false)))
			{
				currentPage = PageType.Page3;
				scrollPos = Vector2.zero;
			}
			if (doCloseButton)
			{
				float closeButtonWidth = 40f;
				float closeButtonHeight = 40f;
				Rect closeRect = new Rect(buttonsAreaRect.x + buttonsAreaRect.width - closeButtonWidth, buttonsAreaRect.y, closeButtonWidth, closeButtonHeight);
				Texture2D closeButtonTexture = ContentFinder<Texture2D>.Get("ModIcon/NoNoNo", reportFailure: false);
				if (DrawCustomButton(closeRect, "", closeButtonTexture))
				{
					Close();
				}
			}
		}
		options.End();
	}

	private bool DrawCustomButton(Rect rect, string label, Texture2D customTexture)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		if (customTexture != null)
		{
			GUI.DrawTexture(rect, (Texture)customTexture);
		}
		else
		{
			Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.8f));
		}
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		if (!string.IsNullOrEmpty(label))
		{
			GameFont originalFont = Text.Font;
			TextAnchor originalAnchor = Text.Anchor;
			Color originalColor = GUI.color;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = Color.white;
			GUIStyle boldStyle = new GUIStyle(Text.CurFontStyle)
			{
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter
			};
			Rect shadowRect = rect;
			shadowRect.x += 1f;
			shadowRect.y += 1f;
			GUI.color = new Color(0f, 0f, 0f, 0.8f);
			GUI.Label(shadowRect, label, boldStyle);
			GUI.color = Color.white;
			GUI.Label(rect, label, boldStyle);
			Text.Font = originalFont;
			Text.Anchor = originalAnchor;
			GUI.color = originalColor;
		}
		return Widgets.ButtonInvisible(rect);
	}

	private void DrawStyledText(Rect rect, string text, GameFont font = GameFont.Small, TextAnchor anchor = TextAnchor.MiddleCenter, Color? textColor = null)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		Color color = textColor ?? Color.white;
		GameFont originalFont = Text.Font;
		TextAnchor originalAnchor = Text.Anchor;
		Color originalColor = GUI.color;
		Text.Font = font;
		Text.Anchor = anchor;
		GUIStyle boldStyle = new GUIStyle(Text.CurFontStyle)
		{
			fontStyle = FontStyle.Bold,
			alignment = anchor
		};
		Rect shadowRect = rect;
		shadowRect.x += 1f;
		shadowRect.y += 1f;
		GUI.color = new Color(0f, 0f, 0f, 0.8f);
		GUI.Label(shadowRect, text, boldStyle);
		GUI.color = color;
		GUI.Label(rect, text, boldStyle);
		Text.Font = originalFont;
		Text.Anchor = originalAnchor;
		GUI.color = originalColor;
	}

	private void DrawCarousel(Rect rect)
	{
		if (carouselImages.Count == 0)
		{
			return;
		}
		Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.3f));
		Rect imageRect = rect.ContractedBy(4f);
		GUI.DrawTexture(imageRect, (Texture)carouselImages[currentImageIndex], (ScaleMode)2);
		int nextImageIndex = (currentImageIndex + 1) % carouselImages.Count;
		int prevImageIndex = (currentImageIndex - 1 + carouselImages.Count) % carouselImages.Count;
		float previewWidth = 100f;
		float previewHeight = 60f;
		float buttonWidth = 30f;
		float buttonHeight = 30f;
		float buttonPadding = 5f;
		Rect leftPreviewRect = new Rect(rect.x + buttonWidth + buttonPadding * 2f, rect.y + (rect.height - previewHeight) / 2f, previewWidth, previewHeight);
		Rect leftArrowRect = new Rect(leftPreviewRect.x - buttonWidth - buttonPadding, rect.y + (rect.height - buttonHeight) / 2f, buttonWidth, buttonHeight);
		Rect rightPreviewRect = new Rect(rect.x + rect.width - previewWidth - buttonWidth - buttonPadding * 2f, rect.y + (rect.height - previewHeight) / 2f, previewWidth, previewHeight);
		Rect rightArrowRect = new Rect(rightPreviewRect.x + previewWidth + buttonPadding, rect.y + (rect.height - buttonHeight) / 2f, buttonWidth, buttonHeight);
		Color oldColor = GUI.color;
		Widgets.DrawBox(leftPreviewRect, 2);
		GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
		GUI.DrawTexture(leftPreviewRect, (Texture)carouselImages[prevImageIndex], (ScaleMode)2);
		Widgets.DrawBox(rightPreviewRect, 2);
		GUI.DrawTexture(rightPreviewRect, (Texture)carouselImages[nextImageIndex], (ScaleMode)2);
		GUI.color = oldColor;
		if (leftArrowTexture != null)
		{
			GUI.color = Color.white;
			GUI.DrawTexture(leftArrowRect, (Texture)leftArrowTexture, (ScaleMode)2);
			if (Widgets.ButtonInvisible(leftArrowRect))
			{
				currentImageIndex = prevImageIndex;
				lastImageChangeTime = Time.realtimeSinceStartup;
			}
		}
		else if (Widgets.ButtonText(leftArrowRect, "◀"))
		{
			currentImageIndex = prevImageIndex;
			lastImageChangeTime = Time.realtimeSinceStartup;
		}
		if (rightArrowTexture != null)
		{
			GUI.color = Color.white;
			GUI.DrawTexture(rightArrowRect, (Texture)rightArrowTexture, (ScaleMode)2);
			if (Widgets.ButtonInvisible(rightArrowRect))
			{
				currentImageIndex = nextImageIndex;
				lastImageChangeTime = Time.realtimeSinceStartup;
			}
		}
		else if (Widgets.ButtonText(rightArrowRect, "▶"))
		{
			currentImageIndex = nextImageIndex;
			lastImageChangeTime = Time.realtimeSinceStartup;
		}
		GUI.color = oldColor;
		float dotSize = 8f;
		float spacing = 4f;
		float totalWidth = (float)carouselImages.Count * dotSize + (float)(carouselImages.Count - 1) * spacing;
		float startX = rect.x + (rect.width - totalWidth) / 2f;
		float y = rect.y + rect.height - dotSize - 8f;
		for (int i = 0; i < carouselImages.Count; i++)
		{
			Rect dotRect = new Rect(startX + (float)i * (dotSize + spacing), y, dotSize, dotSize);
			Color dotColor = ((i == currentImageIndex) ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.7f));
			Widgets.DrawBoxSolid(dotRect, dotColor);
			if (Widgets.ButtonInvisible(dotRect))
			{
				currentImageIndex = i;
				lastImageChangeTime = Time.realtimeSinceStartup;
			}
		}
	}

	private void UpdateCarousel()
	{
		float currentTime = Time.realtimeSinceStartup;
		if (currentTime - lastImageChangeTime >= 3f && carouselImages.Count > 0)
		{
			currentImageIndex = (currentImageIndex + 1) % carouselImages.Count;
			lastImageChangeTime = currentTime;
		}
	}

	private IEnumerable<Widgets.DropdownMenuElement<StoryMode>> GenerateStoryModeDropDownContent(StoryMode target)
	{
		foreach (StoryMode difficulty in Enum.GetValues(typeof(StoryMode)).Cast<StoryMode>())
		{
			yield return new Widgets.DropdownMenuElement<StoryMode>
			{
				option = new FloatMenuOption(difficulty.ToString(), delegate
				{
					ModSettingsAbout.storyMode = difficulty;
				}),
				payload = difficulty
			};
		}
	}
}
