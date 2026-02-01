using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogColorPicker : Window
{
	private Regex regexColor;

	private Regex regexHex;

	private Vector2 scrollPos;

	private Color selectedColor;

	private Color oldSelected;

	private Apparel selectedApparel;

	private ThingWithComps selectedWeapon;

	private ThingDef selectedDef;

	private GeneDef selectedGeneDef;

	private ColorType colorType;

	private bool isColor1Choosen;

	private bool bInstantClose;

	private bool doOnce;

	private float fRed;

	private float fBlue;

	private float fGreen;

	private float fAlpha;

	private float fMaxBright;

	private float fMinBright;

	private int iRed;

	private int iGreen;

	private int iBlue;

	private int iAlpha;

	private string sRGB;

	private string oldsRGB;

	private string sHex;

	private string oldHex;

	private bool isPrimaryColor;

	private double part;

	private Pawn tempPawn;

	private const int wLeft = 252;

	private const int wRight = 240;

	private int Xwidth;

	private Color closestGeneColr = Color.white;

	private GeneDef closestGeneDef = null;

	public override Vector2 InitialSize => new Vector2(288f, WindowTool.MaxH);

	private int GetWindowWidth()
	{
		return (colorType == ColorType.SkinColor || colorType == ColorType.FavColor || colorType == ColorType.EyeColor || colorType == ColorType.GeneColorHair || colorType == ColorType.GeneColorSkinBase || colorType == ColorType.GeneColorSkinOverride) ? 288 : 528;
	}

	internal DialogColorPicker(ColorType _colorType, bool _primaryColor = true, Apparel a = null, ThingWithComps w = null, GeneDef g = null)
	{
		doCloseX = true;
		absorbInputAroundWindow = false;
		draggable = true;
		layer = CEditor.Layer;
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.ColorPicker);
		isPrimaryColor = _primaryColor;
		isColor1Choosen = isPrimaryColor;
		regexColor = new Regex("^[0-9,]*");
		regexHex = new Regex("^[0-9A-F]*");
		scrollPos = default(Vector2);
		colorType = _colorType;
		Init(a, w, g);
	}

	internal bool Preselect(Apparel a, ThingWithComps w, GeneDef g)
	{
		if (colorType == ColorType.SkinColor)
		{
			if (!tempPawn.HasStoryTracker())
			{
				return false;
			}
			selectedColor = tempPawn.GetSkinColor(isPrimaryColor);
		}
		else if (colorType == ColorType.HairColor)
		{
			if (!tempPawn.HasStoryTracker())
			{
				return false;
			}
			HairTool.ASelectedHairModName(HairTool.selectedHairModName);
			StyleTool.ASelectedBeardModName(StyleTool.selectedBeardModName);
			selectedColor = tempPawn.GetHairColor(isPrimaryColor);
		}
		else if (colorType == ColorType.FavColor)
		{
			if (!tempPawn.HasStoryTracker() || tempPawn.story.favoriteColor == null)
			{
				return false;
			}
			selectedColor = tempPawn.story.favoriteColor.color;
		}
		else if (colorType == ColorType.GeneColorHair)
		{
			if (g == null)
			{
				return false;
			}
			selectedGeneDef = g;
			selectedColor = g.hairColorOverride ?? Color.white;
		}
		else if (colorType == ColorType.GeneColorSkinBase)
		{
			if (g == null)
			{
				return false;
			}
			selectedGeneDef = g;
			selectedColor = g.skinColorBase ?? Color.white;
		}
		else if (colorType == ColorType.GeneColorSkinOverride)
		{
			if (g == null)
			{
				return false;
			}
			selectedGeneDef = g;
			selectedColor = g.skinColorOverride ?? Color.white;
		}
		else if (colorType == ColorType.ApparelColor)
		{
			if (!tempPawn.HasApparelTracker())
			{
				return false;
			}
			selectedApparel = ((a != null) ? a : tempPawn.apparel.WornApparel.FirstOrFallback());
			selectedDef = selectedApparel?.def;
			selectedColor = ((selectedApparel == null) ? Color.white : selectedApparel.DrawColor);
			if (selectedApparel != null && (selectedApparel.def.colorGenerator == null || !selectedApparel.def.HasComp(typeof(CompColorable))))
			{
				MessageTool.ShowActionDialog(Label.INFOD_APPAREL, delegate
				{
					ApparelTool.MakeThingColorable(selectedApparel.def);
				}, Label.INFOT_MAKECOLORABLE);
			}
		}
		else if (colorType == ColorType.WeaponColor)
		{
			if (!tempPawn.HasEquipmentTracker())
			{
				return false;
			}
			selectedWeapon = ((w != null) ? w : tempPawn.equipment.Primary);
			selectedDef = selectedWeapon?.def;
			selectedColor = ((selectedWeapon == null) ? Color.white : selectedWeapon.DrawColor);
		}
		else if (colorType == ColorType.EyeColor)
		{
			if (isPrimaryColor)
			{
				selectedColor = tempPawn.GetEyeColor();
			}
			else
			{
				selectedColor = tempPawn.GetEyeColor2();
			}
		}
		else
		{
			selectedColor = Color.white;
		}
		return true;
	}

	private void Init(Apparel a, ThingWithComps w, GeneDef g)
	{
		tempPawn = CEditor.API.Pawn;
		selectedApparel = null;
		selectedDef = null;
		selectedGeneDef = null;
		Xwidth = GetWindowWidth();
		bInstantClose = false;
		doOnce = true;
		if (!Preselect(a, w, g))
		{
			bInstantClose = true;
			return;
		}
		bInstantClose = false;
		fMinBright = 0f;
		fMaxBright = ColorTool.FMAX;
		fRed = selectedColor.r;
		fBlue = selectedColor.b;
		fGreen = selectedColor.g;
		fAlpha = selectedColor.a;
		part = ColorTool.DMAX / ColorTool.DMAXB;
		TextValuesFromSelectedColor();
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (bInstantClose)
		{
			Close();
			return;
		}
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.ColorPicker, ref windowRect, ref doOnce, 370);
			windowRect.width = Xwidth;
		}
		if (tempPawn.ThingID != CEditor.API.Pawn.ThingID)
		{
			Init(null, null, null);
		}
		int num = 0;
		DrawRadioButtons(0, num);
		DrawTitle(0, num, 252, 30);
		num += 40;
		DrawColorTable(0, num, 252, 20);
		num += 280;
		DrawDerivedColors(0, num, 252, 18);
		num += 37;
		DrawColorSlider(0, num, 252, 230);
		num += 190;
		DrawTextFields(0, num, 252, 24);
		num += 72;
		DrawGeneColors(0, num, 252, 30);
		DrawLists(268, 0, 240, (int)InitialSize.y - 110);
		WindowTool.SimpleCloseButton(this);
		WindowTool.TopLayerForWindowOf<Dialog_MessageBox>(force: true);
	}

	private void DrawTitle(int x, int y, int w, int h)
	{
		Text.Font = GameFont.Medium;
		if (colorType != ColorType.FavColor && colorType != ColorType.GeneColorHair && colorType != ColorType.GeneColorSkinBase && colorType != ColorType.GeneColorSkinOverride && tempPawn.HasStoryTracker() && tempPawn.story.favoriteColor != null)
		{
			SZWidgets.ButtonTextureTextHighlight2(new Rect(w - 30, y, 30f, 30f), "", "bfavcolor", tempPawn.story.favoriteColor.color, AFromFavColor, CompatibilityTool.GetFavoriteColorTooltip(tempPawn));
		}
	}

	private void AFromFavColor()
	{
		AColorSelected(tempPawn.story.favoriteColor.color);
	}

	private void DrawColorTable(int x, int y, int w, int h)
	{
		Widgets.DrawBoxSolid(new Rect(x, y, w, h), selectedColor);
		y += h;
		int num = 0;
		int num2 = 63;
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 13; j++)
			{
				SZWidgets.ButtonImageCol(x, y, num2, h, "bwhite", AColorSelected, ColorTool.ListOfColors.ElementAtOrDefault(num));
				y += h;
				num++;
			}
			x += num2;
			y -= 13 * h;
		}
	}

	private void DrawDerivedColors(int x, int y, int w, int h)
	{
		float num = 0.15f;
		for (int i = 0; i < 14; i++)
		{
			SZWidgets.ButtonImageCol(x, y, h, h, "bwhite", AColorSelected, ColorTool.GetDerivedColor(selectedColor, num));
			x += h;
			num -= 0.03f;
		}
	}

	private void DrawColorSlider(int x, int y, int w, int h)
	{
		Listing_X listing_X = new Listing_X();
		listing_X.Begin(new Rect(x, y, w, h));
		fRed = listing_X.Slider(selectedColor.r, 0f, ColorTool.IMAX, Color.red);
		fGreen = listing_X.Slider(selectedColor.g, 0f, ColorTool.IMAX, Color.green);
		fBlue = listing_X.Slider(selectedColor.b, 0f, ColorTool.IMAX, Color.blue);
		fAlpha = listing_X.Slider(selectedColor.a, 0f, ColorTool.IMAX, Color.white);
		selectedColor = new Color(fRed, fGreen, fBlue, fAlpha);
		listing_X.Gap(2f);
		GUI.color = Color.gray;
		listing_X.Label(Label.MIN_RANDOM_BRIGHTNESS);
		listing_X.CurY -= 5f;
		fMinBright = listing_X.Slider(fMinBright, 0f, ColorTool.FMAX);
		listing_X.CurY -= 5f;
		listing_X.Label(Label.MAX_RANDOM_BRIGHTNESS);
		listing_X.CurY -= 5f;
		if (fMaxBright < fMinBright)
		{
			fMaxBright = fMinBright;
		}
		fMaxBright = listing_X.Slider(fMaxBright, 0f, ColorTool.FMAX);
		if (ColorTool.offsetCX != 1f - fMaxBright)
		{
			ColorTool.offsetCX = 1f - fMaxBright;
			ColorTool.lcolors = null;
		}
		listing_X.End();
	}

	private void DrawTextFields(int x, int y, int w, int h)
	{
		if (oldSelected != selectedColor)
		{
			TextValuesFromSelectedColor();
		}
		sRGB = Widgets.TextField(new Rect(x, y, w - 40, h), sRGB, 15, regexColor);
		SZWidgets.ButtonImage(x + w - 30, y, 25f, 25f, "brandom", ARandomColor, Label.TIP_DLG_RANDOM_COLOR);
		y += 37;
		GUI.color = Color.gray;
		sHex = Widgets.TextField(new Rect(x, y, w - 40, h), sHex, 15, regexHex);
		if (colorType == ColorType.HairColor)
		{
			SZWidgets.ButtonImage(x + w - 30, y, 25f, 25f, "brandom", ARandomizeHairAndColor, Label.TIP_DLG_RANDOMIZE_HAIRANDCOLOR);
		}
		if (!string.IsNullOrEmpty(sRGB) && oldsRGB != sRGB)
		{
			RGBTextToSelectedColor();
		}
		if (!string.IsNullOrEmpty(sHex) && oldHex != sHex)
		{
			HEXTextToSelectedColor();
		}
	}

	private void DrawGeneColors(int x, int y, int w, int h)
	{
		if (!ModsConfig.BiotechActive || !tempPawn.HasGeneTracker())
		{
			return;
		}
		List<Gene> list = ((colorType == ColorType.HairColor) ? tempPawn.GetHairGenes() : tempPawn.GetSkinGenes());
		int count = list.Count;
		for (int i = 0; i < 8; i++)
		{
			if (count > i)
			{
				Gene g = list[i];
				bool flag = tempPawn.genes.Xenogenes.Contains(g);
				Color col = g.def.IconColor;
				Rect rect = new Rect(x, y, h, h);
				SZWidgets.Image(rect, flag ? "UI/Icons/Genes/GeneBackground_Xenogene" : "UI/Icons/Genes/GeneBackground_Endogene");
				SZWidgets.ButtonHighlight(rect, g.def.iconPath, delegate
				{
					AColorSelectedByGene(col, g);
				}, col, "select color from gene\n[CTRL]remove gene");
			}
			x += h;
		}
		if (closestGeneDef == null || (colorType != ColorType.HairColor && colorType != ColorType.SkinColor))
		{
			return;
		}
		List<Gene> list2 = list.Where((Gene td) => td.def == closestGeneDef).ToList();
		if (list2.NullOrEmpty())
		{
			Rect rect2 = new Rect(0f, y + h + 5, h, h);
			SZWidgets.Image(rect2, "UI/Icons/Genes/GeneBackground_Endogene");
			SZWidgets.ButtonHighlight(rect2, closestGeneDef.iconPath, delegate
			{
				AGeneSelectedByColor(closestGeneColr, closestGeneDef);
			}, closestGeneColr, "add as new gene");
		}
	}

	private void CalcClosestGeneColor()
	{
		if (ModsConfig.BiotechActive && tempPawn.HasGeneTracker())
		{
			closestGeneDef = GeneTool.ClosestColorGene(selectedColor, colorType == ColorType.HairColor);
			closestGeneColr = closestGeneDef.IconColor;
		}
	}

	private void AGeneSelectedByColor(Color color, GeneDef geneDef)
	{
		selectedColor = color;
		if (colorType == ColorType.HairColor || colorType == ColorType.SkinColor)
		{
			tempPawn.AddGeneAsFirst(geneDef, xeno: false);
		}
	}

	private void AColorSelected(Color color)
	{
		selectedColor = color;
	}

	private void DrawRadioButtons(int x, int y)
	{
		if (Widgets.RadioButtonLabeled(new Rect(x, y, 90f, 30f), Label.COLORA, isColor1Choosen))
		{
			isColor1Choosen = true;
			if (colorType == ColorType.HairColor)
			{
				selectedColor = tempPawn.story.HairColor;
			}
			else if (colorType == ColorType.SkinColor)
			{
				selectedColor = tempPawn.GetSkinColor(primary: true);
			}
		}
		x += 100;
		if ((colorType == ColorType.SkinColor || (CEditor.IsGradientHairActive && colorType == ColorType.HairColor)) && Widgets.RadioButtonLabeled(new Rect(x, y, 90f, 30f), Label.COLORB, !isColor1Choosen))
		{
			isColor1Choosen = false;
			if (colorType == ColorType.HairColor)
			{
				selectedColor = tempPawn.GetHairColor(primary: false);
			}
			else if (colorType == ColorType.SkinColor)
			{
				selectedColor = tempPawn.GetSkinColor(primary: false);
			}
		}
	}

	private void DrawLists(int x, int y, int w, int h)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(x, y, w, h);
		if (colorType == ColorType.HairColor)
		{
			SZWidgets.ButtonTextureTextHighlight2(new Rect(x + 5, y, w - 26, 28f), HairTool.onMouseover ? Label.SETONMOUSEOVER : Label.SETONCLICK, null, Color.white, delegate
			{
				HairTool.onMouseover = !HairTool.onMouseover;
			}, Label.TOGGLESELECTIONONMOUSEOVER);
			y += 30;
			DrawHairSelector(x - 16, ref y, w + 11, h);
			DrawBeardSelector(x - 16, ref y, w + 11, h);
		}
		else if (colorType == ColorType.ApparelColor)
		{
			List<ThingDef> l = tempPawn.apparel.WornApparel.Select((Apparel td) => td.def).ToList();
			SZWidgets.ListView(rect, l, (ThingDef apparel) => apparel.label, (ThingDef apparel) => apparel.DescriptionDetailed, (ThingDef appA, ThingDef appB) => appA == appB, ref selectedDef, ref scrollPos, withRemove: false, AApparelSelected);
		}
		else if (colorType == ColorType.WeaponColor)
		{
			List<ThingDef> l2 = tempPawn.equipment.AllEquipmentListForReading.Select((ThingWithComps td) => td.def).ToList();
			SZWidgets.ListView(rect, l2, (ThingDef weapon) => weapon.label, (ThingDef weapon) => weapon.DescriptionDetailed, (ThingDef wA, ThingDef wB) => wA == wB, ref selectedDef, ref scrollPos, withRemove: false, AWeaponSelected);
		}
	}

	private void DrawHairSelector(int x, ref int y, int w, int h)
	{
		if (!tempPawn.HasStoryTracker())
		{
			return;
		}
		Rect rect = new Rect(x, y, w, 24f);
		SZWidgets.NavSelectorImageBox(rect, HairTool.AChooseHairCustom, HairTool.ARandomHair, null, null, null, HairTool.AConfigHair, Label.FRISUR + " - " + CEditor.API.Pawn.GetHairName(), null, Label.TIP_RANDOM_HAIR, null, null, default(Color), Label.CLICKTOOPENLIST);
		y += 30;
		if (HairTool.isHairConfigOpen)
		{
			int num = (int)InitialSize.y - 83 - y;
			SZWidgets.FloatMenuOnButtonText(new Rect(x + 16, y, w - 32, 25f), HairTool.selectedHairModName ?? Label.ALL, CEditor.API.Get<HashSet<string>>(EType.ModsHairDef), (string s) => s ?? Label.ALL, HairTool.ASelectedHairModName);
			SZWidgets.ListView(new Rect(x + 16, y + 25, w - 32, num), HairTool.lOfHairDefs, (HairDef hd) => hd.LabelCap, (HairDef hd) => hd.description, (HairDef hairA, HairDef hairB) => hairA == hairB, ref tempPawn.story.hairDef, ref scrollPos, withRemove: false, AHairSelected, withSearch: true, drawSection: false, isHead: false, HairTool.onMouseover);
			y += num + 27;
		}
	}

	private void DrawBeardSelector(int x, ref int y, int w, int h)
	{
		if (!tempPawn.HasStyleTracker())
		{
			return;
		}
		Rect rect = new Rect(x, y, w, 24f);
		SZWidgets.NavSelectorImageBox(rect, StyleTool.AChooseBeardCustom, StyleTool.ARandomBeard, null, null, null, StyleTool.AConfigBeard, Label.BEARD + " - " + CEditor.API.Pawn.GetBeardName(), null, Label.TIP_RANDOM_BEARD, null, null, default(Color), Label.CLICKTOOPENLIST);
		y += 30;
		if (StyleTool.isBeardConfigOpen)
		{
			int num = (int)InitialSize.y - 80 - y;
			SZWidgets.FloatMenuOnButtonText(new Rect(x + 16, y, w - 32, 25f), StyleTool.selectedBeardModName ?? Label.ALL, CEditor.API.Get<HashSet<string>>(EType.ModsBeardDef), (string s) => s ?? Label.ALL, StyleTool.ASelectedBeardModName);
			SZWidgets.ListView(new Rect(x + 16, y + 25, w - 32, num), StyleTool.lOfBeardDefs, (BeardDef hd) => hd.LabelCap, (BeardDef hd) => hd.description, (BeardDef beardA, BeardDef beardB) => beardA == beardB, ref tempPawn.style.beardDef, ref scrollPos, withRemove: false, ABeardSelected, withSearch: true, drawSection: false, isHead: false, HairTool.onMouseover);
			y += num + 27;
		}
	}

	private void TextValuesFromSelectedColor()
	{
		double num = (double)selectedColor.r / part;
		double num2 = (double)selectedColor.g / part;
		double num3 = (double)selectedColor.b / part;
		double num4 = (double)selectedColor.a / part;
		iRed = (int)num;
		iGreen = (int)num2;
		iBlue = (int)num3;
		iAlpha = (int)num4;
		sRGB = iRed + "," + iGreen + "," + iBlue + "," + iAlpha;
		sHex = iRed.ToString("X2") + "," + iGreen.ToString("X2") + "," + iBlue.ToString("X2") + "," + iAlpha.ToString("X2");
		oldsRGB = sRGB;
		oldHex = sHex;
		oldSelected = selectedColor;
		if (tempPawn.ThingID != CEditor.API.Pawn.ThingID)
		{
			Init(null, null, null);
			return;
		}
		if (colorType == ColorType.HairColor)
		{
			tempPawn.SetHairColor(isColor1Choosen, selectedColor);
		}
		else if (colorType == ColorType.ApparelColor)
		{
			if (selectedApparel != null)
			{
				selectedApparel.DrawColor = selectedColor;
			}
		}
		else if (colorType == ColorType.WeaponColor)
		{
			if (selectedWeapon != null)
			{
				selectedWeapon.DrawColor = selectedColor;
			}
		}
		else if (colorType == ColorType.SkinColor)
		{
			tempPawn.SetSkinColor(isColor1Choosen, selectedColor);
		}
		else if (colorType == ColorType.FavColor)
		{
			tempPawn.story.favoriteColor.color = selectedColor;
		}
		else if (colorType == ColorType.GeneColorHair)
		{
			if (selectedGeneDef != null)
			{
				selectedGeneDef.hairColorOverride = selectedColor;
			}
		}
		else if (colorType == ColorType.GeneColorSkinBase)
		{
			if (selectedGeneDef != null)
			{
				selectedGeneDef.skinColorBase = selectedColor;
			}
		}
		else if (colorType == ColorType.GeneColorSkinOverride)
		{
			if (selectedGeneDef != null)
			{
				selectedGeneDef.skinColorOverride = selectedColor;
			}
		}
		else if (colorType == ColorType.EyeColor)
		{
			if (isPrimaryColor)
			{
				tempPawn.SetEyeColor(selectedColor);
			}
			else
			{
				tempPawn.SetEyeColor2(selectedColor);
			}
		}
		CalcClosestGeneColor();
		CEditor.API.UpdateGraphics();
	}

	private void RGBTextToSelectedColor()
	{
		oldsRGB = sRGB;
		string[] array = sRGB.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length > 3)
		{
			string s = array[0];
			string s2 = array[1];
			string s3 = array[2];
			string s4 = array[3];
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			if (int.TryParse(s, out iRed))
			{
				flag = true;
			}
			if (int.TryParse(s2, out iGreen))
			{
				flag2 = true;
			}
			if (int.TryParse(s3, out iBlue))
			{
				flag3 = true;
			}
			if (int.TryParse(s4, out iAlpha))
			{
				flag4 = true;
			}
			if (iRed > ColorTool.IMAXB)
			{
				iRed = ColorTool.IMAXB;
			}
			if (iGreen > ColorTool.IMAXB)
			{
				iGreen = ColorTool.IMAXB;
			}
			if (iBlue > ColorTool.IMAXB)
			{
				iBlue = ColorTool.IMAXB;
			}
			if (iAlpha > ColorTool.IMAXB)
			{
				iAlpha = ColorTool.IMAXB;
			}
			if (flag && flag2 && flag3 && flag4)
			{
				sHex = iRed.ToString("X2") + "," + iGreen.ToString("X2") + "," + iBlue.ToString("X2") + "," + iAlpha.ToString("X2");
				oldHex = sHex;
				selectedColor = new Color((float)(part * (double)iRed), (float)(part * (double)iGreen), (float)(part * (double)iBlue), (float)(part * (double)iAlpha));
			}
		}
	}

	private void HEXTextToSelectedColor()
	{
		oldHex = sHex;
		string value = sHex.SubstringTo(",");
		string value2 = sHex.SubstringTo(",", 2).SubstringFrom(",");
		string value3 = sHex.SubstringTo(",", 3).SubstringFrom(",", 2);
		string value4 = sHex.SubstringFrom(",", 3);
		bool flag = true;
		bool flag2 = true;
		bool flag3 = true;
		bool flag4 = true;
		try
		{
			iRed = Convert.ToByte(value, 16);
		}
		catch
		{
			flag = false;
		}
		try
		{
			iGreen = Convert.ToByte(value2, 16);
		}
		catch
		{
			flag2 = false;
		}
		try
		{
			iBlue = Convert.ToByte(value3, 16);
		}
		catch
		{
			flag3 = false;
		}
		try
		{
			iAlpha = Convert.ToByte(value4, 16);
		}
		catch
		{
			flag4 = false;
		}
		if (flag && flag2 && flag3 && flag4)
		{
			sRGB = iRed + "," + iGreen + "," + iBlue + "," + iAlpha;
			oldsRGB = sRGB;
			selectedColor = new Color((float)(part * (double)iRed), (float)(part * (double)iGreen), (float)(part * (double)iBlue), (float)(part * (double)iAlpha));
		}
	}

	private void AColorSelectedByGene(Color color, Gene g)
	{
		if (colorType == ColorType.HairColor || colorType == ColorType.SkinColor)
		{
			if (Event.current.control)
			{
				Gene gene = tempPawn.RemoveGeneKeepFirst(g);
				selectedColor = gene.def.IconColor;
				TextValuesFromSelectedColor();
			}
			else
			{
				selectedColor = color;
				tempPawn.MakeGeneFirst(g);
			}
		}
		else
		{
			selectedColor = color;
		}
	}

	private void ARandomColor()
	{
		if (Event.current.control)
		{
			selectedColor = ColorTool.RandomAlphaColor;
		}
		else
		{
			selectedColor = ColorTool.GetRandomColor(fMinBright, fMaxBright);
		}
	}

	private void ARandomizeHairAndColor()
	{
		if (tempPawn != null && tempPawn.story != null)
		{
			tempPawn.SetHair(next: true, random: true, HairTool.selectedHairModName);
			Color color = (Event.current.control ? ColorTool.RandomAlphaColor : ColorTool.GetRandomColor(fMinBright, fMaxBright));
			tempPawn.SetHairColor(primary: true, color);
			Color color2 = (Event.current.control ? ColorTool.RandomAlphaColor : ColorTool.GetRandomColor(fMinBright, fMaxBright));
			tempPawn.SetHairColor(primary: false, color2);
			selectedColor = (isColor1Choosen ? color : color2);
			CEditor.API.UpdateGraphics();
		}
	}

	private void AHairSelected(HairDef hairDef)
	{
		tempPawn.SetHair(hairDef);
		CEditor.API.UpdateGraphics();
	}

	private void ABeardSelected(BeardDef beardDef)
	{
		tempPawn.SetBeard(beardDef);
		CEditor.API.UpdateGraphics();
	}

	private void AApparelSelected(ThingDef apparelDef)
	{
		selectedApparel = tempPawn.apparel.WornApparel.Where((Apparel td) => td.def == apparelDef).First();
		selectedColor = selectedApparel.DrawColor;
		if (selectedApparel != null && (selectedApparel.def.colorGenerator == null || !selectedApparel.def.HasComp(typeof(CompColorable))))
		{
			MessageTool.ShowActionDialog(Label.INFOD_APPAREL, delegate
			{
				ApparelTool.MakeThingColorable(selectedApparel.def);
			}, Label.INFOT_MAKECOLORABLE);
		}
		CEditor.API.UpdateGraphics();
	}

	private void AWeaponSelected(ThingDef weaponDef)
	{
		selectedWeapon = tempPawn.equipment.AllEquipmentListForReading.Where((ThingWithComps td) => td.def == weaponDef).First();
		selectedColor = selectedWeapon.DrawColor;
		CEditor.API.UpdateGraphics();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.ColorPicker, windowRect.position);
		base.Close(doCloseSound);
	}
}
