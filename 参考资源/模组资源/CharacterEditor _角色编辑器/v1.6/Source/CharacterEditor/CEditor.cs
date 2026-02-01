using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CharacterEditor;

internal sealed class CEditor
{
	private sealed class ModOptions : ModSettings
	{
		private class BData
		{
			public string Title;

			public string Descr;

			public bool Default;

			public bool Value;

			public BData(string title, string desc, bool val, bool defVal)
			{
				Title = title;
				Descr = desc;
				Value = val;
				Default = defVal;
			}
		}

		private class IData
		{
			public string Title;

			public string Descr;

			public int Default;

			public int Value;

			public IData(string title, string desc, int val, int defVal)
			{
				Title = title;
				Descr = desc;
				Value = val;
				Default = defVal;
			}
		}

		private class SData
		{
			public string Title;

			public string Descr;

			public string Default;

			public string Value;

			public SData(string title, string desc, string val, string defVal)
			{
				Title = title;
				Descr = desc;
				Value = val;
				Default = defVal;
			}
		}

		private class DialogConfigurate : Window
		{
			private int buttonWidth = 90;

			private bool doOnce;

			private int frameH;

			private int framwW;

			private Vector2 scrollPos1 = default(Vector2);

			private Vector2 scrollPos2 = default(Vector2);

			private int x;

			private int y;

			private ModOptions mo;

			public override Vector2 InitialSize => new Vector2(800f, WindowTool.MaxH);

			private Rect RectAccept => new Rect(InitialSize.x - 140f, frameH, 100f, 28f);

			private Rect RectBottom => new Rect(x, frameH, buttonWidth, 28f);

			private Rect RectConfig => new Rect(x + framwW - 42, y, 24f, 24f);

			private Rect RectFrame => new Rect(x, y, framwW, frameH - y - 15);

			private Rect RectFullWidth => new Rect(x, y, InitialSize.x - 32f, 28f);

			private Rect RectHalfWidth => new Rect(x, y, framwW, 24f);

			private Rect RectHotkey => new Rect(x + 130, y, framwW - 155, 24f);

			private Rect RectHotkeyLabel => new Rect(x, y, 130f, 24f);

			private Rect RectImage => new Rect(x, y, 64f, 64f);

			private Rect RectNumeric => new Rect(x, y, 130f, 24f);

			private Rect RectRemoveHotkey => new Rect(x + framwW - 24, y, 24f, 24f);

			private Rect RectSlider => new Rect(x + 100, y, framwW - 100, 24f);

			private Rect RectSliderLabel => new Rect(x, y + 2, 130f, 24f);

			private Rect RectSolid => new Rect(x + 25, y, framwW, 24f);

			internal DialogConfigurate()
			{
				doOnce = true;
				framwW = (int)InitialSize.x - 410;
				frameH = (int)InitialSize.y - 70;
				mo = API.Get<ModOptions>(EType.Settings);
				doCloseX = true;
				absorbInputAroundWindow = false;
				draggable = true;
				layer = Layer;
			}

			public override void DoWindowContents(Rect inRect)
			{
				x = 0;
				y = 0;
				DrawTitle();
				DrawHotkey();
				DrawHotkeyTeleport();
				y += 3;
				DrawNumeric();
				DrawStrings();
				DrawBoolean(390);
				DrawButtons();
			}

			private void DoOnce()
			{
				if (doOnce)
				{
					doOnce = false;
					Vector2 mousePositionOnUI = UI.MousePositionOnUI;
					float num = API.EditorPosY;
					windowRect.position = new Vector2(mousePositionOnUI.x + 250f, num);
				}
			}

			private void DoAndClose()
			{
				mo.SaveSettings();
				mo.SaveSlots();
				Close();
			}

			private void AResetAll()
			{
				foreach (OptionS key in mo.dicString.Keys)
				{
					mo.dicString[key].Value = mo.dicString[key].Default;
				}
				foreach (OptionB key2 in mo.dicBool.Keys)
				{
					mo.dicBool[key2].Value = mo.dicBool[key2].Default;
				}
				foreach (OptionI key3 in mo.dicInt.Keys)
				{
					mo.dicInt[key3].Value = mo.dicInt[key3].Default;
				}
			}

			private void AConfirmDelete()
			{
				MessageTool.ShowActionDialog(Label.ALLSLOTSWILLBECLEARED, ADeleteSlots);
			}

			private void ADeleteCustoms()
			{
				PresetPawn.ClearAllCustoms();
			}

			private void ADeleteSlots()
			{
				PresetPawn.ClearAllSlots();
			}

			private void AExportSlots()
			{
				string text = "";
				foreach (int key in mo.dicSlots.Keys)
				{
					text = text + mo.dicSlots[key] + "\n";
				}
				if (FileIO.WriteFile(FileIO.PATH_PAWNEX, text.AsBytes(Encoding.UTF8)))
				{
					MessageTool.Show("export successful to " + FileIO.PATH_PAWNEX);
				}
			}

			private void AImportSlots()
			{
				string text = FileIO.ReadFile(FileIO.PATH_PAWNEX).AsString(Encoding.UTF8);
				if (text.NullOrEmpty())
				{
					return;
				}
				string[] array = text.SplitNo("\n");
				int count = mo.dicSlots.Keys.Count;
				for (int i = 0; i < count; i++)
				{
					if (array.Length > i)
					{
						mo.dicSlots[i] = array[i];
					}
				}
				MessageTool.Show("import successful from " + FileIO.PATH_PAWNEX);
			}

			private void AMinusInt(int index)
			{
				mo.dicInt[(OptionI)index].Value--;
			}

			private void APlusInt(int index)
			{
				mo.dicInt[(OptionI)index].Value++;
			}

			private void BoolChanged(OptionB b)
			{
				if (b == OptionB.SHOWINMENU || b == OptionB.ZOMBOBJECTS || b == OptionB.SHOWBODYSIZEGENES || b == OptionB.SHOWPAWNLIST || b == OptionB.SHOWTABS || b == OptionB.PAUSEGAME || b == OptionB.SHOWMINI)
				{
					API.OnSettingsChanged();
				}
				else if (b == OptionB.HDRARGB)
				{
					API.OnSettingsChanged(updateRender: true);
				}
			}

			private void DrawBoolean(int xPos)
			{
				Text.Font = GameFont.Small;
				float height = mo.dicBool.Keys.Count * 27;
				Rect outRect = new Rect(390f, 30f, 380f, frameH - 30);
				Rect rect = new Rect(0f, 0f, outRect.width - 16f, height);
				Widgets.BeginScrollView(outRect, ref scrollPos1, rect);
				Rect rect2 = rect.ContractedBy(4f);
				rect2.y -= 4f;
				rect2.height = height;
				Listing_X listing_X = new Listing_X();
				listing_X.Begin(rect2);
				listing_X.verticalSpacing = 5f;
				listing_X.DefSelectionLineHeight = 27f;
				foreach (OptionB key in mo.dicBool.Keys)
				{
					bool checkOn = mo.dicBool[key].Value;
					listing_X.CheckboxLabeledWithDefault(mo.dicBool[key].Title, 2f, rect.width - 20f, ref checkOn, mo.dicBool[key].Default, mo.dicBool[key].Descr);
					if (checkOn != mo.dicBool[key].Value)
					{
						mo.dicBool[key].Value = checkOn;
						BoolChanged(key);
					}
				}
				listing_X.End();
				Widgets.EndScrollView();
				y += (int)outRect.height + 10;
			}

			private void DrawButtons()
			{
				Text.Font = GameFont.Small;
				SZWidgets.ButtonText(RectBottom, Label.TODEFAULTS, AResetAll, Label.TIP_TODEFAULTS);
				x += buttonWidth;
				SZWidgets.ButtonText(RectBottom, "Delete".Translate(), AConfirmDelete, Label.DELETE_SLOTS);
				x += buttonWidth;
				SZWidgets.ButtonText(RectBottom, Label.EXPORT, AExportSlots, Label.EXPORT_SLOTS);
				x += buttonWidth;
				SZWidgets.ButtonText(RectBottom, Label.IMPORT, AImportSlots, Label.IMPORT_SLOTS);
				x += buttonWidth;
				WindowTool.SimpleSaveButton(this, DoAndClose);
			}

			private void DrawNumeric()
			{
				int value = mo.dicInt[OptionI.RESOLUTION].Value;
				foreach (OptionI key in mo.dicInt.Keys)
				{
					if (key == OptionI.VERSION)
					{
						if (Prefs.DevMode)
						{
							SZWidgets.Label(RectSliderLabel, mo.dicInt[key].Title + " " + mo.dicInt[key].Value, null, mo.dicInt[key].Descr);
						}
					}
					else
					{
						if (key == OptionI.STACKLIMIT)
						{
							continue;
						}
						SZWidgets.Label(RectSliderLabel, mo.dicInt[key].Title, null, mo.dicInt[key].Descr);
						SZWidgets.ButtonTextVar((float)x + RectNumeric.width, y, 28f, 24f, "+", APlusInt, (int)key);
						int max = ((key == OptionI.RESOLUTION) ? 1600 : 400);
						mo.dicInt[key].Value = SZWidgets.NumericTextField((float)x + RectNumeric.width + 10f, y, (float)framwW - RectNumeric.width - 60f, 24f, mo.dicInt[key].Value, 1, max);
						SZWidgets.ButtonTextVar(x + framwW - 30, y, 28f, 24f, "-", AMinusInt, (int)key);
					}
					y += 28;
				}
				if (value != mo.dicInt[OptionI.RESOLUTION].Value)
				{
					API.OnSettingsChanged(updateRender: true);
				}
				y += 8;
			}

			private void DrawStrings()
			{
				Text.Font = GameFont.Small;
				float height = (mo.dicString.Keys.Count + mo.dicSlots.Keys.Count - 2) * 27;
				Rect rectFrame = RectFrame;
				Rect rect = new Rect(0f, 0f, rectFrame.width - 16f, height);
				Widgets.BeginScrollView(rectFrame, ref scrollPos2, rect);
				Rect rect2 = rect.ContractedBy(4f);
				rect2.y -= 4f;
				rect2.height = height;
				Listing_X listing_X = new Listing_X();
				listing_X.Begin(rect2);
				listing_X.verticalSpacing = 5f;
				listing_X.DefSelectionLineHeight = 27f;
				foreach (OptionS key in mo.dicString.Keys)
				{
					if (key != OptionS.HOTKEYEDITOR && key != OptionS.HOTKEYTELEPORT)
					{
						string value = mo.dicString[key].Value;
						value = listing_X.TextEntryLabeledWithDefaultAndCopy(mo.dicString[key].Title, value, mo.dicString[key].Default);
						if (value != mo.dicString[key].Value)
						{
							mo.dicString[key].Value = value;
						}
					}
				}
				foreach (int key2 in mo.dicSlots.Keys)
				{
					string text = mo.dicSlots[key2];
					text = listing_X.TextEntryLabeledWithDefaultAndCopy(string.Format(Label.O_PAWNSLOT, key2.ToString()), text, "");
					if (text != mo.dicSlots[key2])
					{
						mo.SetSlot(key2, text, andSave: false);
					}
				}
				listing_X.End();
				Widgets.EndScrollView();
				y += (int)rectFrame.height + 30;
			}

			private void DrawTitle()
			{
				Text.Font = GameFont.Medium;
				Widgets.Label(RectFullWidth, Reflect.APP_ATTRIBUTE_TITLE + Label.OPTIONS);
				Text.Font = GameFont.Tiny;
				Widgets.Label(new Rect(InitialSize.x - 160f, 0f, 150f, 22f), Reflect.APP_VERISON_AND_DATE);
				y += 32;
				Text.Font = GameFont.Small;
			}

			private void RemoveHotkey(string kbdName, OptionS s)
			{
				KeyBindingDef keyDef = DefTool.KeyBindingDef(kbdName);
				KeyPrefs.KeyPrefsData.SetBinding(keyDef, KeyPrefs.BindingSlot.A, KeyCode.None);
				if (mo.dicString.ContainsKey(s))
				{
					mo.dicString[s].Value = KeyCode.None.ToStringReadable();
				}
				KeyPrefs.Save();
			}

			private void ChangeHotkey(string kbdName)
			{
				Text.Font = GameFont.Medium;
				KeyBindingDef keyDef = DefTool.KeyBindingDef(kbdName);
				WindowTool.Open(new Dialog_DefineBinding(KeyPrefs.KeyPrefsData, keyDef, KeyPrefs.BindingSlot.A));
				KeyPrefs.Save();
			}

			private void DrawHotkey(string kbdName, OptionS s, string title, string descr)
			{
				y += 5;
				Enum.TryParse<KeyCode>(kbdName, out var result);
				KeyBindingDef keyBindingDef = DefTool.KeyBindingDef(kbdName);
				if (keyBindingDef == null)
				{
					keyBindingDef = DefTool.TryCreateKeyBinding(kbdName, result.ToStringReadable(), title, descr);
				}
				KeyCode k = ((keyBindingDef != null) ? KeyPrefs.KeyPrefsData.GetBoundKeyCode(keyBindingDef, KeyPrefs.BindingSlot.A) : KeyCode.None);
				SZWidgets.Label(RectHotkeyLabel, title, null, descr);
				SZWidgets.ButtonText(RectHotkey, (keyBindingDef == null) ? Label.NONE : k.ToStringReadable(), delegate
				{
					ChangeHotkey(kbdName);
				}, descr);
				SZWidgets.ButtonImage(RectRemoveHotkey, "UI/Buttons/Delete", delegate
				{
					RemoveHotkey(kbdName, s);
				});
				if (keyBindingDef != null && mo.dicString.ContainsKey(s) && k.ToStringReadable() != mo.dicString[s].Value)
				{
					mo.dicString[s].Value = k.ToStringReadable();
				}
				y += 24;
			}

			private void DrawHotkey()
			{
				DrawHotkey("HotkeyEditor", OptionS.HOTKEYEDITOR, Label.O_HOTKEYEDITOR, Label.O_DESC_HOTKEYEDITOR);
			}

			private void DrawHotkeyTeleport()
			{
				DrawHotkey("HotkeyTeleport", OptionS.HOTKEYTELEPORT, Label.O_HOTKEYTELEPORT, Label.O_DESC_HOTKEYTELEPORT);
			}
		}

		private Dictionary<int, string> dicSlots;

		private Dictionary<OptionS, SData> dicString;

		private Dictionary<OptionI, IData> dicInt;

		private Dictionary<OptionB, BData> dicBool;

		private bool bDoRescue;

		private string PATH_MODDIR { get; set; }

		private string PATH_SETTINGS => PATH_MODDIR + Path.DirectorySeparatorChar + "options.txt";

		private string PATH_PAWNS => PATH_MODDIR + Path.DirectorySeparatorChar + "pawnslots.txt";

		internal ModOptions()
		{
			ReInit();
		}

		internal void ReInit()
		{
			PATH_MODDIR = GenFilePaths.ConfigFolderPath.Replace("Config", "CharacterEditor");
			FileIO.CheckOrCreateDir(PATH_MODDIR);
			dicSlots = new Dictionary<int, string>();
			dicString = new Dictionary<OptionS, SData>();
			dicInt = new Dictionary<OptionI, IData>();
			dicBool = new Dictionary<OptionB, BData>();
			Label.UpdateLabels();
			Load(PATH_SETTINGS);
			LoadSlots(PATH_PAWNS);
			UpdateNumSlots();
		}

		private void RescueOldPawns()
		{
			string directoryName = Path.GetDirectoryName(GenFilePaths.ConfigFolderPath.SubstringBackwardTo(Path.DirectorySeparatorChar.ToString()) + Path.DirectorySeparatorChar + "HugsLib");
			directoryName = directoryName + Path.DirectorySeparatorChar + "ModSettings.xml";
			if (!FileIO.Exists(directoryName))
			{
				return;
			}
			try
			{
				string text = FileIO.ReadFile(directoryName).AsString(Encoding.UTF8);
				text = text.SubstringFrom("<P_CE_CustomPawn").SubstringTo("</CharacterEditor>");
				if (text.NullOrEmpty())
				{
					return;
				}
				string[] array = text.SplitNo("<P_CE_CustomPawn");
				if (array.Length != 0)
				{
					string[] array2 = array;
					foreach (string text2 in array2)
					{
						string input = text2.SubstringFrom("CustomPawn").SubstringTo(">");
						string val = text2.SubstringFrom(">").SubstringTo("<");
						int id = input.AsInt32();
						SetSlot(id, val, andSave: false);
					}
					Log.Message("old pawn slots imported");
				}
			}
			catch
			{
			}
		}

		private void Load(string filepath)
		{
			string text = "";
			text = ((!FileIO.Exists(filepath)) ? "\n\n\n\n\n\n\n\n\n" : FileIO.ReadFile(filepath).AsString(Encoding.UTF8));
			if (text.NullOrEmpty())
			{
				text = "\n\n\n\n\n\n\n\n\n";
			}
			string[] array = text.SplitNo("\n");
			for (int i = 0; i < array.Length; i++)
			{
				if (i == 0)
				{
					LoadBoolSettings(array[i]);
				}
				if (i == 1)
				{
					LoadIntSettings(array[i]);
				}
				if (i == 2)
				{
					LoadStringSettings(array[i]);
				}
				if (i == 3)
				{
				}
				if (i == 4)
				{
				}
				if (i == 5)
				{
				}
				if (i == 6)
				{
					dicString.Add(OptionS.CUSTOMGENE, new SData(Label.O_SAVEDGENECHANGES, Label.O_DESC_SAVEDGENECHANGES, array[i], ""));
				}
				if (i == 7)
				{
					dicString.Add(OptionS.CUSTOMOBJECT, new SData(Label.O_SAVEDITEMCHANGES, Label.O_DESC_SAVEDITEMCHANGES, array[i], ""));
				}
				if (i == 8)
				{
					dicString.Add(OptionS.CUSTOMLIFESTAGE, new SData(Label.O_SAVEDLSCHANGES, Label.O_DESC_SAVEDLSCHANGES, array[i], ""));
				}
			}
			if (!dicString.ContainsKey(OptionS.CUSTOMGENE))
			{
				dicString.Add(OptionS.CUSTOMGENE, new SData(Label.O_SAVEDGENECHANGES, Label.O_DESC_SAVEDGENECHANGES, "", ""));
			}
			if (!dicString.ContainsKey(OptionS.CUSTOMOBJECT))
			{
				dicString.Add(OptionS.CUSTOMOBJECT, new SData(Label.O_SAVEDITEMCHANGES, Label.O_DESC_SAVEDITEMCHANGES, "", ""));
			}
			if (!dicString.ContainsKey(OptionS.CUSTOMLIFESTAGE))
			{
				dicString.Add(OptionS.CUSTOMLIFESTAGE, new SData(Label.O_SAVEDLSCHANGES, Label.O_DESC_SAVEDLSCHANGES, "", ""));
			}
			if (!FileIO.Exists(filepath))
			{
				SaveSettings();
			}
		}

		private void LoadSlots(string filepath)
		{
			string text = "";
			try
			{
				if (FileIO.Exists(filepath))
				{
					text = FileIO.ReadFile(filepath).AsString(Encoding.UTF8);
				}
			}
			catch
			{
			}
			Log.Message("loading pawn slot content from file...");
			bDoRescue = text.NullOrEmpty();
			if (text == null)
			{
				text = "";
			}
			string[] array = text.SplitNo("\n");
			dicSlots = new Dictionary<int, string>();
			int num = dicInt.GetValue(OptionI.NUMPAWNSLOTS)?.Value ?? 0;
			for (int i = 0; i < num; i++)
			{
				dicSlots.Add(i, (!text.NullOrEmpty() && array.Length >= i + 1) ? array[i] : "");
			}
			if (bDoRescue)
			{
				RescueOldPawns();
			}
			if (!FileIO.Exists(filepath))
			{
				SaveSlots();
			}
		}

		private string PawnSlotsToString()
		{
			string text = "";
			foreach (int key in dicSlots.Keys)
			{
				text = text + dicSlots[key] + "\n";
			}
			return text;
		}

		private void LoadStringSettings(string line)
		{
			string[] array = line.SplitNo("|");
			dicString = new Dictionary<OptionS, SData>();
			dicString.Add(OptionS.HOTKEYEDITOR, new SData(Label.O_HOTKEYEDITOR, Label.O_DESC_HOTKEYEDITOR, (!line.NullOrEmpty() && array.Length >= 1) ? array[0] : "None", "None"));
			dicString.Add(OptionS.HOTKEYTELEPORT, new SData(Label.O_HOTKEYTELEPORT, Label.O_DESC_HOTKEYTELEPORT, (array.Length >= 2) ? array[1] : "None", "None"));
		}

		private string StringSettingsToString()
		{
			string text = "";
			int num = 2;
			foreach (OptionS key in dicString.Keys)
			{
				if (num > 0)
				{
					text = text + dicString[key].Value + "|";
				}
				num--;
			}
			text += "\n";
			text += "\n";
			text += "\n";
			text += "\n";
			text = text + dicString[OptionS.CUSTOMGENE].Value + "\n";
			text = text + dicString[OptionS.CUSTOMOBJECT].Value + "\n";
			return text + dicString[OptionS.CUSTOMLIFESTAGE].Value + "\n";
		}

		private void LoadIntSettings(string line)
		{
			string[] array = line.SplitNo("|");
			dicInt = new Dictionary<OptionI, IData>();
			dicInt.Add(OptionI.RESOLUTION, new IData(Label.O_RESOLUTION, Label.O_DESC_RESOLUTION, (!line.NullOrEmpty() && array.Length >= 1) ? array[0].AsInt32() : 800, 800));
			dicInt.Add(OptionI.STACKLIMIT, new IData(Label.O_STACKLIMIT, Label.O_DESC_STACKLIMIT, (array.Length >= 2) ? array[1].AsInt32() : 100, 100));
			dicInt.Add(OptionI.NUMCAPSULESETS, new IData(Label.O_NUMCAPSULE, Label.O_DESC_NUMCAPSULE, (array.Length >= 3) ? array[2].AsInt32() : 10, 10));
			dicInt.Add(OptionI.NUMPAWNSLOTS, new IData(Label.O_NUMSLOTS, Label.O_DESC_NUMSLOTS, (array.Length >= 4) ? array[3].AsInt32() : 90, 90));
			dicInt.Add(OptionI.VERSION, new IData("Version", "", (array.Length >= 5) ? array[4].AsInt32() : Reflect.VERSION_BUILD, Reflect.VERSION_BUILD));
		}

		private string IntSettingsToString()
		{
			string text = "";
			foreach (OptionI key in dicInt.Keys)
			{
				text = text + dicInt[key].Value + "|";
			}
			return text + "\n";
		}

		private void LoadBoolSettings(string line)
		{
			string[] array = line.SplitNo("|");
			dicBool = new Dictionary<OptionB, BData>();
			dicBool.Add(OptionB.HDRARGB, new BData(Label.O_HDR, Label.O_DESC_HDR, line.NullOrEmpty() || array.Length < 1 || array[0].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.CREATERACESPECIFIC, new BData(Label.O_CREATERACESPECIFIC, Label.O_DESC_CREATERACESPECIFIC, array.Length < 2 || array[1].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.CREATENUDE, new BData(Label.O_CREATENUDE, Label.O_DESC_CREATENUDE, array.Length >= 3 && array[2].AsBoolWithDefault(defVal: false), defVal: false));
			dicBool.Add(OptionB.CREATENOWEAPON, new BData(Label.O_CREATENOWEAPON, Label.O_DESC_CREATENOWEAPON, array.Length >= 4 && array[3].AsBoolWithDefault(defVal: false), defVal: false));
			dicBool.Add(OptionB.CREATENOINV, new BData(Label.O_CREATENOINV, Label.O_DESC_CREATENOINV, array.Length >= 5 && array[4].AsBoolWithDefault(defVal: false), defVal: false));
			dicBool.Add(OptionB.PAUSEGAME, new BData(Label.O_PAUSEGAME, Label.O_DESC_PAUSEGAME, array.Length < 6 || array[5].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.SHOWTABS, new BData(Label.O_EDITTABS, Label.O_DESC_EDITTABS, array.Length < 7 || array[6].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.SHOWPAWNLIST, new BData(Label.O_PAWNLIST, Label.O_DESC_PAWNLIST, array.Length < 8 || array[7].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.SHOWMINI, new BData(Label.O_MINI, Label.O_DESC_MINI, array.Length >= 9 && array[8].AsBoolWithDefault(defVal: false), defVal: false));
			dicBool.Add(OptionB.SHOWINMENU, new BData(Label.O_SHOWINMAINTABS, Label.O_DESC_SHOWINMAINTABS, array.Length < 10 || array[9].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.ZOMBOBJECTS, new BData(Label.O_DISABLEOBJ, Label.O_DESC_DISABLEOBJ, array.Length < 11 || array[10].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.SHOWMAPVARS, new BData(Label.O_MAPSIZE, Label.O_DESC_MAPSIZE, array.Length >= 12 && array[11].AsBoolWithDefault(defVal: false), defVal: false));
			dicBool.Add(OptionB.DOAPPARELCHECK, new BData(Label.O_DOAPPARELCHECK, Label.O_DESC_DOAPPARELCHECK, array.Length < 13 || array[12].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.STAYINCASCET, new BData(Label.O_STAYINCASCET, Label.O_DESC_STAYINCASCET, array.Length >= 14 && array[13].AsBoolWithDefault(defVal: false), defVal: false));
			dicBool.Add(OptionB.SHOWDEADLOGO, new BData(Label.O_SHOWDEADLOGO, Label.O_DESC_SHOWDEADLOGO, array.Length < 15 || array[14].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.USESORTEDPAWNLIST, new BData(Label.O_USESORTEDPAWNLIST, Label.O_DESC_SORTEDPAWNLIST, array.Length < 16 || array[15].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.MOREPAWNNAMES, new BData(Label.O_MOREPAWNNAMES, Label.O_DESC_MOREPAWNNAMES, array.Length < 17 || array[16].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.USECHAOSABILITY, new BData(Label.O_CHAOSABILITY, Label.O_DESC_CHAOSABILITY, array.Length < 18 || array[17].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.SHOWICONINSTARTING, new BData(Label.O_SHOWICONINSTARTING, Label.O_DESC_SHOWICONINSTARTING, array.Length < 19 || array[18].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.SHOWBUTTONINSTARTING, new BData(Label.O_SHOWBUTTONINSTARTING, Label.O_DESC_SHOWBUTTONINSTARTING, array.Length < 20 || array[19].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.SHOWBODYSIZEGENES, new BData(Label.O_SHOWBODYSIZEGENES, Label.O_DESC_SHOWBODYSIZEGENES, array.Length < 21 || array[20].AsBoolWithDefault(defVal: true), defVal: true));
			dicBool.Add(OptionB.USEFIXEDSHADER, new BData(Label.O_USEFIXEDSHADER, Label.O_DESC_USEFIXEDSHADER, array.Length >= 22 && array[21].AsBoolWithDefault(defVal: false), defVal: false));
			dicBool.Add(OptionB.ADDCOMPCOLORABLE, new BData(Label.O_COMPCOLORABLE, Label.O_DESC_COMPCOLORABLE, array.Length >= 23 && array[22].AsBoolWithDefault(defVal: false), defVal: false));
		}

		private string BoolSettingsToString()
		{
			string text = "";
			foreach (OptionB key in dicBool.Keys)
			{
				text = text + (dicBool[key].Value ? "1" : "0") + "|";
			}
			return text + "\n";
		}

		private void UpdateNumSlots()
		{
			try
			{
				int value = dicInt[OptionI.NUMPAWNSLOTS].Value;
				if (dicSlots.Count < value)
				{
					for (int i = 0; i < value; i++)
					{
						if (!dicSlots.ContainsKey(i))
						{
							dicSlots.Add(i, "");
						}
					}
				}
				else
				{
					if (dicSlots.Count <= value)
					{
						return;
					}
					int count = dicSlots.Count;
					for (int num = count; num > 0; num--)
					{
						if (num > value)
						{
							dicSlots.Remove(num);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.StackTrace);
			}
		}

		private void SaveSettings()
		{
			FileIO.CheckOrCreateDir(PATH_MODDIR);
			string text = BoolSettingsToString() + IntSettingsToString() + StringSettingsToString();
			FileIO.WriteFile(PATH_SETTINGS, text.AsBytes(Encoding.UTF8));
			UpdateNumSlots();
		}

		private void SaveSlots()
		{
			FileIO.CheckOrCreateDir(PATH_MODDIR);
			string text = PawnSlotsToString();
			FileIO.WriteFile(PATH_PAWNS, text.AsBytes(Encoding.UTF8));
		}

		internal void CreateDefaultLists()
		{
			try
			{
				Log.Message("CE is trying to create default parameter lists ...");
				API.Get<Dictionary<string, PresetGene>>(EType.GenePreset);
				API.Get<Dictionary<string, PresetObject>>(EType.ObjectPreset);
				API.Get<Dictionary<string, PresetObject>>(EType.TurretPreset);
				Log.Message("...lists created");
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message + "\n" + ex.StackTrace);
			}
		}

		internal void UpdatingCustoms()
		{
			try
			{
				Log.Message("CE is trying to apply modified parameters...");
				IData value = dicInt.GetValue(OptionI.VERSION);
				if (value == null)
				{
					Log.Message("version < 1206 - clearing customs to defaults...");
					dicString[OptionS.CUSTOMGENE].Value = "";
					dicInt[OptionI.VERSION].Value = Reflect.VERSION_BUILD;
					SaveSettings();
				}
				else if (value.Value != Reflect.VERSION_BUILD)
				{
					dicInt[OptionI.VERSION].Value = Reflect.VERSION_BUILD;
					SaveSettings();
				}
				PresetGene.LoadAllModifications(dicString[OptionS.CUSTOMGENE].Value);
				PresetObject.LoadAllModifications(dicString[OptionS.CUSTOMOBJECT].Value);
				PresetLifeStage.LoadAllModifications(dicString[OptionS.CUSTOMLIFESTAGE].Value);
				Log.Message("...done");
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message + "\n" + ex.StackTrace);
			}
		}

		internal void Configurate()
		{
			WindowTool.Open(new DialogConfigurate());
		}

		public override void ExposeData()
		{
		}

		internal bool Get(OptionB option)
		{
			return dicBool[option].Value;
		}

		internal int Get(OptionI option)
		{
			return dicInt[option].Value;
		}

		internal string Get(OptionS option)
		{
			return dicString[option].Value;
		}

		internal string GetSlot(int id)
		{
			return dicSlots.GetValue(id);
		}

		internal void SetSlot(int id, string val, bool andSave)
		{
			if (dicSlots.ContainsKey(id))
			{
				dicSlots[id] = val;
			}
			if (andSave)
			{
				SaveSlots();
			}
		}

		internal void SetCustom(OptionS option, string val, string defName)
		{
			if (option == OptionS.HOTKEYEDITOR || option == OptionS.HOTKEYTELEPORT)
			{
				return;
			}
			if (!defName.NullOrEmpty())
			{
				string text = Get(option);
				if (text.Contains(defName + ","))
				{
					string text2 = text.SubstringFrom(defName + ",", withoutIt: false);
					text2 = text2.SubstringTo(";", withoutIt: false);
					dicString[option].Value = text.Replace(text2, "");
				}
				dicString[option].Value = dicString[option].Value + val;
			}
			else
			{
				dicString[option].Value = "";
			}
			SaveSettings();
		}

		internal void Toggle(OptionB option)
		{
			if (option == OptionB.SHOWPAWNLIST || option == OptionB.SHOWTABS || option == OptionB.SHOWMINI)
			{
				dicBool[option].Value = !dicBool[option].Value;
			}
		}
	}

	private sealed class ModData
	{
		private bool useZombrella;

		internal Pawn p;

		private readonly Dictionary<EType, ServiceContainer> dicData;

		internal ModData()
		{
			dicData = new Dictionary<EType, ServiceContainer>();
			p = null;
			useZombrella = false;
			if (Get<ModOptions>(EType.Settings).Get(OptionB.MOREPAWNNAMES))
			{
				Label.AddNames(pack.RootDir.Replace("\\", "/"));
			}
		}

		~ModData()
		{
			foreach (EType key in dicData.Keys)
			{
				dicData[key].Dispose();
			}
			dicData.Clear();
		}

		internal List<T> ListOf<T>(EType eType)
		{
			if (!dicData.ContainsKey(eType))
			{
				CreateList(eType);
			}
			else if (dicData[eType] == null)
			{
				dicData.Remove(eType);
				CreateList(eType);
			}
			if (dicData.ContainsKey(eType))
			{
				return (List<T>)dicData[eType].GetService(typeof(List<T>));
			}
			return new List<T>();
		}

		internal T Get<T>(EType eType)
		{
			if (!dicData.ContainsKey(eType))
			{
				CreateType(eType);
			}
			else if (dicData[eType] == null)
			{
				dicData.Remove(eType);
				CreateType(eType);
			}
			if (dicData.ContainsKey(eType))
			{
				return (T)dicData[eType].GetService(typeof(T));
			}
			return default(T);
		}

		internal bool Has(EType eType)
		{
			return dicData.ContainsKey(eType);
		}

		internal void OnSettingsChanged(bool updateRenderOnly, bool updateKeyCode)
		{
			try
			{
				Log.Message("checking editor settings...");
				if (updateRenderOnly)
				{
					Get<Capturer>(EType.Capturer).ChangeRenderTextureParamter(Get<ModOptions>(EType.Settings).Get(OptionI.RESOLUTION), Get<ModOptions>(EType.Settings).Get(OptionB.HDRARGB));
					UpdateGraphics();
					return;
				}
				IsBodysizeActive = API.GetO(OptionB.SHOWBODYSIZEGENES);
				GeneTool.EnDisableBodySizeGenes();
				UpdateMainButton();
				UpdateArchitectObjects();
				UpdateUIParameter();
				if (updateKeyCode)
				{
					UpdateKeyBindings();
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.StackTrace);
			}
		}

		internal void UpdateGraphics()
		{
			Get<Capturer>(EType.Capturer).UpdatePawnGraphic(p);
		}

		internal void StartEditor(Pawn pawn)
		{
			Get<EditorUI>(EType.EditorUI).Start(pawn);
		}

		internal void ReInitSettings()
		{
			Get<ModOptions>(EType.Settings).ReInit();
		}

		private void CreateType(EType eType)
		{
			try
			{
				switch (eType)
				{
				case EType.EditorUI:
					Get<Capturer>(EType.Capturer);
					Get<HashSet<string>>(EType.GraphicPaths);
					AddClassContainer(new EditorUI(), eType);
					break;
				case EType.Capturer:
					AddClassContainer(new Capturer(), eType);
					break;
				case EType.ModsThingDef:
					AddClassContainer(DefTool.ListModnamesWithNull<ThingDef>(), eType);
					break;
				case EType.ModsWeapons:
					AddClassContainer(DefTool.ListModnamesWithNull((ThingDef y) => y.IsWeapon), eType);
					break;
				case EType.ModsApparel:
					AddClassContainer(DefTool.ListModnamesWithNull((ThingDef y) => y.IsApparel), eType);
					break;
				case EType.ModsHediffDef:
					AddClassContainer(DefTool.ListModnamesWithNull<HediffDef>(), eType);
					break;
				case EType.ModsTraitDef:
					AddClassContainer(DefTool.ListModnamesWithNull<TraitDef>(), eType);
					break;
				case EType.ModsHairDef:
					AddClassContainer(DefTool.ListModnamesWithNull<HairDef>(), eType);
					break;
				case EType.ModsBeardDef:
					AddClassContainer(DefTool.ListModnamesWithNull<BeardDef>(), eType);
					break;
				case EType.ModsAbilityDef:
					AddClassContainer(DefTool.ListModnamesWithNull<AbilityDef>(), eType);
					break;
				case EType.GraphicPaths:
					AddClassContainer(ApparelTool.CreateListOfGraphicPaths(), eType);
					break;
				default:
					if (eType != EType.ThoughtsAll)
					{
						switch (eType)
						{
						case EType.ThingCategoryDef:
							AddClassContainer((from d in DefTool.AllDefsWithLabelWithNull<ThingCategoryDef>()
								orderby d?.iconPath.NullOrEmpty() ?? false
								select d).ToHashSet(), eType);
							break;
						case EType.ThingCategory:
						{
							List<ThingCategory> allEnumsOfType = EnumTool.GetAllEnumsOfType<ThingCategory>();
							AddClassContainer(EnumTool.GetAllEnumsOfType<ThingCategory>().ToHashSet(), eType);
							break;
						}
						case EType.ApparelLayerDef:
							AddClassContainer(ApparelTool.ListOfApparelLayerDefs(insertNull: true).ToHashSet(), eType);
							break;
						case EType.BodyPartGroupDef:
							AddClassContainer(BodyTool.ListAllBodyPartGroupDefs(insertNull: true).ToHashSet(), eType);
							break;
						case EType.WeaponType:
							AddClassContainer(EnumTool.GetAllEnumsOfType<WeaponType>().ToHashSet(), eType);
							break;
						case EType.QualityCategory:
							AddClassContainer(EnumTool.GetAllEnumsOfType<QualityCategory>().ToHashSet(), eType);
							break;
						case EType.WeaponTraitDef:
							AddClassContainer(DefTool.ListBy((WeaponTraitDef t) => !t.defName.NullOrEmpty()).ToHashSet(), eType);
							break;
						case EType.ResearchProjectDef:
							AddClassContainer(DefTool.ListByModWithNull(null, (ResearchProjectDef t) => !t.defName.NullOrEmpty()).ToHashSet(), eType);
							break;
						case EType.Bullet:
							AddClassContainer(DefTool.AllDefsWithLabelWithNull((ThingDef b) => b.IsBullet()), eType);
							break;
						case EType.Factions:
							AddClassContainer(FactionTool.GetDicOfFactions(), eType);
							break;
						case EType.ExplosionSound:
							AddClassContainer(ThingTool.GetExplosionSounds(), eType);
							break;
						case EType.GunRelatedSound:
							AddClassContainer(ThingTool.GetGunRelatedSounds(), eType);
							break;
						case EType.GunShotSound:
							AddClassContainer(ThingTool.GetGunShotSounds(), eType);
							break;
						case EType.DamageDef:
							AddClassContainer(DefTool.AllDefsWithLabelWithNull<DamageDef>(), eType);
							break;
						case EType.MutantDef:
							AddClassContainer(DefTool.AllDefsWithLabelWithNull<MutantDef>(), eType);
							break;
						case EType.EffecterDef:
							AddClassContainer(DefTool.AllDefsWithNameWithNull<EffecterDef>(), eType);
							break;
						case EType.TurretPreset:
							Log.Message("creating default parameter list for turrets...");
							AddClassContainer(PresetObject.CreateDefaultTurrets(), eType);
							break;
						case EType.ObjectPreset:
							Log.Message("creating default parameter list for objects...");
							AddClassContainer(PresetObject.CreateDefaultObjects(), eType);
							break;
						case EType.GenePreset:
							Log.Message("creating default parameter list for genes...");
							AddClassContainer(PresetGene.CreateDefaults(), eType);
							break;
						case EType.LifestagePreset:
							Log.Message("creating default parameter list for lifestages...");
							AddClassContainer(PresetLifeStage.CreateDefaults(), eType);
							break;
						case EType.Search:
							AddClassContainer(new Dictionary<SearchTool.SIndex, SearchTool>(), eType);
							break;
						case EType.Settings:
							AddClassContainer(new ModOptions(), eType);
							break;
						case EType.UIContainers:
						{
							Dictionary<int, Building_CryptosleepCasket> dictionary = new Dictionary<int, Building_CryptosleepCasket>();
							dictionary.Add(0, null);
							AddClassContainer(dictionary, eType);
							break;
						}
						case EType.MainButton:
						{
							MainButtonDef createMainButton2 = DefTool.GetCreateMainButton("HotkeyEditor", Label.CHARACTER, Label.MAINBUTTON_DESCR, typeof(MainTabWindow_CharacterEditor), pack, Get<ModOptions>(EType.Settings).Get(OptionS.HOTKEYEDITOR), Get<ModOptions>(EType.Settings).Get(OptionB.SHOWINMENU));
							AddClassContainer(createMainButton2, eType);
							break;
						}
						case EType.TeleButton:
						{
							MainButtonDef createMainButton = DefTool.GetCreateMainButton("HotkeyTeleport", Label.TELEPORT, "quick teleport", typeof(Teleport_Character), pack, Get<ModOptions>(EType.Settings).Get(OptionS.HOTKEYTELEPORT), isVisible: false);
							AddClassContainer(createMainButton, eType);
							break;
						}
						}
						break;
					}
					goto case EType.ThoughtMemory;
				case EType.ThoughtMemory:
				case EType.ThoughtMemorySocial:
				case EType.ThoughtSituational:
				case EType.ThoughtSituationalSocial:
				case EType.ThoughtUnsupported:
				{
					Dictionary<EType, HashSet<ThoughtDef>> allThoughtLists = MindTool.GetAllThoughtLists();
					{
						foreach (EType key in allThoughtLists.Keys)
						{
							AddClassContainer(allThoughtLists[key], key);
						}
						break;
					}
				}
				}
			}
			catch (Exception ex)
			{
				Log.Error("Could not create default type. " + ex.Message + "\n" + ex.StackTrace);
			}
		}

		private void CreateList(EType eType)
		{
			try
			{
				switch (eType)
				{
				case EType.Pawns:
					AddServiceContainer(new List<Pawn>(), eType);
					break;
				case EType.PawnKindHuman:
					AddServiceContainer(PawnKindTool.GetHumanlikes(), eType);
					break;
				case EType.PawnKindAnimal:
					AddServiceContainer(PawnKindTool.GetAnimals(), eType);
					break;
				case EType.PawnKindOther:
					AddServiceContainer(PawnKindTool.GetOther(), eType);
					break;
				case EType.PawnKindListed:
					AddServiceContainer(PawnKindTool.ListOfPawnKindDef(Faction.OfPlayer, Label.COLONISTS, null), eType);
					break;
				case EType.Bodies:
					AddServiceContainer(DefTool.ListAll<BodyTypeDef>(), eType);
					break;
				case EType.MentalStates:
					AddServiceContainer(MindTool.GetAllMentalStates(), eType);
					break;
				case EType.Inspirations:
					AddServiceContainer(MindTool.GetAllInspirations(), eType);
					break;
				case EType.ApparelTags:
					AddServiceContainer(ThingTool.GetAllApparelTags(), eType);
					break;
				case EType.OutfitTags:
					AddServiceContainer(ThingTool.GetAllOutfitTags(), eType);
					break;
				case EType.WeaponTags:
					AddServiceContainer(ThingTool.GetAllWeaponTags(), eType);
					break;
				case EType.TradeTags:
					AddServiceContainer(ThingTool.GetAllTradeTags(), eType);
					break;
				case EType.ExclusionTags:
					AddServiceContainer(GeneTool.GetAllExclusionTags(), eType);
					break;
				case EType.HairTags:
					AddServiceContainer(GeneTool.GetAllHairTags(), eType);
					break;
				case EType.BeardTags:
					AddServiceContainer(GeneTool.GetAllBeardTags(), eType);
					break;
				case EType.StuffCategory:
					AddServiceContainer(ThingTool.GetAllStuffCategories(), EType.StuffCategory);
					break;
				case EType.TechLevel:
					AddServiceContainer(EnumTool.GetAllEnumsOfType<TechLevel>(), EType.TechLevel);
					break;
				case EType.Tradeability:
					AddServiceContainer(EnumTool.GetAllEnumsOfType<Tradeability>(), EType.Tradeability);
					break;
				case EType.GasType:
				{
					List<GasType> allEnumsOfType = EnumTool.GetAllEnumsOfType<GasType>();
					List<GasType?> list = new List<GasType?>();
					list.Add(null);
					foreach (GasType item in allEnumsOfType)
					{
						list.Add(item);
					}
					AddServiceContainer(list, EType.GasType);
					break;
				}
				case EType.StatCategoryApparel:
					AddServiceContainer(ThingTool.GetAllStatCategoriesApparel(), eType);
					break;
				case EType.StatCategoryWeapon:
					AddServiceContainer(ThingTool.GetAllStatCategoriesWeapon(), eType);
					break;
				case EType.StatCategoryOnEquip:
					AddServiceContainer(ThingTool.GetAllStatCategoriesOnEquip(), eType);
					break;
				case EType.CostItems:
					AddServiceContainer(ThingTool.GetAllCostThingDefs(), EType.CostItems);
					break;
				case EType.StatDefWeapon:
					AddServiceContainer(ThingTool.GetAllWeaponStatDefs(), eType);
					break;
				case EType.StatDefApparel:
					AddServiceContainer(ThingTool.GetAllApparelStatDefs(), eType);
					break;
				case EType.StatDefOnEquip:
					AddServiceContainer(ThingTool.GetAllOnEquipStatDefs(), eType);
					break;
				}
			}
			catch (Exception ex)
			{
				Log.Error("Could not create default list. " + ex.Message + "\n" + ex.StackTrace);
			}
		}

		private void AddClassContainer<T>(T t, EType eType)
		{
			dicData.Add(eType, new ServiceContainer());
			dicData[eType].AddService(typeof(T), t);
		}

		private void AddServiceContainer<T>(List<T> l, EType eType)
		{
			dicData.Add(eType, new ServiceContainer());
			dicData[eType].AddService(l.GetType(), l);
		}

		internal void UpdateUIParameter()
		{
			EditorUI editorUI = Get<EditorUI>(EType.EditorUI);
			if (editorUI != null)
			{
				_ = editorUI.windowRect;
				if (true)
				{
					editorUI.windowRect.width = editorUI.InitialSize.x;
					editorUI.windowRect.height = editorUI.InitialSize.y;
				}
				editorUI.forcePause = Get<ModOptions>(EType.Settings).Get(OptionB.PAUSEGAME);
			}
		}

		internal void UpdateMainButton()
		{
			MainButtonDef mainButtonDef = Get<MainButtonDef>(EType.MainButton);
			if (mainButtonDef != null)
			{
				mainButtonDef.buttonVisible = Get<ModOptions>(EType.Settings).Get(OptionB.SHOWINMENU);
			}
			Get<MainButtonDef>(EType.TeleButton);
		}

		private void UpdateKeyBindings()
		{
			try
			{
				string text = Get<ModOptions>(EType.Settings).Get(OptionS.HOTKEYTELEPORT);
				string text2 = Get<ModOptions>(EType.Settings).Get(OptionS.HOTKEYEDITOR);
				KeyBindingDef keyBindingDef = DefTool.KeyBindingDef("HotkeyTeleport");
				KeyBindingDef keyBindingDef2 = DefTool.KeyBindingDef("HotkeyEditor");
				if (keyBindingDef == null)
				{
					keyBindingDef = DefTool.TryCreateKeyBinding("HotkeyTeleport", text, Label.O_HOTKEYTELEPORT, Label.O_DESC_HOTKEYTELEPORT);
				}
				if (keyBindingDef2 == null)
				{
					keyBindingDef2 = DefTool.TryCreateKeyBinding("HotkeyEditor", text2, Label.O_HOTKEYEDITOR, Label.O_DESC_HOTKEYEDITOR);
				}
				Get<MainButtonDef>(EType.TeleButton).hotKey = keyBindingDef;
				Get<MainButtonDef>(EType.MainButton).hotKey = keyBindingDef2;
				if (Enum.TryParse<KeyCode>(text, out var result))
				{
					KeyPrefs.KeyPrefsData.SetBinding(DefTool.KeyBindingDef("HotkeyTeleport"), KeyPrefs.BindingSlot.A, result);
					KeyPrefs.KeyPrefsData.CheckConflictsFor(DefTool.KeyBindingDef("HotkeyTeleport"), KeyPrefs.BindingSlot.A);
				}
				if (Enum.TryParse<KeyCode>(text2, out result))
				{
					KeyPrefs.KeyPrefsData.SetBinding(DefTool.KeyBindingDef("HotkeyEditor"), KeyPrefs.BindingSlot.A, result);
					KeyPrefs.KeyPrefsData.CheckConflictsFor(DefTool.KeyBindingDef("HotkeyEditor"), KeyPrefs.BindingSlot.A);
				}
				KeyPrefs.Save();
			}
			catch
			{
			}
		}

		private void UpdateArchitectObjects()
		{
			bool flag = Get<ModOptions>(EType.Settings).Get(OptionB.ZOMBOBJECTS);
			if (useZombrella == flag)
			{
				return;
			}
			useZombrella = flag;
			ThingDef thingDef = DefTool.ThingDef("Zombrella");
			ThingDef thingDef2 = DefTool.ThingDef("Zombgrella");
			if (useZombrella)
			{
				if (thingDef == null)
				{
					ThingTool.CreateBuilding("Zombrella", "Zombrella", Label.DESC_CASCET, typeof(CharacterEditorCascet), "CryptosleepCasket");
				}
				if (thingDef2 == null)
				{
					ThingTool.CreateBuilding("Zombgrella", "Zombgrella", Label.DESC_GRAVE, typeof(CharacterEditorGrave), "Grave");
				}
			}
			thingDef = DefTool.ThingDef("Zombrella");
			thingDef2 = DefTool.ThingDef("Zombgrella");
			if (useZombrella)
			{
				if (thingDef != null)
				{
					thingDef.designatorDropdown = DefTool.DesignatorDropdownGroupDef("Zombrella");
					thingDef.designationCategory = DefTool.DesignationCategoryDef("Misc");
				}
				if (thingDef2 != null)
				{
					thingDef2.designatorDropdown = DefTool.DesignatorDropdownGroupDef("Zombrella");
					thingDef2.designationCategory = DefTool.DesignationCategoryDef("Misc");
				}
			}
			else
			{
				if (thingDef != null)
				{
					thingDef.designatorDropdown = null;
					thingDef.designationCategory = null;
				}
				if (thingDef2 != null)
				{
					thingDef2.designatorDropdown = null;
					thingDef2.designationCategory = null;
				}
			}
			DefTool.DesignatorDropdownGroupDef("Zombrella")?.ResolveReferences();
			DefTool.DesignationCategoryDef("Misc")?.ResolveReferences();
			try
			{
				ThingCategoryDef.Named("BuildingsMisc").ResolveReferences();
				ThingCategoryDef.Named("BuildingsMisc").PostLoad();
				ThingCategoryDefOf.Root.ResolveReferences();
				ThingCategoryDefOf.Root.PostLoad();
			}
			catch (Exception ex)
			{
				Log.Error(ex.StackTrace);
			}
		}
	}

	private sealed class EditorUI : Window
	{
		private enum TabType
		{
			BlockBio,
			BlockHealth,
			BlockInfo,
			BlockInventory,
			BlockLog,
			BlockNeeds,
			BlockPawnList,
			BlockPerson,
			BlockRecords,
			BlockSocial,
			None
		}

		private struct coord
		{
			internal int x;

			internal int y;

			internal int w;

			internal int h;
		}

		private class BlockBio
		{
			private int oY = 0;

			private int iTickInputName;

			internal int iTickInputAge;

			private int iTickInputSkill;

			private int iChronoAge;

			private int iAge;

			private string ageBuffer;

			private string chronoAgeBuffer;

			private string ttip;

			private string selectedParamName = "";

			private Regex regexInt;

			private Vector2 scrollPosParam = default(Vector2);

			private bool bRemoveAbility;

			private bool bRemoveTrait;

			private bool bShowPsyValues = false;

			private Vector2 scrollTraits;

			private List<Trait> lCopyTraits;

			private List<Ability> lCopyAbilities;

			private List<SkillRecord> lOfCopySkills;

			private List<Pawn> lOfColonists;

			private RoyalTitle currentTitle;

			private BackstoryDef oBackAdult;

			private BackstoryDef oBackChild;

			private HashSet<RoyalTitleDef> lOfRoyalTitles;

			private HashSet<ColorDef> lOfColorDefs;

			private HashSet<MutantDef> lOfMutantDefs;

			private Pawn selectedTrainer;

			private Ideo selectedIdeo = null;

			private string uniqueID;

			private Func<Trait, string> FTraitLabel = (Trait t) => t.LabelCap ?? t.Label ?? t.def.label;

			private Func<Trait, Trait, bool> FTraitComparator = (Trait tA, Trait tB) => tA == tB;

			private Func<Trait, string> FTraitTooltip;

			private string tooltip1 = "";

			private string tooltip2 = "";

			private bool rememberDevMode = false;

			private string GetTraitTooltip(Trait t)
			{
				try
				{
					return t.TipString(API.Pawn);
				}
				catch
				{
					return "";
				}
			}

			internal BlockBio()
			{
				iTickInputName = 0;
				iTickInputAge = 0;
				iTickInputSkill = 0;
				iChronoAge = 0;
				iAge = 0;
				ageBuffer = "";
				chronoAgeBuffer = "";
				uniqueID = null;
				FTraitTooltip = GetTraitTooltip;
				bRemoveTrait = false;
				bRemoveAbility = false;
				scrollTraits = default(Vector2);
				lCopyTraits = new List<Trait>();
				lCopyAbilities = new List<Ability>();
				lOfCopySkills = new List<SkillRecord>();
				lOfColonists = ((Current.Game?.World == null) ? null : PawnxTool.GetPawnList(Label.COLONISTS, onMap: true, Faction.OfPlayer));
				lOfRoyalTitles = DefTool.ListByModWithNull(null, (RoyalTitleDef def) => !def.defName.NullOrEmpty()).ToHashSet();
				lOfColorDefs = DefTool.ListByModWithNull(null, (ColorDef def) => !def.defName.NullOrEmpty()).ToHashSet();
				lOfMutantDefs = PawnxTool.AllMutantDefs;
				selectedTrainer = null;
				currentTitle = null;
				regexInt = new Regex("^[0-9]*");
			}

			internal void Draw(coord c)
			{
				int x = c.x;
				int y = c.y;
				int w = c.w;
				int h = c.h;
				if (API.Pawn != null)
				{
					DrawName(x + 20, y + 30, w, 30);
					DrawDesc(x + 20, y + 60, w, 30);
					DrawBackstory(x + 20, y + 100);
					DrawIncapableOf(x + 20, y + 205);
					DrawTraits(x + 20, y + 305, h);
					DrawSkills(x + 330, y + 100);
					DrawAbilities(x + 330, y + 500);
					DrawPsycasts(x + 590, y + 380);
					DrawTraining(x + 20, y + 100, w, h - 100);
					DrawFaction(x + 600, y + 30, 250);
					DrawIdeo(x + 600, y + 60, 250);
					DrawXeno(x + 600, y + 90, 250);
					oY = 0;
					DrawFavorite(x + 600, y + 120 + oY, 250);
					DrawMutant(x + 600, y + 120 + oY, 250);
					DrawTitle(x + 600, y + 120 + oY, 250);
					DrawExtendables(x + 600, y + 120 + oY, 30);
					DrawCapsule(x + 590, y + 220, 200, 144);
					DrawLowerButtons(x, y, w, h);
				}
			}

			private void DrawLowerButtons(int x, int y, int w, int h)
			{
				try
				{
					if (API.Pawn.Faction != Faction.OfPlayer && API.Pawn.Faction != Faction.OfMechanoids && !API.Pawn.Dead)
					{
						GUI.color = Color.white;
						SZWidgets.ButtonText(new Rect(x + w - 120, y + h - 30, 120f, 30f), Label.RECRUIT, ARecruit);
						SZWidgets.ButtonText(new Rect(x + w - 240, y + h - 30, 120f, 30f), Label.ENSLAVE, AEnslave);
					}
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
			}

			private void ARecruit()
			{
				if (API.Pawn.guest != null)
				{
					API.Pawn.guest.Recruitable = true;
				}
				DebugActionsUtility.DustPuffFrom(API.Pawn);
				API.Pawn.RecruitPawn();
			}

			private void AEnslave()
			{
				DebugActionsUtility.DustPuffFrom(API.Pawn);
				API.Pawn.EnslavePawn();
			}

			private void DrawBiologicalAge(int x, int y)
			{
				iAge = API.Pawn.ageTracker.AgeBiologicalYears;
				SZWidgets.ButtonImage(x, y, 24f, 24f, "bbackward", ASubAge);
				x += 25;
				Text.WordWrap = false;
				Widgets.Label(new Rect(x, y + 1, 150f, 24f), "AgeBiological".Translate().ToString().SubstringTo(":", withoutIt: false));
				Text.WordWrap = true;
				x += 150;
				ageBuffer = Widgets.TextField(new Rect(x, y, 50f, 24f), ageBuffer, 4, regexInt);
				x += 50;
				SZWidgets.ButtonImage(x, y, 24f, 24f, "bforward", AAddAge);
				int result = 0;
				if (int.TryParse(ageBuffer, out result) && iAge != result && result > 0)
				{
					API.Pawn.SetAge(result);
					API.UpdateGraphics();
				}
			}

			private void DrawChronologicalAge(int x, int y)
			{
				iChronoAge = API.Pawn.ageTracker.AgeChronologicalYears;
				SZWidgets.ButtonImage(x, y, 24f, 24f, "bbackward", ASubChronoAge);
				x += 25;
				Text.WordWrap = false;
				Widgets.Label(new Rect(x, y + 1, 165f, 24f), "AgeChronological".Translate().ToString().SubstringTo(":", withoutIt: false));
				Text.WordWrap = true;
				x += 165;
				chronoAgeBuffer = Widgets.TextField(new Rect(x, y, 50f, 24f), chronoAgeBuffer, 5, regexInt);
				x += 50;
				SZWidgets.ButtonImage(x, y, 24f, 24f, "bforward", AAddChronoAge);
				x += 30;
				SZWidgets.ButtonImage(x, y, 24f, 24f, "bstar", AAddBirthdayTick);
				int result = 0;
				if (int.TryParse(chronoAgeBuffer, out result) && iChronoAge.ToString() != chronoAgeBuffer && result > 0)
				{
					API.Pawn.SetChronoAge(result);
				}
			}

			private void DrawDesc(int x, int y, int w, int h)
			{
				try
				{
					Text.Font = GameFont.Small;
					Rect rect = new Rect(x, y, w - 260, h - 4);
					Rect rect2 = new Rect(rect.x + 100f, rect.y, rect.width - 300f, rect.height);
					Rect rect3 = new Rect(x, y, 100f, 25f);
					if (iTickInputAge > 0)
					{
						SZWidgets.ButtonInvisible(new Rect(x, y + h, w, 600f), delegate
						{
							iTickInputAge = 0;
						});
						if (uniqueID != API.Pawn.ThingID)
						{
							iTickInputAge = 0;
						}
						else
						{
							DrawBiologicalAge((int)rect.x - 25, (int)rect.y);
							DrawChronologicalAge((int)rect.x + 250, (int)rect.y);
							iTickInputAge--;
						}
					}
					else
					{
						SZWidgets.ButtonInvisible(rect3, AConfirmRaceChange, RACEDESC(API.Pawn.def));
						SZWidgets.ButtonInvisible(rect2, ABeginAgeChange);
						Widgets.Label(rect, API.Pawn.GetPawnDescription());
					}
					if (API.Pawn == null)
					{
						return;
					}
					TooltipHandler.TipRegion(rect3, RACEDESC(API.Pawn.def));
					if (API.Pawn.ageTracker != null && !API.Pawn.ageTracker.AgeTooltipString.NullOrEmpty())
					{
						TooltipHandler.TipRegion(rect2, () => API.Pawn.ageTracker.AgeTooltipString, 6873641);
					}
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
			}

			private void DrawNameTripe(Rect rName)
			{
				GUI.BeginGroup(rName);
				NameTriple nameTriple = API.Pawn.Name as NameTriple;
				GUI.color = Color.white;
				string text = Widgets.TextField(new Rect(0f, 0f, 100f, 28f), nameTriple.First, 17, CharacterCardUtility.ValidNameRegex);
				if (nameTriple.Nick == text || nameTriple.Nick == nameTriple.Last)
				{
					GUI.color = new Color(1f, 1f, 1f, 0.5f);
				}
				string text2 = Widgets.TextField(new Rect(105f, 0f, 100f, 28f), nameTriple.Nick, 17, CharacterCardUtility.ValidNameRegex);
				GUI.color = Color.white;
				string text3 = Widgets.TextField(new Rect(210f, 0f, 100f, 28f), nameTriple.Last, 17, CharacterCardUtility.ValidNameRegex);
				SZWidgets.ButtonImageCol(new Rect(320f, 2f, 25f, 25f), "brandom", AChangeNameTriple, Color.white, Label.TIP_CHANGE_NAME);
				SZWidgets.ButtonImageCol(new Rect(350f, 2f, 25f, 25f), "brandom", AChangeNameTripleAll, ColorLibrary.Beige, Label.TIP_CHANGE_NAME_ALL);
				if (nameTriple.First != text || nameTriple.Nick != text2 || nameTriple.Last != text3)
				{
					API.Pawn.Name = new NameTriple(text, text2, text3);
					iTickInputName = 1200;
				}
				TooltipHandler.TipRegion(new Rect(0f, 0f, 100f, 30f), "FirstNameDesc".Translate());
				TooltipHandler.TipRegion(new Rect(105f, 0f, 100f, 30f), "ShortIdentifierDesc".Translate());
				TooltipHandler.TipRegion(new Rect(210f, 0f, 100f, 30f), "LastNameDesc".Translate());
				GUI.EndGroup();
				GUI.color = Color.white;
			}

			private void ResetTickInput()
			{
				iTickInputName = 1200;
			}

			private void AChangeNameTriple()
			{
				API.Pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(API.Pawn);
				ResetTickInput();
			}

			private void AChangeNameTripleAll()
			{
				GenderPossibility gp = ((API.Pawn.gender != Gender.Male) ? ((API.Pawn.gender == Gender.Female) ? GenderPossibility.Female : GenderPossibility.Either) : GenderPossibility.Male);
				API.Pawn.Name = PawnNameDatabaseSolid.GetListForGender(gp).RandomElement();
				ResetTickInput();
			}

			private void DrawNameSingle(Rect rName)
			{
				GUI.BeginGroup(rName);
				if (API.Pawn.Name == null)
				{
					API.Pawn.Name = new NameSingle(API.Pawn.kindDef.label);
				}
				NameSingle nameSingle = API.Pawn.Name as NameSingle;
				string text = nameSingle.Name ?? API.Pawn.kindDef.label;
				GUI.color = Color.white;
				Rect rect = new Rect(0f, 0f, 200f, 28f);
				text = Widgets.TextField(rect, text, 64, CharacterCardUtility.ValidNameRegex);
				if (nameSingle.Name != text)
				{
					API.Pawn.Name = new NameSingle(text);
					iTickInputName = 1200;
				}
				Rect butRect = new Rect(rect.x + rect.width + 10f, rect.y, rect.height, rect.height);
				if (Widgets.ButtonImage(butRect, ContentFinder<Texture2D>.Get("brandom")))
				{
					API.Pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(API.Pawn);
				}
				TooltipHandler.TipRegion(rect, "FirstNameDesc".Translate());
				GUI.EndGroup();
			}

			private void DrawName(int x, int y, int w, int h)
			{
				try
				{
					Rect rect = new Rect(x, y, w, h);
					if (iTickInputName > 0)
					{
						SZWidgets.ButtonInvisible(new Rect(x, y + h, w, 600f), delegate
						{
							iTickInputName = 0;
						});
						if (API.Pawn.Name?.GetType() == typeof(NameTriple))
						{
							DrawNameTripe(rect);
						}
						else
						{
							DrawNameSingle(rect);
						}
						iTickInputName--;
						return;
					}
					Text.Font = GameFont.Medium;
					currentTitle = (API.Pawn.HasRoyalTitle() ? API.Pawn.royalty.AllTitlesForReading.First() : null);
					if (currentTitle != null)
					{
						int num = currentTitle.def.LabelCap.Length * 12;
						string toolTip = "";
						try
						{
							if (API.Pawn.royalty != null && currentTitle.faction != null)
							{
								toolTip = (string)typeof(CharacterCardUtility).CallMethod("GetTitleTipString", new object[4]
								{
									API.Pawn,
									currentTitle.faction,
									currentTitle,
									API.Pawn.royalty.GetFavor(currentTitle.faction)
								});
							}
						}
						catch
						{
						}
						SZWidgets.Label(rect, currentTitle.def.GetLabelCapFor(API.Pawn) + " " + API.Pawn.GetPawnNameColored(needFull: true));
						SZWidgets.ButtonInvisibleVar(new Rect(x, y, num, h), AShowTitle, currentTitle, toolTip);
						SZWidgets.ButtonInvisible(new Rect(x + num, y, 300f, h), ABeginEditName, "Rename".Translate());
					}
					else
					{
						SZWidgets.Label(rect, API.Pawn.GetPawnNameColored(needFull: true));
						SZWidgets.ButtonInvisible(new Rect(x, y, 300f, h), ABeginEditName, "Rename".Translate());
					}
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
			}

			private void ARemoveTitle()
			{
				if (currentTitle != null)
				{
					API.Pawn.RemoveTitle(currentTitle);
				}
			}

			private void ABeginEditName()
			{
				iTickInputName = 1200;
			}

			private void AShowTitle(RoyalTitle r)
			{
				WindowTool.Open(new Dialog_InfoCard(r.def, r.faction));
			}

			private void ASetTitle(RoyalTitleDef r)
			{
				if (r == null)
				{
					ARemoveTitle();
				}
				else
				{
					API.Pawn.SetTitle(r.defName);
				}
			}

			private void ASetFavColor(ColorDef c)
			{
				API.Pawn.SetFavColor(c);
			}

			private void AConfirmRaceChange()
			{
				WindowTool.Open(new DialogChangeRace(API.Pawn));
			}

			private void ABeginAgeChange()
			{
				uniqueID = API.Pawn.ThingID;
				iAge = API.Pawn.ageTracker.AgeBiologicalYears;
				ageBuffer = iAge.ToString();
				iChronoAge = API.Pawn.ageTracker.AgeChronologicalYears;
				chronoAgeBuffer = iChronoAge.ToString();
				iTickInputAge = 1200;
			}

			private void AAddAge()
			{
				iAge++;
				API.Pawn.SetAge(iAge);
				ageBuffer = iAge.ToString();
				iTickInputAge = 1200;
				API.UpdateGraphics();
			}

			private void ASubAge()
			{
				if (iAge > 0)
				{
					iAge--;
				}
				API.Pawn.SetAge(iAge);
				ageBuffer = iAge.ToString();
				iTickInputAge = 1200;
				API.UpdateGraphics();
			}

			private void AAddChronoAge()
			{
				if (iChronoAge < 15498)
				{
					iChronoAge++;
				}
				API.Pawn.SetChronoAge(iChronoAge);
				chronoAgeBuffer = iChronoAge.ToString();
				iTickInputAge = 1200;
			}

			private void ASubChronoAge()
			{
				if (iChronoAge > 0)
				{
					iChronoAge--;
				}
				API.Pawn.SetChronoAge(iChronoAge);
				chronoAgeBuffer = iChronoAge.ToString();
				iTickInputAge = 1200;
			}

			private void AAddBirthdayTick()
			{
				iTickInputAge = 0;
				WindowTool.Open(new DialogChangeBirthday(API.Pawn));
			}

			private void DrawBackstory(int x, int y)
			{
				try
				{
					if (API.Pawn.story != null)
					{
						Text.Font = GameFont.Medium;
						Widgets.Label(new Rect(x, y, 250f, 30f), "Backstory".Translate());
						y += 35;
						Rect rect = new Rect(x - 24, y, 320f, 30f);
						if (Mouse.IsOver(rect))
						{
							tooltip1 = API.Pawn.story.Childhood?.FullDescriptionFor(API.Pawn).Resolve();
						}
						SZWidgets.NavSelectorImageBox(rect, AChangeChildhoodUI, ARandomChildhood, ASetPrevChildhood, ASetNextChildhood, null, null, "Childhood".Translate() + ": " + API.Pawn.story.Childhood?.TitleCapFor(API.Pawn.gender), tooltip1);
						y += 30;
						Rect rect2 = new Rect(x - 24, y, 320f, 30f);
						if (Mouse.IsOver(rect2))
						{
							tooltip2 = API.Pawn.story.Adulthood?.FullDescriptionFor(API.Pawn).Resolve();
						}
						SZWidgets.NavSelectorImageBox(rect2, AChangeAdulthoodUI, ARandomAdulthood, ASetPrevAdulthood, ASetNextAdulthood, null, null, "Adulthood".Translate() + ": " + API.Pawn.story.Adulthood?.TitleCapFor(API.Pawn.gender), tooltip2);
					}
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
			}

			private void ARandomChildhood()
			{
				RememberOldBackstory();
				API.Pawn.SetBackstory(next: true, random: true, isChildhood: true, notDisabled: false);
				RecalcSkills();
			}

			private void ARandomAdulthood()
			{
				RememberOldBackstory();
				API.Pawn.SetBackstory(next: true, random: true, isChildhood: false, notDisabled: false);
				RecalcSkills();
			}

			private void ASetNextChildhood()
			{
				RememberOldBackstory();
				API.Pawn.SetBackstory(next: true, random: false, isChildhood: true, notDisabled: false);
				RecalcSkills();
			}

			private void ASetPrevChildhood()
			{
				RememberOldBackstory();
				API.Pawn.SetBackstory(next: false, random: false, isChildhood: true, notDisabled: false);
				RecalcSkills();
			}

			private void AChangeChildhoodUI()
			{
				WindowTool.Open(new DialogChangeBackstory(_isChildhood: true));
			}

			private void AChangeAdulthoodUI()
			{
				WindowTool.Open(new DialogChangeBackstory(_isChildhood: false));
			}

			private void ASetNextAdulthood()
			{
				RememberOldBackstory();
				API.Pawn.SetBackstory(next: true, random: false, isChildhood: false, notDisabled: false);
				RecalcSkills();
			}

			private void ASetPrevAdulthood()
			{
				RememberOldBackstory();
				API.Pawn.SetBackstory(next: false, random: false, isChildhood: false, notDisabled: false);
				RecalcSkills();
			}

			public void RememberOldBackstory()
			{
				oBackAdult = API.Pawn.story.GetBackstory(BackstorySlot.Adulthood);
				oBackChild = API.Pawn.story.GetBackstory(BackstorySlot.Childhood);
			}

			public int TryGetSkillValueForSkill(BackstoryDef def, SkillDef skillDef)
			{
				int result = 0;
				if (def != null)
				{
					foreach (SkillGain skillGain in def.skillGains)
					{
						if (skillGain.skill == skillDef)
						{
							result = skillGain.amount;
							break;
						}
					}
				}
				return result;
			}

			public void RecalcSkills()
			{
				BackstoryDef backstory = API.Pawn.story.GetBackstory(BackstorySlot.Adulthood);
				BackstoryDef backstory2 = API.Pawn.story.GetBackstory(BackstorySlot.Childhood);
				foreach (SkillRecord skill in API.Pawn.skills.skills)
				{
					int num = TryGetSkillValueForSkill(oBackAdult, skill.def);
					int num2 = TryGetSkillValueForSkill(oBackChild, skill.def);
					int num3 = TryGetSkillValueForSkill(backstory, skill.def);
					int num4 = TryGetSkillValueForSkill(backstory2, skill.def);
					skill.levelInt -= ((oBackAdult != null) ? num : 0);
					skill.levelInt -= ((oBackChild != null) ? num2 : 0);
					skill.levelInt += ((backstory != null) ? num3 : 0);
					skill.levelInt += ((backstory2 != null) ? num4 : 0);
				}
			}

			private void DrawIncapableOf(int x, int y)
			{
				try
				{
					if (API.Pawn.story != null)
					{
						Text.Font = GameFont.Medium;
						Widgets.Label(new Rect(x, y, 270f, 30f), "IncapableOf".Translate(API.Pawn));
						y += 30;
						Text.Font = GameFont.Small;
						Widgets.Label(new Rect(x, y, 270f, 60f), API.Pawn.GetIncapableOf(out ttip));
						SZWidgets.ButtonInvisible(new Rect(x, y, 270f, 60f), null, ttip);
					}
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
			}

			private void DrawFaction(int x, int y, int w)
			{
				GUI.color = Color.white;
				if (API.Pawn.Faction != null)
				{
					try
					{
						string text = API.Pawn.Faction.Name.CapitalizeFirst().Colorize(API.Pawn.Faction.Color);
						string texPath = API.Pawn.Faction?.def?.factionIconPath ?? "bwhite";
						Color facionColor = API.Pawn.GetFacionColor();
						string factionFullDesc = API.Pawn.GetFactionFullDesc();
						SZWidgets.ButtonTextureTextHighlight2(new Rect(x, y, w, 30f), text, texPath, facionColor, AChangeFaction, factionFullDesc);
						return;
					}
					catch (Exception e)
					{
						MessageTool.DebugException(e);
						return;
					}
				}
				SZWidgets.ButtonTextureTextHighlight2(new Rect(x, y, w, 30f), Label.NONE, "bwhite", Color.white, AChangeFaction);
			}

			private void DrawIdeo(int x, int y, int w)
			{
				Pawn pawn = API.Pawn;
				if (!ModsConfig.IdeologyActive || pawn.ideo == null)
				{
					return;
				}
				Window windowOf = WindowTool.GetWindowOf<Dialog_ConfigureIdeo>();
				if (windowOf != null)
				{
					selectedIdeo = IdeoUIUtility.selected;
				}
				string text = ((pawn.Ideo != null) ? pawn.Ideo.name : "");
				Texture2D icon = ((pawn.Ideo != null) ? pawn.Ideo.Icon : Texture2D.whiteTexture);
				Color color = ((pawn.Ideo != null) ? pawn.Ideo.Color : Color.white);
				Precept_Role precept_Role = ((pawn.Ideo != null) ? pawn.Ideo.GetRole(API.Pawn) : null);
				string text2 = ((precept_Role != null) ? precept_Role.GetTip() : ((pawn.Ideo != null) ? pawn.Ideo.description : ""));
				string pawnName = pawn.GetPawnName();
				if (!pawnName.NullOrEmpty() && pawn.ideo != null && pawn.Ideo != null && !pawn.Dead)
				{
					string text3 = "CertaintyInIdeo".Translate(pawn.Named("PAWN"), pawn.Ideo.Named("IDEO")) + ": " + pawn.ideo.Certainty.ToStringPercent() + "\n\n" + "CertaintyChangePerDay".Translate() + ": " + pawn.ideo.CertaintyChangePerDay.ToStringPercent() + "\n\n";
					if (!text3.NullOrEmpty())
					{
						text2 = text2 + "\n\n" + pawnName.Colorize(ColorTool.colBeige) + text3.Replace(pawnName, "");
					}
				}
				SZWidgets.ButtonTextureTextHighlight(new Rect(x, y, w + 100, 30f), text, icon, color, AChangeIdeo, text2);
			}

			private void DrawTitle(int x, int y, int w)
			{
				if (!ModsConfig.RoyaltyActive || !API.Pawn.HasRoyaltyTracker())
				{
					return;
				}
				try
				{
					SZWidgets.ButtonTextureTextHighlight2(new Rect(x, y, w, 30f), (currentTitle != null) ? currentTitle.Label : "", "UI/Buttons/Renounce", ColorTool.colBeige, null, Label.SETROYALTITLE, withButton: false, 15f);
					SZWidgets.FloatMenuOnButtonInvisible(new Rect(x, y, w, 30f), lOfRoyalTitles, (RoyalTitleDef rtitle) => (rtitle == null) ? Label.NONE : rtitle.GetLabelCapFor(API.Pawn), ASetTitle, Label.SETROYALTITLE);
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
				oY += 30;
			}

			private void DrawMutant(int x, int y, int w)
			{
				if (!ModsConfig.AnomalyActive)
				{
					return;
				}
				try
				{
					SZWidgets.ButtonTextureTextHighlight2(new Rect(x, y, w, 30f), API.Pawn.GetMutantName(), "bmutant", API.Pawn.HasMutantTracker() ? Color.red : Color.white, null, "", withButton: false, 15f);
					SZWidgets.FloatMenuOnButtonInvisible(new Rect(x, y, w, 30f), lOfMutantDefs, (MutantDef m) => (m == null) ? Label.NONE : m.LabelCap.RawText, delegate(MutantDef m)
					{
						OnChangeMutant(m);
					}, API.Pawn.HasMutantTracker() ? API.Pawn.GetMutantDesc() : Label.MUTATION_TIP);
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
				oY += 30;
			}

			private void DrawExtendables(int x, int y, int w)
			{
				try
				{
					int x2 = DrawPersonality(x, y, w);
					x2 = DrawRJW(x2, y, w);
				}
				catch
				{
				}
			}

			private void DrawFavorite(int x, int y, int w)
			{
				if (API.Pawn.HasStoryTracker())
				{
					SZWidgets.ButtonTextureTextHighlight2(new Rect(x, y, w, 30f), API.Pawn.GetFavColor().SLabel(), "bfavcolor", API.Pawn.GetFavColorDepricated(), null, "", withButton: false, 15f);
					SZWidgets.FloatMenuOnButtonInvisibleColorDef(new Rect(x, y, w, 30f), lOfColorDefs, (ColorDef rcolor) => rcolor.SLabel(), ASetFavColor, CompatibilityTool.GetFavoriteColorTooltip(API.Pawn));
					oY += 30;
				}
			}

			private int DrawRJW(int x, int y, int w)
			{
				if (!IsRJWActive)
				{
					return x;
				}
				ThingComp rJWComp = API.Pawn.GetRJWComp();
				if (rJWComp == null)
				{
					return x;
				}
				SZWidgets.ButtonImage(new Rect(x, y, w, w), "RJW-LOGO", AChangeRJW, CompatibilityTool.GetRJWTooltip(API.Pawn));
				return x + w + 2;
			}

			private int DrawPersonality(int x, int y, int w)
			{
				if (!IsPersonalitiesActive)
				{
					return x;
				}
				ThingComp persoComp = API.Pawn.GetPersoComp();
				if (persoComp == null)
				{
					return x;
				}
				SZWidgets.ButtonImage(new Rect(x, y, w - 2, w - 2), "bmemory", AChangePersonality, CompatibilityTool.GetPersonalitiesTooltip(API.Pawn));
				return x + w + 2;
			}

			private void OnChangeMutant(MutantDef m)
			{
				API.Pawn.SetUnsetMutant(m);
				API.UpdateGraphics();
			}

			private void AChangeMutant()
			{
				WindowTool.Open(new DialogChangeMutant());
			}

			private void AChangeRJW()
			{
				CompatibilityTool.OpenRJWDialog(API.Pawn);
			}

			private void AChangePersonality()
			{
				CompatibilityTool.OpenPersonalitiesDialog(API.Pawn);
			}

			private void AChangeFaction()
			{
				WindowTool.Open(new DialogChangeFaction());
			}

			private void AChangeIdeo()
			{
				Type aType = Reflect.GetAType("RimWorld", "IdeoUIUtility");
				aType.SetMemberValue("showAll", true);
				aType.SetMemberValue("devEditMode", true);
				bool devMode = Prefs.DevMode;
				if (!Prefs.DevMode)
				{
					Prefs.DevMode = true;
				}
				API.Get<EditorUI>(EType.EditorUI).layer = WindowLayer.Dialog;
				if (InStartingScreen && API.Pawn.Ideo != null && API.Pawn.Ideo.Fluid)
				{
					MessageTool.Show("Do not click on back button or you will need to restart your game. This is not a bug from the Editor, but because of the fluid ideo.", MessageTypeDefOf.CautionInput);
				}
				WindowTool.Open(new Dialog_ConfigureIdeo(new List<Pawn> { API.Pawn }, AOpenIdeoConfig));
				IdeoUIUtility.SetSelected(API.Pawn.Ideo);
			}

			private void AOpenIdeoConfig()
			{
				if (selectedIdeo != null)
				{
					API.Pawn.ideo.SetIdeo(selectedIdeo);
				}
				Type aType = Reflect.GetAType("RimWorld", "IdeoUIUtility");
				try
				{
					aType.SetMemberValue("showAll", false);
					aType.SetMemberValue("devEditMode", false);
					Prefs.DevMode = rememberDevMode;
				}
				catch
				{
				}
				API.Get<EditorUI>(EType.EditorUI).layer = Layer;
				MeditationFocusTypeAvailabilityCache.ClearFor(API.Pawn);
			}

			private void AChangeFavColorUI()
			{
				WindowTool.Open(new DialogColorPicker(ColorType.FavColor));
			}

			private void DrawTraits(int x, int y, int h)
			{
				try
				{
					if (API.Pawn.story != null)
					{
						Text.Font = GameFont.Medium;
						Widgets.Label(new Rect(x, y, 200f, 30f), "Traits".Translate());
						y += 3;
						SZWidgets.ButtonImage(x + 250, y, 24f, 24f, "UI/Buttons/Dev/Add", AAddTrait);
						SZWidgets.ButtonImageCol(x + 225, y, 24f, 24f, "bminus", AToggleRemoveTrait, bRemoveTrait ? Color.red : Color.white);
						SZWidgets.ButtonImage(x + 200, y, 18f, 24f, "UI/Buttons/Copy", ACopyTraits);
						if (!lCopyTraits.NullOrEmpty())
						{
							SZWidgets.ButtonImage(x + 180, y, 18f, 24f, "UI/Buttons/Paste", APasteTraits);
						}
						if (IsRandom)
						{
							SZWidgets.ButtonImage(x + 153, y, 25f, 25f, "brandom", ARandomTraits);
						}
						y += 25;
						if (API.Pawn.story.traits != null)
						{
							SZWidgets.TraitListView(x, y, 280f, h - y, API.Pawn.story.traits.allTraits, ref scrollTraits, 25, AOnTraitClick, ARandomTrait, APrevTrait, ANextTrait, FTraitLabel, FTraitTooltip);
						}
					}
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
			}

			private void AOnTraitClick(Trait val)
			{
				if (bRemoveTrait)
				{
					API.Pawn.RemoveTrait(val);
				}
				else
				{
					WindowTool.Open(new DialogAddTrait(val));
				}
			}

			private void ARandomTrait(Trait val)
			{
				List<KeyValuePair<TraitDef, TraitDegreeData>> list = TraitTool.ListOfTraitsKeyValuePair(null);
				int index = list.IndexOf(new KeyValuePair<TraitDef, TraitDegreeData>(val.def, val.CurrentData));
				index = list.NextOrPrevIndex(index, next: true, random: true);
				KeyValuePair<TraitDef, TraitDegreeData> keyValuePair = list[index];
				API.Pawn.AddTrait(keyValuePair.Key, keyValuePair.Value, random: false, doChangeSkillValue: true, val);
			}

			private void ANextTrait(Trait val)
			{
				List<KeyValuePair<TraitDef, TraitDegreeData>> list = TraitTool.ListOfTraitsKeyValuePair(null);
				int index = list.IndexOf(new KeyValuePair<TraitDef, TraitDegreeData>(val.def, val.CurrentData));
				index = list.NextOrPrevIndex(index, next: true, random: false);
				KeyValuePair<TraitDef, TraitDegreeData> keyValuePair = list[index];
				API.Pawn.AddTrait(keyValuePair.Key, keyValuePair.Value, random: false, doChangeSkillValue: true, val);
			}

			private void APrevTrait(Trait val)
			{
				List<KeyValuePair<TraitDef, TraitDegreeData>> list = TraitTool.ListOfTraitsKeyValuePair(null);
				int index = list.IndexOf(new KeyValuePair<TraitDef, TraitDegreeData>(val.def, val.CurrentData));
				index = list.NextOrPrevIndex(index, next: false, random: false);
				KeyValuePair<TraitDef, TraitDegreeData> keyValuePair = list[index];
				API.Pawn.AddTrait(keyValuePair.Key, keyValuePair.Value, random: false, doChangeSkillValue: true, val);
			}

			private void ARandomTraits()
			{
				API.Pawn.story.traits.allTraits.Clear();
				int num = zufallswert.Next(0, 11);
				for (int i = 0; i < num; i++)
				{
					API.Pawn.AddTrait(null, null, random: true, doChangeSkillValue: true);
				}
			}

			private void AAddTrait()
			{
				WindowTool.Open(new DialogAddTrait());
			}

			private void AToggleRemoveTrait(Color col)
			{
				bRemoveTrait = !bRemoveTrait;
			}

			private void ACopyTraits()
			{
				lCopyTraits = new List<Trait>();
				foreach (Trait allTrait in API.Pawn.story.traits.allTraits)
				{
					lCopyTraits.Add(allTrait);
				}
			}

			private void APasteTraits()
			{
				if (lCopyTraits.NullOrEmpty())
				{
					return;
				}
				foreach (Trait lCopyTrait in lCopyTraits)
				{
					API.Pawn.story.traits.allTraits.Add(new Trait(lCopyTrait.def, lCopyTrait.Degree));
				}
				MeditationFocusTypeAvailabilityCache.ClearFor(API.Pawn);
			}

			private void DrawSkills(int x, int y)
			{
				try
				{
					if (API.Pawn.skills == null)
					{
						return;
					}
					Text.Font = GameFont.Medium;
					Widgets.Label(new Rect(x, y, 200f, 30f), "Skills".Translate());
					y += 35;
					BackstoryDef backstory = API.Pawn.story.GetBackstory(BackstorySlot.Adulthood);
					BackstoryDef backstory2 = API.Pawn.story.GetBackstory(BackstorySlot.Childhood);
					float num = 0f;
					float num2 = (float)Reflect.GetAType("RimWorld", "SkillUI").GetMemberValue("levelLabelWidth");
					float num3 = (234f - num2 - 35f) / 20f;
					foreach (SkillRecord skill in API.Pawn.skills.skills)
					{
						int num4 = (int)skill.GetMemberConstValue("MaxLevel");
						num3 = (234f - num2 - 35f) / (float)num4;
						int num5 = TryGetSkillValueForSkill(backstory, skill.def);
						int num6 = TryGetSkillValueForSkill(backstory2, skill.def);
						int num7 = ((backstory != null) ? num5 : 0);
						int num8 = ((backstory2 != null) ? num6 : 0);
						int traitOffsetForSkill = API.Pawn.GetTraitOffsetForSkill(skill.def);
						int aptitude = skill.Aptitude;
						int num9 = num7 + num8 + traitOffsetForSkill + aptitude;
						num = y + skill.def.GetSkillIndex() * 27;
						SZWidgets.LabelBackground(new Rect((float)x + num2 + 35f, num, num3 * (float)num9, 24f), "", (num9 < 0) ? new Color(0.5f, 0f, 0f, 0.25f) : new Color(0f, 0.5f, 0f, 0.15f));
					}
					try
					{
						SkillUI.DrawSkillsOf(API.Pawn, new Vector2(x, y), SkillUI.SkillDrawMode.Menu, new Rect(x, y, 200f, 30f));
					}
					catch (Exception e)
					{
						MessageTool.DebugException(e);
					}
					num = 0f;
					foreach (SkillRecord skill2 in API.Pawn.skills.skills)
					{
						num = y + skill2.def.GetSkillIndex() * 27;
						SZWidgets.ButtonInvisibleVar(new Rect(x, num, num2 + 35f, 24f), ATogglePassion, skill2);
						if (iTickInputSkill > 0)
						{
							skill2.levelInt = SZWidgets.NumericTextField((float)x + num2 + 18f, num, 40f, 24f, skill2.levelInt, 0, 9999);
							iTickInputSkill--;
						}
						else
						{
							SZWidgets.ButtonInvisibleVar(new Rect((float)x + num2 + 40f, num, (float)(x + 180) - ((float)x + num2 + 40f), 24f), ASetSkillNumeric, skill2);
						}
						SZWidgets.ButtonImageVar(x + 210, num, 24f, 24f, "UI/Buttons/Dev/Add", AAddSkillLevel, skill2);
						SZWidgets.ButtonImageVar(x + 185, num, 24f, 24f, "bminus", ASubSkillLevel, skill2);
						TooltipHandler.TipRegion(new Rect(x, num, num2 + 80f, 24f), API.Pawn.GetTooltipForSkillpoints(skill2));
					}
					SZWidgets.ButtonImage(x + 210, y - 32, 18f, 24f, "UI/Buttons/Copy", ACopySkills);
					if (!lOfCopySkills.NullOrEmpty())
					{
						SZWidgets.ButtonImage(x + 190, y - 32, 18f, 24f, "UI/Buttons/Paste", APasteSkills);
					}
					if (IsRandom)
					{
						SZWidgets.ButtonImage(x + 163, y - 32, 25f, 25f, "brandom", ARandomSkills);
					}
				}
				catch (Exception e2)
				{
					MessageTool.DebugException(e2);
				}
			}

			private void ACopySkills()
			{
				lOfCopySkills = new List<SkillRecord>();
				foreach (SkillRecord skill in API.Pawn.skills.skills)
				{
					lOfCopySkills.Add(skill);
				}
			}

			private void APasteSkills()
			{
				API.Pawn.PasteSkills(lOfCopySkills);
			}

			private void ARandomSkills()
			{
				foreach (SkillRecord skill in API.Pawn.skills.skills)
				{
					if (!skill.TotallyDisabled)
					{
						int maxValue = zufallswert.Next(0, 21);
						skill.Level = zufallswert.Next(0, maxValue);
						skill.passion = (Passion)zufallswert.Next(0, 3);
					}
				}
			}

			private void ATogglePassion(SkillRecord record)
			{
				if (record.passion == Passion.None)
				{
					record.passion = Passion.Minor;
				}
				else if (record.passion == Passion.Minor)
				{
					record.passion = Passion.Major;
				}
				else if (record.passion == Passion.Major)
				{
					record.passion = Passion.None;
				}
			}

			private void ASetSkillNumeric(SkillRecord record)
			{
				iTickInputSkill = 9000;
			}

			private void AAddSkillLevel(SkillRecord record)
			{
				record.levelInt++;
			}

			private void ASubSkillLevel(SkillRecord record)
			{
				record.levelInt--;
			}

			private void DrawAbilities(int x, int y)
			{
				try
				{
					if (API.Pawn.abilities != null)
					{
						Text.Font = GameFont.Medium;
						Widgets.Label(new Rect(x, y - 2, 200f, 30f), Label.PSYTALENTE);
						SZWidgets.ButtonImage(x + 210, y, 24f, 24f, "UI/Buttons/Dev/Add", AAddAbility);
						SZWidgets.ButtonImageCol(x + 185, y, 24f, 24f, "bminus", AToggleRemoveAbility, bRemoveAbility ? Color.red : Color.white);
						SZWidgets.ButtonImage(x + 165, y, 18f, 24f, "UI/Buttons/Copy", ACopyAbilities);
						if (!lCopyAbilities.NullOrEmpty())
						{
							SZWidgets.ButtonImage(x + 145, y, 18f, 24f, "UI/Buttons/Paste", APasteAbilities);
						}
						if (IsRandom)
						{
							SZWidgets.ButtonImage(x + 117, y, 25f, 25f, "brandom", ARandomAbilities);
						}
						else if (!InStartingScreen && API.Pawn.HasPsylink)
						{
							SZWidgets.ButtonImage(x + 117, y, 25f, 25f, "UI/Buttons/DragHash", ATogglePsyvalues);
						}
						y += 30;
						Text.Font = GameFont.Small;
						GUI.color = Color.white;
						List<Ability> abilities = API.Pawn.abilities.abilities;
						SZContainers.DrawElementStack(new Rect(x, y, 500f, 50f), abilities, bRemoveAbility, delegate(Ability abil)
						{
							API.Pawn.abilities.RemoveAbility(abil.def);
						}, (Ability abil) => abil.def);
					}
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
			}

			private void ATogglePsyvalues()
			{
				bShowPsyValues = !bShowPsyValues;
			}

			private void ARandomAbilities()
			{
				API.Pawn.abilities.abilities.Clear();
				int num = zufallswert.Next(0, 11);
				List<AbilityDef> source = DefTool.ListByMod<AbilityDef>(null).ToList();
				for (int i = 0; i < num; i++)
				{
					API.Pawn.abilities.GainAbility(source.RandomElement());
				}
				API.Pawn.abilities.Notify_TemporaryAbilitiesChanged();
			}

			private void AShowXenoType()
			{
				WindowTool.Open(new DialogViewXenoGenes(API.Pawn));
			}

			private void AConfigXenoType()
			{
				WindowTool.Open(new DialogXenoType(API.Pawn));
			}

			private void AAddAbility()
			{
				WindowTool.Open(new DialogAddAbility());
			}

			private void AToggleRemoveAbility(Color col)
			{
				bRemoveAbility = !bRemoveAbility;
			}

			private void ACopyAbilities()
			{
				lCopyAbilities = new List<Ability>();
				foreach (Ability ability in API.Pawn.abilities.abilities)
				{
					lCopyAbilities.Add(ability);
				}
			}

			private void APasteAbilities()
			{
				if (lCopyAbilities.NullOrEmpty())
				{
					return;
				}
				foreach (Ability lCopyAbility in lCopyAbilities)
				{
					API.Pawn.abilities.GainAbility(lCopyAbility.def);
				}
			}

			private void DrawPsycasts(int x, int y)
			{
				if (bShowPsyValues && API.Pawn.psychicEntropy != null && API.Pawn.psychicEntropy.Psylink != null)
				{
					float value = API.Pawn.psychicEntropy.EntropyValue;
					float value2 = API.Pawn.psychicEntropy.CurrentPsyfocus;
					float height = 100f;
					Rect outRect = new Rect(x, y, 250f, 100f);
					Rect rect = new Rect(0f, 0f, outRect.width - 16f, height);
					Widgets.BeginScrollView(outRect, ref scrollPosParam, rect);
					Rect rect2 = rect.ContractedBy(4f);
					rect2.y -= 4f;
					rect2.height = height;
					Listing_X listing_X = new Listing_X();
					listing_X.Begin(rect2);
					listing_X.verticalSpacing = 30f;
					listing_X.AddSection(Label.ENTROPY, "", ref selectedParamName, ref value, 0f, (float)Math.Round(API.Pawn.psychicEntropy.MaxEntropy), small: true);
					listing_X.AddSection(Label.PSYFOCUS, "%", ref selectedParamName, ref value2, 0f, 1f, small: true);
					listing_X.End();
					Widgets.EndScrollView();
					if (value != API.Pawn.psychicEntropy.EntropyValue)
					{
						API.Pawn.SetEntropy(value);
					}
					if (value2 != API.Pawn.psychicEntropy.CurrentPsyfocus)
					{
						API.Pawn.SetPsyfocus(value2);
					}
				}
			}

			private void DrawTraining(int x, int y, int w, int h)
			{
				try
				{
					if (!API.Pawn.RaceProps.Animal || API.Pawn.Faction == null || API.Pawn.training == null)
					{
						return;
					}
					GUI.color = Color.white;
					if (API.Pawn.training.HasLearned(TrainableDefOf.Obedience))
					{
						Widgets.Label(new Rect(x, y, 100f, h), "Master".Translate() + ":");
						SZWidgets.FloatMenuOnButtonText(new Rect(x + 80, y - 5, 120f, 30f), selectedTrainer.GetPawnName(), lOfColonists, (Pawn p) => p.GetPawnName(), ASelectMaster);
					}
					y += 30;
					Widgets.Label(new Rect(x, y, 300f, 30f), API.Pawn.GetTrainabilityLabel());
					y += 30;
					y += 30;
					foreach (TrainableDef allDef in DefDatabase<TrainableDef>.AllDefs)
					{
						bool wanted = API.Pawn.training.GetWanted(allDef);
						bool checkOn = wanted;
						Rect rect = new Rect(x, y, 150f, 30f);
						Widgets.DrawHighlightIfMouseover(rect);
						AcceptanceReport canTrain = API.Pawn.training.CanAssignToTrain(allDef);
						DoTrainableTooltip(rect, API.Pawn, allDef, canTrain);
						GUI.color = (canTrain.Accepted ? Color.white : Color.gray);
						Widgets.CheckboxLabeled(rect, allDef.label, ref checkOn);
						if (checkOn != wanted)
						{
							API.Pawn.training.SetWantedRecursive(allDef, checkOn);
						}
						int trainingSteps = GetTrainingSteps(allDef);
						Widgets.Label(new Rect(x + 160, y, 30f, 30f), trainingSteps + " / " + allDef.steps);
						if (trainingSteps < allDef.steps)
						{
							SZWidgets.ButtonImageVar(x + 200, y, 24f, 24f, "UI/Buttons/Dev/Add", ATrain, allDef);
						}
						y += 30;
					}
				}
				catch (Exception e)
				{
					MessageTool.DebugException(e);
				}
			}

			private void DoTrainableTooltip(Rect rect, Pawn pawn, TrainableDef td, AcceptanceReport canTrain)
			{
				TooltipHandler.TipRegion(rect, delegate
				{
					string text = td.LabelCap + "\n\n" + td.description;
					if (!canTrain.Accepted)
					{
						text = text + "\n\n" + canTrain.Reason;
					}
					else if (!td.prerequisites.NullOrEmpty())
					{
						text += "\n";
						for (int i = 0; i < td.prerequisites.Count; i++)
						{
							if (!pawn.training.HasLearned(td.prerequisites[i]))
							{
								text = text + "\n" + "TrainingNeedsPrerequisite".Translate().Formatted(td.prerequisites[i].LabelCap);
							}
						}
					}
					return text;
				}, (int)(rect.y * 612f + rect.x));
			}

			private int GetTrainingSteps(TrainableDef td)
			{
				return (int)API.Pawn.training.CallMethod("GetSteps", new object[1] { td });
			}

			private void ASelectMaster(Pawn p)
			{
				selectedTrainer = p;
				API.Pawn.playerSettings.Master = p;
			}

			private void ATrain(TrainableDef trainableDef)
			{
				API.Pawn.training.Train(trainableDef, null);
			}

			private void DrawXeno(int x, int y, int w)
			{
				if (API.Pawn.HasGeneTracker() && ModsConfig.BiotechActive)
				{
					SZWidgets.ButtonTextureTextHighlight(new Rect(x, y, 30f, 30f), "", API.Pawn.genes.XenotypeIcon, Color.white, AShowXenoType, "ViewGenes".Translate());
					x = x + 30 + 4;
					SZWidgets.ButtonTextureTextHighlight2(new Rect(x, y, w, 30f), API.Pawn.genes.XenotypeLabelCap, null, Color.white, AConfigXenoType, "XenotypeEditor".Translate() + "\n\n" + API.Pawn.genes.XenotypeDescShort);
				}
			}

			private void DrawCapsule(int x, int y, int w, int h)
			{
				if (InStartingScreen)
				{
					SZWidgets.ButtonImage(x, y, w, h, "bcapsule", ACapsuleUI);
				}
			}

			private void ACapsuleUI()
			{
				WindowTool.Open(new DialogCapsuleUI());
			}
		}

		private class BlockHealth
		{
			private static readonly Color StaticHighlightColor = new Color(0.75f, 0.75f, 0.85f, 1f);

			private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

			private static readonly Color backgr = new Color(0.0823f, 0.098f, 0.113f);

			private Vector2 scrollPos;

			private float scrollH;

			private bool bShowHidden;

			private bool bHighlight;

			private bool bShowRemove;

			private List<Hediff> lCopyHealth;

			private Texture2D texBleeding;

			private float lastMaxIconsTotalWidth = 20f;

			private bool ShowHidden
			{
				get
				{
					return (bool)typeof(HealthCardUtility).GetMemberValue("showAllHediffs");
				}
				set
				{
					typeof(HealthCardUtility).SetMemberValue("showAllHediffs", value);
					bShowHidden = value;
				}
			}

			internal BlockHealth()
			{
				scrollH = 0f;
				bShowHidden = false;
				bHighlight = true;
				bShowRemove = false;
				lCopyHealth = new List<Hediff>();
				texBleeding = ContentFinder<Texture2D>.Get("UI/Icons/Medical/Bleeding", reportFailure: false);
			}

			internal void Draw(coord c)
			{
				int x = c.x;
				int y = c.y;
				int w = c.w;
				int h = c.h;
				if (API.Pawn != null)
				{
					DrawHealth(new Rect(x + 5, y + 25, w, h - 60));
					DrawUpper(x, y, w, h);
					DrawLower(x + w, y + h - 30, w, h);
				}
			}

			private void DrawHealth(Rect outRect)
			{
				outRect = outRect.Rounded();
				Rect rect = new Rect(outRect.x, outRect.y, outRect.width * 0.375f, outRect.height).Rounded();
				Rect rect2 = new Rect(rect.xMax, outRect.y, outRect.width - rect.width, outRect.height);
				rect.yMin += 11f;
				try
				{
					HealthCardUtility.DrawHealthSummary(rect, API.Pawn, allowOperations: false, API.Pawn);
				}
				catch
				{
				}
				DrawHediffListing(rect2.ContractedBy(10f), API.Pawn, showBloodLoss: true);
			}

			private void DrawHediffListing(Rect rect, Pawn pawn, bool showBloodLoss)
			{
				//IL_016d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0173: Invalid comparison between Unknown and I4
				GUI.color = Color.white;
				GUI.BeginGroup(rect);
				float lineHeight = Text.LineHeight;
				Rect outRect = new Rect(0f, 0f, rect.width, rect.height - lineHeight);
				Rect viewRect = new Rect(0f, 0f, rect.width - 16f, scrollH);
				Rect rect2 = rect;
				if (viewRect.height > outRect.height)
				{
					rect2.width -= 16f;
				}
				Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
				try
				{
					GUI.color = Color.white;
					float curY = 0f;
					bHighlight = true;
					bool flag = false;
					foreach (IGrouping<BodyPartRecord, Hediff> item in VisibleHediffGroupsInOrder(pawn, showBloodLoss))
					{
						try
						{
							flag = true;
							DrawHediffRow(rect2, pawn, item, ref curY);
						}
						catch
						{
						}
					}
					if (!flag)
					{
						Widgets.NoneLabelCenteredVertically(new Rect(0f, 0f, viewRect.width, outRect.height), "(" + "NoHealthConditions".Translate() + ")");
						curY = outRect.height - 1f;
					}
					if ((int)Event.current.type == 8)
					{
						scrollH = curY;
					}
				}
				catch
				{
					MessageTool.Show("health conditions are malformed -> consider to use 'full heal'");
				}
				Widgets.EndScrollView();
				try
				{
					float bleedRateTotal = pawn.health.hediffSet.BleedRateTotal;
					if (bleedRateTotal > 0.01f)
					{
						Rect rect3 = new Rect(0f, rect.height - lineHeight, rect.width, lineHeight);
						string text = string.Concat("BleedingRate".Translate(), ": ", bleedRateTotal.ToStringPercent(), " / ", "Day".Translate());
						int num = HealthUtility.TicksUntilDeathDueToBloodLoss(pawn);
						string text2 = "TimeToDeath".Translate().Formatted(num.ToStringTicksToPeriod());
						text = ((num >= 60000) ? ((string)(text + " (" + "WontBleedOutSoon".Translate() + ")")) : (text + " (" + text2 + ")"));
						Widgets.Label(rect3, text);
					}
					GUI.EndGroup();
				}
				catch
				{
					pawn.health.hediffSet.Clear();
				}
				GUI.color = Color.white;
			}

			private IEnumerable<IGrouping<BodyPartRecord, Hediff>> VisibleHediffGroupsInOrder(Pawn pawn, bool showBloodLoss)
			{
				foreach (IGrouping<BodyPartRecord, Hediff> item in from x in VisibleHediffs(pawn, showBloodLoss)
					group x by x.Part into x
					orderby GetListPriority(x.First().Part) descending
					select x)
				{
					yield return item;
				}
			}

			private IEnumerable<Hediff> VisibleHediffs(Pawn pawn, bool showBloodLoss)
			{
				if (!bShowHidden)
				{
					List<Hediff_MissingPart> mpca = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
					for (int i = 0; i < mpca.Count; i++)
					{
						yield return mpca[i];
					}
					IEnumerable<Hediff> visibleDiffs = pawn.health.hediffSet.hediffs.Where((Hediff d) => !(d is Hediff_MissingPart) && d.Visible && (showBloodLoss || d.def != HediffDefOf.BloodLoss));
					foreach (Hediff item in visibleDiffs)
					{
						yield return item;
					}
					yield break;
				}
				foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
				{
					yield return hediff;
				}
			}

			private float GetListPriority(BodyPartRecord rec)
			{
				if (rec == null)
				{
					return 9999999f;
				}
				return (float)((int)rec.height * 10000) + rec.coverageAbsWithChildren;
			}

			private void DrawHediffRow(Rect rect, Pawn pawn, IEnumerable<Hediff> diffs, ref float curY)
			{
				float num = rect.width * 0.375f;
				float width = rect.width - num - lastMaxIconsTotalWidth;
				BodyPartRecord part = diffs.First().Part;
				float a = ((part != null) ? Text.CalcHeight(part.LabelCap, num) : Text.CalcHeight("WholeBody".Translate(), num));
				float num2 = 0f;
				float num3 = curY;
				float num4 = 0f;
				foreach (IGrouping<int, Hediff> item in from x in diffs
					group x by x.UIGroupKey)
				{
					int num5 = item.Count();
					string text = item.First().LabelCap;
					if (num5 != 1)
					{
						text = text + " x" + num5;
					}
					num4 += Text.CalcHeight(text, width);
				}
				num2 = num4;
				Rect rect2 = new Rect(0f, curY, rect.width, Mathf.Max(a, num2));
				DoRightRowHighlight(rect2);
				if (part != null)
				{
					GUI.color = HealthUtility.GetPartConditionLabel(pawn, part).Second;
					Widgets.Label(new Rect(0f, curY, num, 100f), part.LabelCap);
				}
				else
				{
					GUI.color = HealthUtility.RedColor;
					Widgets.Label(new Rect(0f, curY, num, 100f), "WholeBody".Translate());
				}
				GUI.color = Color.white;
				foreach (IGrouping<int, Hediff> item2 in from x in diffs
					group x by x.UIGroupKey)
				{
					int num6 = 0;
					Hediff hediff = null;
					Texture2D bleedingIcon = null;
					TextureAndColor stateIcon = null;
					float totalBleedRate = 0f;
					foreach (Hediff item3 in item2)
					{
						if (num6 == 0)
						{
							hediff = item3;
						}
						stateIcon = item3.StateIcon;
						if (item3.Bleeding)
						{
							bleedingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Medical/Bleeding");
						}
						totalBleedRate += item3.BleedRate;
						num6++;
					}
					string text2 = hediff.LabelCap;
					if (num6 != 1)
					{
						text2 = text2 + " x" + num6.ToStringCached();
					}
					float num7 = Text.CalcHeight(text2, width);
					Rect rect3 = new Rect(num, curY, width, num7);
					Rect rect4 = new Rect(rect3.x, rect3.y, rect3.width - 30f, rect3.height);
					Widgets.DrawHighlightIfMouseover(rect4);
					GUI.color = hediff.LabelColor;
					Widgets.Label(rect3, text2);
					SZWidgets.ButtonInvisibleVar(rect4, AEditHediff, hediff);
					GUI.color = Color.white;
					Rect iconsRect = new Rect(rect3.x + 10f, rect3.y, (float)((double)rect.width - (double)num - 10.0), rect3.height);
					if (bShowRemove)
					{
						SZWidgets.ButtonImageVar(rect3.x + rect3.width - 30f, rect3.y, 24f, 24f, "UI/Buttons/Delete", BRemoveHediff, hediff);
					}
					List<GenUI.AnonymousStackElement> list = new List<GenUI.AnonymousStackElement>();
					Hediff localHediff = hediff;
					list.Add(new GenUI.AnonymousStackElement
					{
						drawer = delegate(Rect r)
						{
							r = new Rect(iconsRect.x + (float)((double)iconsRect.width - ((double)r.x - (double)iconsRect.x) - 20.0), r.y, 20f, 20f);
							Widgets.InfoCardButton(r, localHediff.def);
						},
						width = 20f
					});
					if ((bool)bleedingIcon)
					{
						list.Add(new GenUI.AnonymousStackElement
						{
							drawer = delegate(Rect r)
							{
								r = new Rect(iconsRect.x + (float)((double)iconsRect.width - ((double)r.x - (double)iconsRect.x) - 20.0), r.y, 20f, 20f);
								GUI.DrawTexture(r.ContractedBy(GenMath.LerpDouble(0f, 0.6f, 5f, 0f, Mathf.Min(totalBleedRate, 1f))), (Texture)bleedingIcon);
							},
							width = 20f
						});
					}
					if (stateIcon.HasValue)
					{
						list.Add(new GenUI.AnonymousStackElement
						{
							drawer = delegate(Rect r)
							{
								r = new Rect(iconsRect.x + (float)((double)iconsRect.width - ((double)r.x - (double)iconsRect.x) - 20.0), r.y, 20f, 20f);
								GUI.color = stateIcon.Color;
								GUI.DrawTexture(r, (Texture)stateIcon.Texture);
								GUI.color = Color.white;
							},
							width = 20f
						});
					}
					GenUI.DrawElementStack(iconsRect, num7, list, delegate(Rect r, GenUI.AnonymousStackElement obj)
					{
						obj.drawer(r);
					}, (GenUI.AnonymousStackElement obj) => obj.width);
					lastMaxIconsTotalWidth = Mathf.Max(lastMaxIconsTotalWidth, list.Sum((GenUI.AnonymousStackElement x) => x.width + 5f) - 5f);
					if (Mouse.IsOver(rect3))
					{
						int num8 = 0;
						foreach (Hediff item4 in item2)
						{
							Hediff hediff2 = item4;
							TooltipHandler.TipRegion(rect3, new TipSignal(() => hediff2.GetTooltip(pawn, Prefs.DevMode), (int)curY + 7857 + num8++, TooltipPriority.Default));
						}
						if (part != null)
						{
							string text3 = (string)typeof(HealthCardUtility).CallMethod("GetTooltip", new object[2] { pawn, part });
							TooltipHandler.TipRegion(rect3, text3);
						}
					}
					if (Widgets.ButtonInvisible(rect3, doMouseoverSound: false))
					{
						typeof(HealthCardUtility).CallMethod("EntryClicked", new object[2] { diffs, pawn });
					}
					curY += num7;
				}
				GUI.color = Color.white;
				curY = num3 + Mathf.Max(a, num2);
				OnClickOrHover(rect2, diffs, pawn, part);
			}

			private void OnClickOrHover(Rect rect, IEnumerable<Hediff> diffs, Pawn pawn, BodyPartRecord part)
			{
				if (Widgets.ButtonInvisible(rect, doMouseoverSound: false))
				{
					typeof(HealthCardUtility).CallMethod("EntryClicked", new object[2] { diffs, pawn });
				}
				string text = (string)typeof(HealthCardUtility).CallMethod("GetTooltip", new object[2] { pawn, part });
				TooltipHandler.TipRegion(rect, text);
			}

			private void DoRightRowHighlight(Rect rowRect)
			{
				if (bHighlight)
				{
					GUI.color = StaticHighlightColor;
					GUI.DrawTexture(rowRect, (Texture)TexUI.HighlightTex);
				}
				bHighlight = !bHighlight;
			}

			private void DrawUpper(int x, int y, int w, int h)
			{
				y += 5;
				Widgets.DrawBoxSolid(new Rect(x + 5, y, 850f, 31f), backgr);
				Widgets.DrawBoxSolid(new Rect(x + 5, y + 31, 250f, 1f), Color.gray);
				SZWidgets.CheckBoxOnChange(new Rect(x + 176, y, 135f, 25f), Label.SHOW_HIDDEN, bShowHidden, AHiddenChanged);
				SZWidgets.ButtonImage(x + w - 30, y, 24f, 24f, "UI/Buttons/Dev/Add", AAddHediff);
				SZWidgets.ButtonImage(x + w - 55, y, 24f, 24f, "bminus", ARemoveHediff);
				SZWidgets.ButtonImage(x + w - 80, y, 18f, 24f, "UI/Buttons/Copy", ACopyHealth);
				if (!lCopyHealth.NullOrEmpty())
				{
					SZWidgets.ButtonImage(x + w - 100, y, 18f, 24f, "UI/Buttons/Paste", APasteHealth);
				}
				if (IsRandom)
				{
					SZWidgets.ButtonImage(x + w - 130, y, 25f, 25f, "brandom", ARandomHealth);
				}
			}

			private void BRemoveHediff(Hediff hediff)
			{
				API.Pawn.RemoveHediff(hediff);
				API.UpdateGraphics();
			}

			private void ARandomHealth()
			{
				int num = zufallswert.Next(1, 11);
				for (int i = 0; i < num; i++)
				{
					API.Pawn.AddHediff2(random: true);
				}
			}

			private void AHiddenChanged(bool val)
			{
				ShowHidden = val;
			}

			private void AEditHediff(Hediff val)
			{
				WindowTool.Open(new DialogAddHediff(val));
			}

			private void AAddHediff()
			{
				WindowTool.Open(new DialogAddHediff());
			}

			private void ARemoveHediff()
			{
				bShowRemove = !bShowRemove;
			}

			private void ACopyHealth()
			{
				lCopyHealth = new List<Hediff>();
				foreach (Hediff hediff in API.Pawn.health.hediffSet.hediffs)
				{
					lCopyHealth.Add(hediff);
				}
			}

			private void APasteHealth()
			{
				if (lCopyHealth.NullOrEmpty())
				{
					return;
				}
				foreach (Hediff item in lCopyHealth)
				{
					BodyPartRecord bodyPart = HealthTool.GetBodyPart(API.Pawn, item);
					if ((item.Part != null && bodyPart != null) || (item.Part == null && bodyPart == null))
					{
						API.Pawn.AddHediff2(random: false, item.def, item.Severity, bodyPart, item.IsPermanent(), item.GetLevel(), item.GetPainValue(), item.GetDuration(), item.GetOtherPawn());
					}
				}
				API.UpdateGraphics();
			}

			private void DrawLower(int x, int y, int w, int h)
			{
				if (API.Pawn.Dead)
				{
					SZWidgets.ButtonText(x - 130, y, 130f, 30f, Label.RESURRECT, ARessurect);
					return;
				}
				SZWidgets.ButtonText(x - 130, y, 130f, 30f, Label.FULLHEAL, AFullHeal, Label.TIP_HEAL);
				SZWidgets.ButtonText(x - 260, y, 130f, 30f, Label.MEDICATE, AMedicate);
				SZWidgets.ButtonText(x - 390, y, 130f, 30f, Label.ANAESTHETIZE, AAnaesthetize);
				SZWidgets.ButtonText(x - 520, y, 130f, 30f, Label.HURT, AHurt, Label.TIP_HURT);
			}

			private void ARessurect()
			{
				ResurrectionUtility.TryResurrect(API.Pawn);
				if (!API.Pawn.Spawned)
				{
					API.Pawn.SpawnPawn(null, API.Pawn.Position);
				}
				API.UpdateGraphics();
			}

			private void AFullHeal()
			{
				if (Event.current.alt)
				{
					HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(API.Pawn);
				}
				else
				{
					WindowTool.Open(new DialogFullheal());
				}
				API.UpdateGraphics();
			}

			private void AMedicate()
			{
				API.Pawn.Medicate();
				API.UpdateGraphics();
			}

			private void AAnaesthetize()
			{
				API.Pawn.Anaesthetize();
				API.UpdateGraphics();
			}

			private void AHurt()
			{
				if (Event.current.alt || InStartingScreen)
				{
					API.Pawn.Hurt();
				}
				else
				{
					API.Pawn.DamageUntilDeath();
				}
				API.UpdateGraphics();
			}
		}

		private class BlockInfo
		{
			private string uniqueID;

			private List<Faction> lfe;

			internal BlockInfo()
			{
				uniqueID = null;
				lfe = new List<Faction>();
			}

			internal void Draw(coord c)
			{
				int x = c.x;
				int y = c.y;
				int w = c.w;
				int h = c.h;
				if (API.Pawn == null)
				{
					return;
				}
				if (uniqueID != API.Pawn.ThingID)
				{
					uniqueID = API.Pawn.ThingID;
					lfe = API.Pawn.FactionEnemies();
					if (API.Pawn.Dead && (API.Pawn.Corpse == null || !API.Pawn.Corpse.Spawned || API.Pawn.Corpse.Discarded))
					{
						return;
					}
					StatsReportUtility.Notify_QuickSearchChanged();
				}
				try
				{
					if (API.Pawn.Dead)
					{
						StatsReportUtility.DrawStatsReport(new Rect(x + 10, y, w - 10, h - 30), API.Pawn.Corpse);
					}
					else
					{
						StatsReportUtility.DrawStatsReport(new Rect(x + 10, y, w - 10, h - 30), API.Pawn);
					}
				}
				catch
				{
				}
				DrawEnemies(x, y, w, h);
			}

			private void DrawEnemies(int x, int y, int w, int h)
			{
				if (API.Pawn == null)
				{
					return;
				}
				try
				{
					int num = lfe.CountAllowNull();
					int num2 = x + w;
					int num3 = x + w - 80 - lfe.Count * 22;
					num3 = ((num3 < x) ? x : num3);
					y = h;
					Widgets.Label(new Rect(num3, y, 80f, 30f), (string)"EnemyOf".Translate());
					SZWidgets.ButtonInvisible(new Rect(num3, y, w, 20f), AOpenFactionCard);
					num3 += 80;
					string text = "";
					foreach (Faction item in lfe)
					{
						Rect rect = new Rect(num3, y, 20f, 20f);
						SZWidgets.ButtonImageCol2(toolTip: (!Mouse.IsOver(rect)) ? "" : API.Pawn.GetFactionFullDesc(item), rect: rect, texPath: item.def.factionIconPath ?? "bwhite", action: ASetRelation, value: item, color: item.GetFacionColor());
						num3 += 22;
						if (num3 > num2)
						{
							break;
						}
					}
				}
				catch
				{
				}
			}

			private void ASetRelation(Faction f)
			{
			}

			private void AOpenFactionCard()
			{
			}
		}

		private class BlockInventory
		{
			private Vector2 scrollPos;

			private float fCount;

			private float scrollH;

			private const float ICONH = 32f;

			private const float BUTTONH = 28f;

			private List<Thing> workingInvList;

			private List<Apparel> lOfCopyOutfits;

			private List<Thing> lOfCopyItems;

			private List<ThingWithComps> lOfCopyWeapons;

			private Rect GetCopyRect(Rect rBase, float y)
			{
				return new Rect(rBase.x + rBase.width - 20f, rBase.y + y, 18f, 24f);
			}

			private Rect GetPasteRect(Rect rBase, float y)
			{
				return new Rect(rBase.x + rBase.width - 43f, rBase.y + y, 18f, 24f);
			}

			internal BlockInventory()
			{
				fCount = 0f;
				scrollH = 0f;
				workingInvList = new List<Thing>();
				lOfCopyOutfits = new List<Apparel>();
				lOfCopyItems = new List<Thing>();
				lOfCopyWeapons = new List<ThingWithComps>();
			}

			internal void Draw(coord c)
			{
				int x = c.x;
				int y = c.y;
				int w = c.w;
				int h = c.h;
				if (API.Pawn != null)
				{
					DrawInvData(new Rect(x + 10, y + 10, w - 10, h - 40));
					DrawLowerButtons(x + w, y + h - 30);
				}
			}

			private void DrawInventory(Rect rView)
			{
				//IL_0111: Unknown result type (might be due to invalid IL or missing references)
				//IL_0117: Invalid comparison between Unknown and I4
				fCount += 20f;
				SZWidgets.ButtonImage(GetCopyRect(rView, fCount), "UI/Buttons/Copy", ACopyInv);
				if (!lOfCopyItems.NullOrEmpty())
				{
					SZWidgets.ButtonImage(GetPasteRect(rView, fCount), "UI/Buttons/Paste", APasteInv);
				}
				Widgets.ListSeparator(ref fCount, rView.width, "Inventory".Translate());
				workingInvList.Clear();
				workingInvList.AddRange(API.Pawn.inventory.innerContainer);
				for (int i = 0; i < workingInvList.Count; i++)
				{
					DrawThingRow(ref fCount, rView.width, workingInvList[i], DialogType.Object);
				}
				workingInvList.Clear();
				if ((int)Event.current.type == 8)
				{
					scrollH = fCount + 30f;
				}
			}

			private void DrawApparel(Rect rView)
			{
				fCount += 20f;
				SZWidgets.ButtonImage(GetCopyRect(rView, fCount), "UI/Buttons/Copy", ACopyApparel);
				if (!lOfCopyOutfits.NullOrEmpty())
				{
					SZWidgets.ButtonImage(GetPasteRect(rView, fCount), "UI/Buttons/Paste", APasteApparel);
				}
				Widgets.ListSeparator(ref fCount, rView.width, "Apparel".Translate());
				Pawn_ApparelTracker apparel = API.Pawn.apparel;
				if (apparel == null || apparel.WornApparelCount <= 0)
				{
					return;
				}
				try
				{
					List<Apparel> list = API.Pawn.apparel.WornApparel.OrderByDescending((Apparel ap) => ap.def.apparel.bodyPartGroups[0].listOrder).ToList();
					for (int num = 0; num < list.Count; num++)
					{
						DrawThingRow(ref fCount, rView.width, list[num], DialogType.Apparel);
					}
				}
				catch
				{
					List<Apparel> list2 = API.Pawn.apparel.WornApparel.ToList();
					foreach (Apparel item in list2)
					{
						if (!API.Pawn.ApparelGraphicTest(item, showError: true, force: true))
						{
							API.Pawn.TransferToInventory(item);
						}
					}
					API.UpdateGraphics();
				}
			}

			private void ARandomThing(Thing t, DialogType type)
			{
				switch (type)
				{
				case DialogType.Apparel:
					API.Pawn.ReplaceAndWearRandomApparel(t as Apparel);
					break;
				case DialogType.Weapon:
					API.Pawn.Reequip(null, (!API.Pawn.IsPrimaryWeapon(t as ThingWithComps)) ? 1 : 0);
					break;
				default:
					API.Pawn.ReplaceItem(t);
					break;
				}
				API.UpdateGraphics();
			}

			private void DrawEquipment(Rect rView)
			{
				fCount += 20f;
				SZWidgets.ButtonImage(GetCopyRect(rView, fCount), "UI/Buttons/Copy", ACopyWeapon);
				if (!lOfCopyWeapons.NullOrEmpty())
				{
					SZWidgets.ButtonImage(GetPasteRect(rView, fCount), "UI/Buttons/Paste", APasteWeapon);
				}
				Widgets.ListSeparator(ref fCount, rView.width, "Equipment".Translate());
				Pawn pawn = API.Pawn;
				if (pawn != null && pawn.equipment?.AllEquipmentListForReading.NullOrEmpty() == false)
				{
					List<ThingWithComps> allEquipmentListForReading = API.Pawn.equipment.AllEquipmentListForReading;
					for (int i = 0; i < allEquipmentListForReading.Count; i++)
					{
						DrawThingRow(ref fCount, rView.width, API.Pawn.equipment.AllEquipmentListForReading[i], DialogType.Weapon);
					}
				}
			}

			private void ARandomWeapon(int i)
			{
				API.Pawn.Reequip(null, i);
			}

			private void DrawArmorRating(Rect rView)
			{
				fCount += 20f;
				SZWidgets.ButtonImage(GetCopyRect(rView, fCount), "UI/Buttons/Copy", ACopyAll);
				if (!lOfCopyOutfits.NullOrEmpty())
				{
					SZWidgets.ButtonImage(GetPasteRect(rView, fCount), "UI/Buttons/Paste", APasteAll);
				}
				Widgets.ListSeparator(ref fCount, rView.width, "OverallArmor".Translate());
				TryDrawOverallArmor(ref fCount, rView.width, StatDefOf.ArmorRating_Sharp, "ArmorSharp".Translate());
				TryDrawOverallArmor(ref fCount, rView.width, StatDefOf.ArmorRating_Blunt, "ArmorBlunt".Translate());
				TryDrawOverallArmor(ref fCount, rView.width, StatDefOf.ArmorRating_Heat, "ArmorHeat".Translate());
			}

			private void DrawCarryAndComfy(Rect rView)
			{
				fCount = 0f;
				TryDrawMassInfo(ref fCount, rView.width);
				TryDrawComfyTemperatureRange(ref fCount, rView.width);
			}

			private void DrawInvData(Rect rect)
			{
				GUI.BeginGroup(rect);
				Text.Font = GameFont.Small;
				GUI.color = Color.white;
				Rect outRect = new Rect(0f, 0f, rect.width, rect.height);
				Rect rect2 = new Rect(0f, 0f, rect.width - 16f, scrollH);
				Widgets.BeginScrollView(outRect, ref scrollPos, rect2);
				DrawCarryAndComfy(rect2);
				DrawArmorRating(rect2);
				DrawEquipment(rect2);
				DrawApparel(rect2);
				DrawInventory(rect2);
				Widgets.EndScrollView();
				GUI.EndGroup();
				Text.Anchor = TextAnchor.UpperLeft;
			}

			private void ACopyAll()
			{
				lOfCopyOutfits = API.Pawn.ListOfCopyOutfits();
				lOfCopyItems = API.Pawn.ListOfCopyItems();
				lOfCopyWeapons = API.Pawn.ListOfCopyWeapons();
			}

			private void APasteAll()
			{
				API.Pawn.PasteCopyOutfits(lOfCopyOutfits);
				API.Pawn.PasteCopyItems(lOfCopyItems);
				API.Pawn.PasteCopyWeapons(lOfCopyWeapons);
			}

			private void ACopyWeapon()
			{
				lOfCopyWeapons = API.Pawn.ListOfCopyWeapons();
			}

			private void APasteWeapon()
			{
				API.Pawn.PasteCopyWeapons(lOfCopyWeapons);
			}

			private void ACopyApparel()
			{
				lOfCopyOutfits = API.Pawn.ListOfCopyOutfits();
			}

			private void APasteApparel()
			{
				API.Pawn.PasteCopyOutfits(lOfCopyOutfits);
			}

			private void ACopyInv()
			{
				lOfCopyItems = API.Pawn.ListOfCopyItems();
			}

			private void APasteInv()
			{
				API.Pawn.PasteCopyItems(lOfCopyItems);
			}

			private void DrawLowerButtons(int x, int y)
			{
				int num = 120;
				SZWidgets.ButtonText(x - num, y, 120f, 30f, Label.UNDRESS, AUndress, Label.TIP_UNDRESS);
				num += 120;
				SZWidgets.ButtonText(x - num, y, 120f, 30f, Label.REDRESS, ARedress, Label.TIP_REDRESS);
				num += 120;
				SZWidgets.ButtonText(x - num, y, 120f, 30f, Label.REEQUIP, AReequip, Label.TIP_REEQUIP);
				num += 120;
				SZWidgets.ButtonText(x - num, y, 120f, 30f, Label.REINVENT, AReinvent, Label.TIP_REINVENT);
				num += 120;
				SZWidgets.ButtonText(x - num, y, 120f, 30f, "Equipment".Translate() + " +", AAddGun, Label.TIP_ADD_EQUIP);
				num += 120;
				SZWidgets.ButtonText(x - num, y, 120f, 30f, "Apparel".Translate() + " +", AAddApparel, Label.TIP_ADD_APPAREL);
				num += 120;
				SZWidgets.ButtonText(x - num, y, 120f, 30f, "Inventory".Translate() + " +", AAddItem, Label.TIP_ADD_ITEM);
			}

			private void AUndress()
			{
				List<Apparel> list = API.Pawn?.apparel?.WornApparel;
				if (!list.NullOrEmpty())
				{
					int index = list.IndexOf(list.RandomElement());
					if (Event.current.alt)
					{
						API.Pawn.apparel.WornApparel[index].Destroy();
					}
					else if (InStartingScreen)
					{
						API.Pawn.MoveDressToInv(API.Pawn.apparel.WornApparel[index].def.apparel.layers.LastOrDefault());
					}
					else
					{
						API.Pawn.apparel.TryDrop(list[index]);
					}
					API.UpdateGraphics();
				}
			}

			private void ARedress()
			{
				API.Pawn.Redress(null, originalColors: true, -1, pawnSpecific: true);
				API.UpdateGraphics();
			}

			private void AReequip()
			{
				API.Pawn.Reequip(null, -1);
				API.UpdateGraphics();
			}

			private void AReinvent()
			{
				API.Pawn.Reinvent(null, zufallswert.Next(1, 10));
				API.UpdateGraphics();
			}

			private void AAddGun()
			{
				WindowTool.Open(new DialogObjects(DialogType.Weapon));
			}

			private void AAddApparel()
			{
				WindowTool.Open(new DialogObjects(DialogType.Apparel));
			}

			private void AAddItem()
			{
				WindowTool.Open(new DialogObjects(DialogType.Object));
			}

			private void InterfaceDrop(Thing t, bool shift)
			{
				ThingWithComps thingWithComps = t as ThingWithComps;
				if (t is Apparel apparel && API.Pawn.apparel != null && API.Pawn.apparel.WornApparel.Contains(apparel))
				{
					if (shift)
					{
						API.Pawn.TransferToInventory(apparel);
					}
					else if (InStartingScreen || Event.current.alt)
					{
						API.Pawn.TransferToInventory(apparel);
						API.Pawn.DestroyApparel(apparel);
					}
					else
					{
						API.Pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.RemoveApparel, apparel), JobTag.Misc);
					}
				}
				else if (thingWithComps != null && API.Pawn.equipment != null && API.Pawn.IsEquippedByPawn(thingWithComps))
				{
					if (shift)
					{
						API.Pawn.TransferToInventory(thingWithComps);
					}
					else if (InStartingScreen || Event.current.alt)
					{
						API.Pawn.TransferToInventory(thingWithComps);
						API.Pawn.DestroyEquipment(thingWithComps);
					}
					else
					{
						API.Pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, thingWithComps), JobTag.Misc);
					}
				}
				else if (shift)
				{
					if (t.def.IsApparel && !API.Pawn.apparel.CanWearWithoutDroppingAnything(t.def))
					{
						API.Pawn.MoveDressToInv(t.def.apparel.LastLayer);
					}
					API.Pawn.TransferFromInventory(t);
				}
				else if (InStartingScreen || Event.current.alt)
				{
					API.Pawn.DestroyItem(t);
				}
				else if (t.def.destroyOnDrop)
				{
					API.Pawn.inventory.innerContainer.Remove(t);
				}
				else
				{
					API.Pawn.inventory.innerContainer.TryDrop(t, API.Pawn.Position, API.Pawn.Map, ThingPlaceMode.Near, out var _);
				}
			}

			private void InterfaceIngest(Thing t)
			{
				Job job = new Job(JobDefOf.Ingest, t);
				job.count = Mathf.Min(t.stackCount, t.def.ingestible.maxNumToIngestAtOnce);
				job.count = Mathf.Min(job.count, FoodUtility.WillIngestStackCountOf(API.Pawn, t.def, t.GetStatValue(StatDefOf.Nutrition)));
				API.Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			}

			private void AShowInfo(Thing thing)
			{
				WindowTool.Open(new Dialog_InfoCard(thing));
			}

			private void ADropOrDestroy(Thing thing)
			{
				SoundTool.PlayThis(SoundDefOf.Tick_High);
				InterfaceDrop(thing, shift: false);
				API.UpdateGraphics();
			}

			private void AShift(Thing thing)
			{
				SoundTool.PlayThis(SoundDefOf.Tick_High);
				InterfaceDrop(thing, shift: true);
				API.UpdateGraphics();
			}

			private void AIngestThing(Thing thing)
			{
				SoundTool.PlayThis(SoundDefOf.Tick_High);
				InterfaceIngest(thing);
				API.UpdateGraphics();
			}

			private string GetThingLabel(Thing thing)
			{
				string text = thing.LabelCap;
				if (thing is Apparel ap && API.Pawn.outfits != null && API.Pawn.outfits.forcedHandler.IsForced(ap))
				{
					text += ", " + "ApparelForcedLower".Translate();
				}
				return text;
			}

			private void DrawThingRow(ref float y, float width, Thing thing, DialogType type)
			{
				try
				{
					Rect rect = new Rect(0f, y, width, 28f);
					Text.Anchor = TextAnchor.MiddleLeft;
					GUI.color = ITab_Pawn_Gear.ThingLabelColor;
					Text.WordWrap = false;
					Widgets.Label(new Rect(36f, y, rect.width - 36f, rect.height), GetThingLabel(thing));
					Text.WordWrap = true;
					SZWidgets.ButtonImageVar(rect.width - 28f, y, 28f, 28f, "UI/Buttons/InfoButton", AShowInfo, thing, "DefInfoTip".Translate());
					rect.width -= 28f;
					SZWidgets.ButtonImageVar(rect.width - 28f, y, 28f, 28f, "UI/Buttons/Drop", ADropOrDestroy, thing, "DropThing".Translate() + Label.TIP_DESTROYDROP);
					rect.width -= 28f;
					SZWidgets.ButtonImageVar(rect.width - 28f, y, 28f, 28f, "bpfeil", AShift, thing);
					rect.width -= 28f;
					if (IsRandom)
					{
						if (Widgets.ButtonImage(new Rect(rect.width - 28f, y, 28f, 28f), ContentFinder<Texture2D>.Get("brandom")))
						{
							ARandomThing(thing, type);
						}
						rect.width -= 28f;
					}
					try
					{
						if ((thing.def.IsNutritionGivingIngestible || thing.def.IsNonMedicalDrug) && thing.IngestibleNow && API.Pawn.RaceProps.CanEverEat(thing))
						{
							SZWidgets.ButtonImageVar(rect.width - 28f, y, 28f, 28f, "UI/Buttons/Ingest", AIngestThing, thing);
							rect.width -= 28f;
						}
					}
					catch
					{
					}
					Rect rect2 = rect;
					rect2.xMin = rect2.xMax - 60f;
					CaravanThingsTabUtility.DrawMass(thing, rect2);
					rect.width -= 60f;
					if (Mouse.IsOver(rect))
					{
						GUI.color = ITab_Pawn_Gear.HighlightColor;
						GUI.DrawTexture(rect, (Texture)TexUI.HighlightTex);
						SZWidgets.ButtonInvisible(rect, delegate
						{
							WindowTool.Open(new DialogObjects(type, null, (ThingWithComps)thing));
						});
					}
					Widgets.ThingIcon(new Rect(4f, y, 32f, 32f), thing);
					DoThingToolTip(rect, thing);
					y += 32f;
				}
				catch
				{
				}
			}

			private void DoThingToolTip(Rect rect, Thing thing)
			{
				string text = thing.DescriptionDetailed;
				if (thing.def.useHitPoints)
				{
					text = text + "\n" + thing.HitPoints + " / " + thing.MaxHitPoints;
				}
				TooltipHandler.TipRegion(rect, text);
			}

			private void TryDrawComfyTemperatureRange(ref float curY, float width)
			{
				if (!API.Pawn.Dead)
				{
					Rect rect = new Rect(0f, curY, width, 22f);
					float statValue = API.Pawn.GetStatValue(StatDefOf.ComfyTemperatureMin);
					float statValue2 = API.Pawn.GetStatValue(StatDefOf.ComfyTemperatureMax);
					Widgets.Label(rect, string.Concat("ComfyTemperatureRange".Translate(), ": ", statValue.ToStringTemperature("F0"), " ~ ", statValue2.ToStringTemperature("F0")));
					curY += 22f;
				}
			}

			private void TryDrawMassInfo(ref float curY, float width)
			{
				if (!API.Pawn.Dead)
				{
					try
					{
						Rect rect = new Rect(0f, curY, width, 22f);
						float num = (float)Math.Round(MassUtility.GearAndInventoryMass(API.Pawn), 2);
						float num2 = (float)Math.Round(MassUtility.Capacity(API.Pawn), 2);
						string label = "MassCarried".Translate(new NamedArgument(num, "0.##"), new NamedArgument(num2, "0.##"));
						Widgets.Label(rect, label);
					}
					catch
					{
					}
					curY += 22f;
				}
			}

			private void TryDrawOverallArmor(ref float curY, float width, StatDef stat, string label)
			{
				float num = 0f;
				float num2 = Mathf.Clamp01(API.Pawn.GetStatValue(stat) / 2f);
				List<BodyPartRecord> allParts = API.Pawn.RaceProps.body.AllParts;
				List<Apparel> list = API.Pawn.apparel?.WornApparel;
				for (int i = 0; i < allParts.Count; i++)
				{
					float num3 = 1f - num2;
					if (list != null)
					{
						for (int j = 0; j < list.Count; j++)
						{
							if (list[j].def.apparel.CoversBodyPart(allParts[i]))
							{
								float num4 = Mathf.Clamp01(list[j].GetStatValue(stat) / 2f);
								num3 *= 1f - num4;
							}
						}
					}
					num += allParts[i].coverageAbs * (1f - num3);
				}
				num = Mathf.Clamp(num * 2f, 0f, 2f);
				Rect rect = new Rect(0f, curY, width, 100f);
				Widgets.Label(rect, label.Truncate(120f));
				rect.xMin += 120f;
				Widgets.Label(rect, num.ToStringPercent());
				curY += 22f;
			}
		}

		private class BlockLog
		{
			private Vector2 scrollPos;

			private float wLog;

			private float hLog;

			private float yLine;

			private int logCachedDisplayLastTick;

			private int logCachedPlayLastTick;

			private List<ITab_Pawn_Log_Utility.LogLineDisplayable> logCachedDisplay;

			private ITab_Pawn_Log_Utility.LogDrawData logData;

			private LogEntry logSeek;

			private string uniqueID;

			internal BlockLog()
			{
				wLog = 0f;
				hLog = 0f;
				yLine = 0f;
				LogInit();
			}

			private void LogInit()
			{
				logCachedDisplayLastTick = -1;
				logCachedPlayLastTick = -1;
				logCachedDisplay = null;
				logData = new ITab_Pawn_Log_Utility.LogDrawData();
				logSeek = null;
				uniqueID = API.Pawn?.ThingID;
			}

			internal void Draw(coord c)
			{
				int x = c.x;
				int y = c.y;
				int w = c.w;
				int h = c.h;
				if (API.Pawn == null)
				{
					return;
				}
				if (uniqueID != API.Pawn.ThingID)
				{
					LogInit();
				}
				if (logCachedDisplay == null || logCachedDisplayLastTick != API.Pawn.records.LastBattleTick || logCachedPlayLastTick != Find.PlayLog.LastTick)
				{
					logCachedDisplay = ITab_Pawn_Log_Utility.GenerateLogLinesFor(API.Pawn, showAll: true, showCombat: true, showSocial: true, 1000).ToList();
					logCachedDisplayLastTick = API.Pawn.records.LastBattleTick;
					logCachedPlayLastTick = Find.PlayLog.LastTick;
				}
				Rect outRect = new Rect(x + 10, y + 5, w - 10, h - 5);
				wLog = outRect.width - 26f;
				hLog = 0f;
				foreach (ITab_Pawn_Log_Utility.LogLineDisplayable item in logCachedDisplay)
				{
					if (item.Matches(logSeek))
					{
						scrollPos.y = hLog - (item.GetHeight(wLog) + outRect.height) / 2f;
					}
					hLog += item.GetHeight(wLog);
				}
				logSeek = null;
				if (hLog > 0f)
				{
					Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, hLog);
					logData.StartNewDraw();
					Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
					yLine = 0f;
					foreach (ITab_Pawn_Log_Utility.LogLineDisplayable item2 in logCachedDisplay)
					{
						item2.Draw(yLine, wLog, logData);
						yLine += item2.GetHeight(wLog);
					}
					Widgets.EndScrollView();
				}
				else
				{
					Text.Anchor = TextAnchor.MiddleCenter;
					Text.Font = GameFont.Medium;
					GUI.color = Color.grey;
					Widgets.Label(new Rect(outRect.x, 0f, outRect.width, outRect.height), "(" + "NoRecentEntries".Translate() + ")");
					Text.Anchor = TextAnchor.UpperLeft;
					GUI.color = Color.white;
				}
			}
		}

		private class BlockNeeds
		{
			private Vector2 scrollPos;

			private int wNeedBar;

			private int hNeedBar;

			private int yStartMemories;

			private int listH;

			private bool bToggleShowRemoveThought = false;

			internal BlockNeeds()
			{
				wNeedBar = 0;
				hNeedBar = 0;
				yStartMemories = 0;
			}

			internal void Draw(coord c)
			{
				int x = c.x;
				int y = c.y;
				int w = c.w;
				int h = c.h;
				if (API.Pawn.HasNeedsTracker() && !API.Pawn.Dead)
				{
					yStartMemories = DrawNeedBars(x, y, w, h);
					yStartMemories = ((API.Pawn.needs.AllNeeds.Count > 12) ? yStartMemories : (y + 80));
					DrawMemories(x + wNeedBar + 30, yStartMemories, wNeedBar + 225, h - yStartMemories - 36);
					DrawThoughtButtons(x + w - 50, yStartMemories, w, h);
					DrawLowerButtons(x, y + h - 30, w, h);
				}
			}

			private int DrawNeedBars(int x, int y, int w, int h)
			{
				wNeedBar = w / 3;
				hNeedBar = h / 11;
				if (!InStartingScreen || !IsAVPActive)
				{
					foreach (Need allNeed in API.Pawn.needs.AllNeeds)
					{
						if (allNeed.def.defName == "Mood")
						{
							GUI.color = ((!((double)allNeed.CurLevelPercentage < 0.2)) ? Color.white : (GUI.color = Color.red));
							try
							{
								allNeed.DrawOnGUI(new Rect(x + wNeedBar, y, wNeedBar * 2, hNeedBar));
							}
							catch
							{
							}
							SZWidgets.ButtonImageVar(x + wNeedBar + 11, y + 30, 24f, 24f, "bbackward", ASubNeed, allNeed);
							SZWidgets.ButtonImageVar(x + wNeedBar * 3 - 36, y + 30, 24f, 24f, "bforward", AAddNeed, allNeed);
						}
					}
				}
				int num = 0;
				foreach (Need allNeed2 in API.Pawn.needs.AllNeeds)
				{
					if (allNeed2.def.defName != "Mood")
					{
						if (num == 11)
						{
							x += wNeedBar;
							y = 80;
						}
						GUI.color = ((!((double)allNeed2.CurLevelPercentage < 0.2)) ? Color.white : (GUI.color = Color.red));
						allNeed2.DrawOnGUI(new Rect(x, y, wNeedBar, hNeedBar));
						SZWidgets.ButtonImageVar(x + 11, y + 30, 24f, 24f, "bbackward", ASubNeed, allNeed2);
						SZWidgets.ButtonImageVar(x + wNeedBar - 36, y + 30, 24f, 24f, "bforward", AAddNeed, allNeed2);
						y += 60;
						num++;
					}
				}
				return y;
			}

			private void AAddNeed(Need need)
			{
				need.CurLevelPercentage += 0.05f;
			}

			private void ASubNeed(Need need)
			{
				need.CurLevelPercentage -= 0.05f;
			}

			private void DrawMemories(int x, int y, int w, int h)
			{
				List<Thought> thoughtsSorted = API.Pawn.GetThoughtsSorted();
				Text.Font = GameFont.Small;
				GUI.color = Color.white;
				SZWidgets.Label(new Rect(x, y, w, 30f), string.Concat("MemoryLower".Translate() + ": ", thoughtsSorted.Count.ToString()));
				y += 30;
				Thought example = null;
				int num = ThoughtTool.CountOfDefs<Thought_MemorySocial>(thoughtsSorted, out example, "AteNon");
				listH = (thoughtsSorted.Count - num + 1) * 22;
				Rect outRect = new Rect(x, y, w, h);
				Rect rect = new Rect(0f, 0f, outRect.width - 16f, listH);
				Widgets.BeginScrollView(outRect, ref scrollPos, rect);
				Rect rect2 = rect.ContractedBy(0f);
				rect2.height = listH;
				Listing_X listing_X = new Listing_X();
				listing_X.Begin(rect2);
				using (List<Thought>.Enumerator enumerator = thoughtsSorted.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Thought current = enumerator.Current;
						if (!current.def.IsClassOf<Thought_MemorySocial>() || !current.def.defName.StartsWith("AteNon"))
						{
							LabelHelper(current, out var label, out var _, out var otherPawnName, out var tooltip);
							IconAndOffsetHelper(current, out var opinionOffset, out var moodOffset, out var icon, out var iconColor);
							if (listing_X.SelectableThought(label, icon, iconColor, opinionOffset, moodOffset, tooltip, otherPawnName, bToggleShowRemoveThought))
							{
								API.Pawn.RemoveThought(enumerator.Current);
							}
						}
					}
				}
				if (example != null)
				{
					LabelHelper(example, out var label2, out var _, out var otherPawnName2, out var tooltip2, num);
					IconAndOffsetHelper(example, out var opinionOffset2, out var moodOffset2, out var icon2, out var iconColor2);
					if (listing_X.SelectableThought(label2, icon2, iconColor2, opinionOffset2, moodOffset2, tooltip2, otherPawnName2, bToggleShowRemoveThought))
					{
						API.Pawn.RemoveThought(example);
					}
				}
				listing_X.End();
				Widgets.EndScrollView();
			}

			private void LabelHelper(Thought t, out string label, out string desc, out string otherPawnName, out string tooltip, int count = 0)
			{
				label = t.GetThoughtLabel();
				desc = ((t.CurStageIndex < 0) ? t.GetThoughtDescription() : t.Description);
				tooltip = desc;
				if (Prefs.DevMode)
				{
					tooltip = ((desc == null) ? "\n\n" : (desc + "\n\n"));
					tooltip = tooltip + t.def.defName + "\n" + t.def.ThoughtClass.ToString() + "\n";
					if (t.CurStageIndex >= 0)
					{
						try
						{
							int num = (int)t.def.stages[t.CurStageIndex].baseMoodEffect;
							int num2 = (int)t.def.stages[t.CurStageIndex].baseOpinionOffset;
							tooltip = tooltip + "Stage " + t.CurStageIndex + "\nBaseMood: " + num.ToString().Colorize((num == 0) ? ColorLibrary.Grey : ((num > 0) ? ColorLibrary.Green : ColorLibrary.Red));
							tooltip = tooltip + " BaseOpinion: " + num2.ToString().Colorize((num2 == 0) ? ColorLibrary.Grey : ((num2 > 0) ? ColorLibrary.Green : ColorLibrary.Red));
						}
						catch
						{
						}
					}
					else
					{
						tooltip = tooltip + "Stage " + t.CurStageIndex;
					}
				}
				string reason;
				Pawn otherPawn = t.GetOtherPawn(out reason);
				otherPawnName = otherPawn.GetPawnName();
				try
				{
					if (t.def.defName == "WrongApparelGender")
					{
						label = string.Format(label, t.pawn.GetOppositeGender().ToString().Translate());
					}
					if (t.def.defName == "DeadMansApparel")
					{
						label = t.LabelCap.CapitalizeFirst();
					}
					if (label.Contains("{0}") && !otherPawnName.NullOrEmpty())
					{
						if (label.Contains("{1}"))
						{
							label = string.Format(label, reason, otherPawnName);
						}
						else
						{
							label = string.Format(label, otherPawnName);
						}
					}
					if (label.Contains("{TITLE}") || label.Contains("{TITLE_label}"))
					{
						if (t.def.IsClassOf<Thought_MemoryRoyalTitle>())
						{
							Thought_MemoryRoyalTitle thought_MemoryRoyalTitle = (Thought_MemoryRoyalTitle)t;
							string newValue = ((API.Pawn.gender == Gender.Female) ? thought_MemoryRoyalTitle.titleDef.labelFemale : thought_MemoryRoyalTitle.titleDef.label);
							label = label.Replace("{TITLE}", newValue).Replace("{TITLE_label}", newValue);
						}
						else
						{
							label = label.Replace("{TITLE}", API.Pawn.GetMainTitle()).Replace("{TITLE_label}", API.Pawn.GetMainTitle());
						}
					}
					else if (t.def.IsClassOf<Thought_IdeoRoleEmpty>())
					{
						Thought_IdeoRoleEmpty thought_IdeoRoleEmpty = (Thought_IdeoRoleEmpty)t;
						label = label.Replace("{ROLE}", thought_IdeoRoleEmpty.Role.LabelCap).Replace("{ROLE_label}", thought_IdeoRoleEmpty.Role.LabelCap);
					}
					else if (t.def.IsClassOf<Thought_IdeoRoleLost>())
					{
						Thought_IdeoRoleLost thought_IdeoRoleLost = (Thought_IdeoRoleLost)t;
						label = label.Replace("{ROLE}", thought_IdeoRoleLost.Role.LabelCap).Replace("{ROLE_label}", thought_IdeoRoleLost.Role.LabelCap);
					}
					else if (t.def.IsClassOf<Thought_IdeoRoleApparelRequirementNotMet>())
					{
						Thought_IdeoRoleApparelRequirementNotMet thought_IdeoRoleApparelRequirementNotMet = (Thought_IdeoRoleApparelRequirementNotMet)t;
						label = label.Replace("{ROLE}", thought_IdeoRoleApparelRequirementNotMet.Role.LabelCap).Replace("{ROLE_label}", thought_IdeoRoleApparelRequirementNotMet.Role.LabelCap);
					}
					else if (t.def.IsClassOf<Thought_Situational_WearingDesiredApparel>())
					{
						Thought_Situational_WearingDesiredApparel thought_Situational_WearingDesiredApparel = (Thought_Situational_WearingDesiredApparel)t;
						Precept_Apparel precept_Apparel = (Precept_Apparel)thought_Situational_WearingDesiredApparel.sourcePrecept;
						label = label.Replace("{APPAREL}", precept_Apparel.apparelDef.LabelCap).Replace("{APPAREL_label}", precept_Apparel.apparelDef.LabelCap);
					}
					else if (t.def.IsClassOf<Thought_PsychicHarmonizer>())
					{
						Thought_PsychicHarmonizer thought_PsychicHarmonizer = (Thought_PsychicHarmonizer)t;
						label = thought_PsychicHarmonizer.LabelCap;
					}
					else if (t.def.IsClassOf<Thought_RelicAtRitual>())
					{
						Thought_RelicAtRitual thought_RelicAtRitual = (Thought_RelicAtRitual)t;
						label = label.Replace("{RELICNAME}", thought_RelicAtRitual.relicName).Replace("{RELICNAME_label}", thought_RelicAtRitual.relicName);
					}
					if (count > 0)
					{
						label = label + " x" + count;
					}
				}
				catch
				{
				}
			}

			private void IconAndOffsetHelper(Thought t, out float opinionOffset, out float moodOffset, out Texture2D icon, out Color iconColor)
			{
				bool flag = t.def.IsSocial && t.def.IsSituational;
				opinionOffset = t.GetOpinionOffset();
				moodOffset = t.TryGetMoodOffset();
				Texture2D memberValue = t.def.GetMemberValue<Texture2D>("iconInt", null);
				icon = ((memberValue != null) ? memberValue : ((opinionOffset != float.MinValue || flag) ? ContentFinder<Texture2D>.Get("bsocial") : ((t.def.Icon != null) ? t.def.Icon : ((moodOffset > 0f) ? ContentFinder<Texture2D>.Get("Things/Mote/ThoughtSymbol/GenericGood") : ContentFinder<Texture2D>.Get("Things/Mote/ThoughtSymbol/GenericBad")))));
				iconColor = (flag ? ColorLibrary.Aquamarine : Color.white);
			}

			private void DrawThoughtButtons(int x, int y, int w, int h)
			{
				SZWidgets.ButtonImage(x, y, 24f, 24f, "UI/Buttons/Dev/Add", ADoAddThought);
				SZWidgets.ButtonImage(x - 25, y, 24f, 24f, "bminus", ADoRemoveThought);
			}

			private void ADoAddThought()
			{
				WindowTool.Open(new DialogAddThought());
			}

			private void ADoRemoveThought()
			{
				bToggleShowRemoveThought = !bToggleShowRemoveThought;
			}

			private void DrawLowerButtons(int x, int y, int w, int h)
			{
				GUI.color = Color.white;
				SZWidgets.ButtonText(x + w - 120, y, 120f, 30f, Label.FULLNEEDS, AFullNeeds);
				SZWidgets.ButtonText(x + w - 230, y, 110f, 30f, Label.NOMEMORIES, AClearAllMemory);
			}

			private void AClearAllMemory()
			{
				API.Pawn.ClearAllThoughts();
			}

			private void AFullNeeds()
			{
				foreach (Need allNeed in API.Pawn.needs.AllNeeds)
				{
					allNeed.CurLevelPercentage = 1f;
				}
			}
		}

		private class BlockRecords
		{
			internal BlockRecords()
			{
			}

			internal void Draw(coord c)
			{
				int x = c.x;
				int y = c.y;
				int w = c.w;
				int h = c.h;
				if (API.Pawn != null)
				{
					RecordTool.DrawRecordCard(new Rect(x + 10, y, w - 10, h), API.Pawn);
					DrawMiniCapsule(x + 110, y + h - 160);
				}
			}

			private void DrawMiniCapsule(int x, int y)
			{
				if (!InStartingScreen)
				{
					SZWidgets.ButtonImage(x, y, 200f, 144f, "bcapsule", delegate
					{
						WindowTool.Open(new DialogCapsuleUI());
					});
				}
			}
		}

		private class BlockSocial : IPawnable
		{
			private const int LIST_OBJ_H = 196;

			private const int LIST_OBJ_W = 140;

			private const int LIST_SELECTED_H = 180;

			private const int widthLeft = 400;

			private bool bShowRemove;

			private DirectPawnRelation dpr;

			private Vector2 imgSize;

			private List<PawnRelationDef> lOfIndirectRelations;

			private List<InspirationDef> lOfInspirations;

			private List<MentalStateDef> lOfMentalStates;

			private List<Pawn> lOfRelatedPawns;

			private List<PawnRelationDef> lOfRelations;

			private Vector2 scrollDirectRelated = default(Vector2);

			private Vector2 scrollIndirectRelated = default(Vector2);

			private Gender selectedGender;

			private Gender selectedGender2;

			private Gender selectedGender3;

			private Gender selectedGender4;

			private PawnRelationDef selectedIndirectRelationDef;

			private InspirationDef selectedInspiration;

			private MentalStateDef selectedMentalState;

			private Pawn selectedPawnToSwap;

			private PawnRelationDef selRelation1;

			private PawnRelationDef selRelation2;

			private PawnRelationDef selRelation3;

			private PawnRelationDef selRelation4;

			private bool allowGender2 = false;

			private bool allowGender3 = false;

			private bool allowGender4 = false;

			private bool showPawn3 = true;

			private bool showPawn4 = true;

			private bool canAddRelation = true;

			public Pawn SelectedPawn { get; set; }

			public Pawn SelectedPawn2 { get; set; }

			public Pawn SelectedPawn3 { get; set; }

			public Pawn SelectedPawn4 { get; set; }

			private Func<InspirationDef, string> FGetInspirationLabel => (InspirationDef i) => (i == null) ? Label.NONE : ((string)i.LabelCap);

			private Func<MentalStateDef, string> FGetMentalLabel => (MentalStateDef m) => (m == null) ? Label.NONE : ((string)m.LabelCap);

			private Func<ThoughtDef, string> FGetThoughtLabel => (ThoughtDef t) => (t == null) ? Label.NONE : t.GetThoughtLabel();

			private Func<PawnRelationDef, string> FGetRelationLabel => (PawnRelationDef r) => GetLabelSelectedPawn(r);

			private Func<PawnRelationDef, string> FGetRelationLabel2 => (PawnRelationDef r) => GetLabelSelectedPawn2(r);

			private Func<PawnRelationDef, string> FGetRelationLabel3 => (PawnRelationDef r) => GetLabelSelectedPawn3(r);

			private Func<PawnRelationDef, string> FGetRelationLabel4 => (PawnRelationDef r) => GetLabelSelectedPawn4(r);

			internal BlockSocial()
			{
				bShowRemove = false;
				dpr = null;
				selectedInspiration = null;
				selectedMentalState = null;
				selectedIndirectRelationDef = null;
				selectedPawnToSwap = null;
				SelectedPawn = null;
				SelectedPawn2 = null;
				SelectedPawn3 = null;
				SelectedPawn4 = null;
				selectedGender = Gender.Male;
				selectedGender2 = Gender.Male;
				selectedGender3 = Gender.Female;
				selectedGender4 = Gender.Male;
				selRelation1 = PawnRelationDefOf.Parent;
				selRelation2 = null;
				selRelation3 = null;
				selRelation4 = null;
				imgSize = new Vector2(140f, 196f);
				Type aType = Reflect.GetAType("RimWorld", "SocialCardUtility");
				if (aType != null)
				{
					aType.SetMemberValue("showAllRelations", true);
				}
				lOfRelations = DefTool.ListBy((PawnRelationDef r) => !r.implied).ToList();
				lOfRelations.Add(PawnRelationDefOf.Grandparent);
				lOfRelations.Add(PawnRelationDefOf.Sibling);
				lOfRelations.Add(PawnRelationDefOf.HalfSibling);
				lOfRelations.Add(PawnRelationDefOf.Child);
				lOfRelations.Add(PawnRelationDefOf.UncleOrAunt);
				lOfIndirectRelations = null;
				lOfMentalStates = API.ListOf<MentalStateDef>(EType.MentalStates);
				lOfInspirations = API.ListOf<InspirationDef>(EType.Inspirations);
				lOfRelatedPawns = null;
			}

			private string GetLabelSelectedPawn(PawnRelationDef r)
			{
				return (selectedGender == Gender.Male) ? r.label : (r.labelFemale.NullOrEmpty() ? r.label : r.labelFemale);
			}

			private string GetLabelSelectedPawn2(PawnRelationDef r)
			{
				if (selRelation1 == PawnRelationDefOf.Child)
				{
					return ((selectedGender2 == Gender.Male) ? r.label : (r.labelFemale.NullOrEmpty() ? r.label : r.labelFemale)) + "(" + SelectedPawn.GetPawnName() + ")";
				}
				return ((selectedGender2 == Gender.Male) ? r.label : (r.labelFemale.NullOrEmpty() ? r.label : r.labelFemale)) + "(" + API.Pawn.GetPawnName() + ")";
			}

			private string GetLabelSelectedPawn3(PawnRelationDef r)
			{
				if (selRelation1 == PawnRelationDefOf.UncleOrAunt || selRelation1 == PawnRelationDefOf.Child)
				{
					return ((selectedGender3 == Gender.Male) ? r.label : (r.labelFemale.NullOrEmpty() ? r.label : r.labelFemale)) + "(" + ((SelectedPawn != null) ? SelectedPawn.GetPawnName() : "") + ")";
				}
				return ((selectedGender3 == Gender.Male) ? r.label : (r.labelFemale.NullOrEmpty() ? r.label : r.labelFemale)) + "(" + API.Pawn.GetPawnName() + ")";
			}

			private string GetLabelSelectedPawn4(PawnRelationDef r)
			{
				if (selRelation1 == PawnRelationDefOf.UncleOrAunt)
				{
					return ((selectedGender4 == Gender.Male) ? r.label : (r.labelFemale.NullOrEmpty() ? r.label : r.labelFemale)) + "(" + ((SelectedPawn != null) ? SelectedPawn.GetPawnName() : "") + ")";
				}
				return ((selectedGender4 == Gender.Male) ? r.label : (r.labelFemale.NullOrEmpty() ? r.label : r.labelFemale)) + "(" + ((SelectedPawn != null) ? SelectedPawn.GetPawnName() : "") + ")";
			}

			internal void Draw(coord c)
			{
				int x = c.x;
				int y = c.y;
				int w = c.w;
				int h = c.h;
				if (API.Pawn.HasRelationTracker())
				{
					y += 20;
					DrawRelations(x, ref y, w, h);
					DrawCard(x, ref y, w, h);
				}
			}

			private void AAddIndirectRelation()
			{
				if (selRelation1 == PawnRelationDefOf.Child)
				{
					SelectedPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn2);
					SelectedPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn3);
				}
				else if (selRelation1 == PawnRelationDefOf.HalfSibling)
				{
					API.Pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn2);
					API.Pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn3);
					SelectedPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn3);
					SelectedPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn4);
				}
				else if (selRelation1 == PawnRelationDefOf.Sibling)
				{
					SelectedPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn2);
					SelectedPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn3);
					API.Pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn2);
					API.Pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn3);
				}
				else if (selRelation1 == PawnRelationDefOf.Grandparent)
				{
					SelectedPawn2.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn);
					if (API.Pawn.GetFirstParentForPawn(selectedGender2) == null)
					{
						API.Pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn2);
					}
				}
				else if (selRelation1 == PawnRelationDefOf.UncleOrAunt)
				{
					SelectedPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn3);
					SelectedPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn4);
					SelectedPawn2.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn3);
					SelectedPawn2.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn4);
					API.Pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, SelectedPawn2);
				}
				SelectedPawn = null;
				SelectedPawn2 = null;
				SelectedPawn3 = null;
				SelectedPawn4 = null;
				selRelation1 = PawnRelationDefOf.Parent;
			}

			private void AAddRelation()
			{
				API.Pawn.relations.AddDirectRelation(selRelation1, SelectedPawn);
				SelectedPawn = null;
				SelectedPawn2 = null;
				SelectedPawn3 = null;
				SelectedPawn4 = null;
			}

			private void AAddThought()
			{
				WindowTool.Open(new DialogAddThought(Label.TH_SOCIAL));
			}

			private void AChangeInspiration(InspirationDef i)
			{
				selectedInspiration = i;
				if (API.Pawn.Inspiration != null)
				{
					API.Pawn.Inspiration.PostEnd();
					API.Pawn.mindState.inspirationHandler.EndInspiration(API.Pawn.Inspiration);
				}
				if (i != null)
				{
					API.Pawn.mindState.inspirationHandler.TryStartInspiration(i);
				}
			}

			private void AChangeMentalState(MentalStateDef m)
			{
				selectedMentalState = m;
				API.Pawn.Notify_Teleported();
				if (API.Pawn.MentalState != null)
				{
					API.Pawn.MentalState.PostEnd();
					API.Pawn.mindState.mentalStateHandler.Reset();
				}
				if (m == MentalStateDefOf.SocialFighting)
				{
					List<Pawn> source = Find.CurrentMap.mapPawns.FreeColonists.Where((Pawn td) => td != API.Pawn).ToList();
					Pawn otherPawn = source.RandomElement();
					API.Pawn.interactions.StartSocialFight(otherPawn);
				}
				else
				{
					API.Pawn.mindState.mentalStateHandler.TryStartMentalState(m);
				}
			}

			private void AOnGenderChange()
			{
				selectedGender = ((selectedGender == Gender.Female) ? Gender.Male : Gender.Female);
				SelectedPawn = null;
			}

			private void AOnGenderChange2()
			{
				selectedGender2 = ((selectedGender2 == Gender.Female) ? Gender.Male : Gender.Female);
				SelectedPawn2 = null;
			}

			private void AOnGenderChange3()
			{
				selectedGender3 = ((selectedGender3 == Gender.Female) ? Gender.Male : Gender.Female);
				SelectedPawn3 = null;
			}

			private void AOnGenderChange4()
			{
				selectedGender4 = ((selectedGender4 == Gender.Female) ? Gender.Male : Gender.Female);
				SelectedPawn4 = null;
			}

			private void AOnImgAction()
			{
				WindowTool.Open(new DialogChoosePawn(this, 1, selectedGender));
			}

			private void AOnRelationSelected(PawnRelationDef pr)
			{
				selRelation1 = pr;
				if (Prefs.DevMode && selRelation1 != null)
				{
					MessageTool.Show(selRelation1.defName + " " + ((selectedGender == Gender.Male) ? selRelation1.label : selRelation1.labelFemale));
				}
				selectedGender2 = Gender.Male;
				selectedGender3 = Gender.Female;
				selectedGender4 = Gender.Male;
				SelectedPawn = null;
				SelectedPawn2 = null;
				SelectedPawn3 = null;
				SelectedPawn4 = null;
			}

			private void ASelectOtherPawn()
			{
				API.Pawn = selectedPawnToSwap;
			}

			private void AToggleRemove()
			{
				bShowRemove = !bShowRemove;
			}

			private void CreateDirect(int x, int y, int w, int h)
			{
				if (API.Pawn.HasRelationTracker() && !API.Pawn.relations.DirectRelations.NullOrEmpty())
				{
					SZWidgets.ScrollView(x, y, w, h, API.Pawn.relations.DirectRelations.Count, 196, ref scrollDirectRelated, DrawDirect);
				}
			}

			private void CreateIndirect(int x, int y, int w, int h)
			{
				if (API.Pawn.HasRelationTracker())
				{
					lOfRelatedPawns = API.Pawn.GetRelatedPawns(out var countImpliedByOtherPawn);
					SZWidgets.ScrollView(x, y, w, h, countImpliedByOtherPawn, 196, ref scrollIndirectRelated, DrawIndirect);
				}
			}

			private void DrawCard(int x, ref int y, int w, int h)
			{
				Rect rect = new Rect(x + 400, y, w - 400, h - 20);
				Widgets.DrawBoxSolid(rect, ColorTool.colDarkDimGray);
				SocialCardUtility.DrawRelationsAndOpinions(rect, API.Pawn);
				y += h - y;
			}

			private void DrawDirect(Listing_X view)
			{
				try
				{
					using List<DirectPawnRelation>.Enumerator enumerator = API.Pawn.relations.DirectRelations.GetEnumerator();
					while (enumerator.MoveNext())
					{
						Pawn otherPawn = enumerator.Current.otherPawn;
						RenderTexture image = PortraitsCache.Get(otherPawn, imgSize, Rot4.South);
						string name = enumerator.Current.RelationLabelDirect();
						string tooltip = API.Pawn.RelationTooltip(otherPawn);
						bool selected = dpr == enumerator.Current;
						switch (view.Selectable(name, selected, tooltip, image, null, null, imgSize, bShowRemove, 180f, Color.white, ColorTool.colLightGray))
						{
						case 1:
							selectedPawnToSwap = otherPawn;
							dpr = enumerator.Current;
							break;
						case 2:
							API.Pawn.relations.RemoveDirectRelation(enumerator.Current);
							dpr = null;
							break;
						}
					}
				}
				catch
				{
				}
			}

			private void DrawIndirect(Listing_X view)
			{
				try
				{
					foreach (Pawn lOfRelatedPawn in lOfRelatedPawns)
					{
						lOfIndirectRelations = API.Pawn.GetRelations(lOfRelatedPawn).ToList();
						foreach (PawnRelationDef lOfIndirectRelation in lOfIndirectRelations)
						{
							if (lOfIndirectRelation.implied)
							{
								RenderTexture image = PortraitsCache.Get(lOfRelatedPawn, imgSize, Rot4.South);
								string name = lOfIndirectRelation.RelationLabelIndirect(lOfRelatedPawn);
								string tooltip = API.Pawn.RelationTooltip(lOfRelatedPawn);
								bool selected = selectedPawnToSwap == lOfRelatedPawn && selectedIndirectRelationDef == lOfIndirectRelation;
								int num = view.Selectable(name, selected, tooltip, image, null, null, imgSize, withRemove: false, 180f, Color.white, ColorTool.colLightGray);
								if (num == 1)
								{
									selectedPawnToSwap = lOfRelatedPawn;
									selectedIndirectRelationDef = lOfIndirectRelation;
									dpr = null;
								}
							}
						}
					}
				}
				catch
				{
				}
			}

			private void HandleChild()
			{
				showPawn3 = true;
				showPawn4 = false;
				allowGender2 = false;
				allowGender3 = false;
				allowGender4 = false;
				selRelation2 = PawnRelationDefOf.Parent;
				selRelation3 = PawnRelationDefOf.Parent;
				if (API.Pawn.gender == Gender.Male)
				{
					SelectedPawn2 = API.Pawn;
				}
				else
				{
					SelectedPawn3 = API.Pawn;
				}
				selectedGender2 = Gender.Male;
				selectedGender3 = Gender.Female;
				if (SelectedPawn == API.Pawn || SelectedPawn == SelectedPawn2 || SelectedPawn == SelectedPawn3)
				{
					SelectedPawn = null;
				}
				if (SelectedPawn2 == SelectedPawn)
				{
					SelectedPawn2 = null;
				}
				if (SelectedPawn3 == SelectedPawn)
				{
					SelectedPawn3 = null;
				}
				canAddRelation = SelectedPawn != null && SelectedPawn2 != null && SelectedPawn3 != null;
			}

			private void HandleUncle()
			{
				showPawn3 = true;
				showPawn4 = true;
				allowGender2 = true;
				allowGender3 = false;
				allowGender4 = false;
				selRelation2 = PawnRelationDefOf.Parent;
				selRelation3 = PawnRelationDefOf.Parent;
				selRelation4 = PawnRelationDefOf.Parent;
				Pawn firstParentForPawn = API.Pawn.GetFirstParentForPawn(selectedGender2);
				if (firstParentForPawn != null)
				{
					SelectedPawn2 = firstParentForPawn;
				}
				Pawn firstParentForPawn2 = SelectedPawn.GetFirstParentForPawn(selectedGender3);
				if (firstParentForPawn2 != null)
				{
					SelectedPawn3 = firstParentForPawn2;
				}
				Pawn firstParentForPawn3 = SelectedPawn.GetFirstParentForPawn(selectedGender4);
				if (firstParentForPawn3 != null)
				{
					SelectedPawn4 = firstParentForPawn3;
				}
				if (SelectedPawn == API.Pawn || SelectedPawn == SelectedPawn2 || SelectedPawn == SelectedPawn3 || SelectedPawn == SelectedPawn4)
				{
					SelectedPawn = null;
				}
				if (SelectedPawn3 == API.Pawn || SelectedPawn3 == SelectedPawn || SelectedPawn3 == SelectedPawn2 || SelectedPawn3 == SelectedPawn4)
				{
					SelectedPawn3 = null;
				}
				if (SelectedPawn4 == API.Pawn || SelectedPawn4 == SelectedPawn || SelectedPawn4 == SelectedPawn2 || SelectedPawn4 == SelectedPawn3)
				{
					SelectedPawn4 = null;
				}
				canAddRelation = SelectedPawn != null && SelectedPawn2 != null && SelectedPawn3 != null && SelectedPawn4 != null;
			}

			private void HandleSibling()
			{
				showPawn3 = true;
				showPawn4 = false;
				allowGender2 = true;
				allowGender3 = true;
				allowGender4 = false;
				selRelation2 = PawnRelationDefOf.Parent;
				selRelation3 = PawnRelationDefOf.Parent;
				if (selectedGender2 == Gender.Male)
				{
					selectedGender3 = Gender.Female;
				}
				else
				{
					selectedGender3 = Gender.Male;
				}
				Pawn firstParentForPawn = API.Pawn.GetFirstParentForPawn(selectedGender2);
				if (firstParentForPawn != null)
				{
					SelectedPawn2 = firstParentForPawn;
				}
				Pawn firstParentForPawn2 = API.Pawn.GetFirstParentForPawn(selectedGender3);
				if (firstParentForPawn2 != null)
				{
					SelectedPawn3 = firstParentForPawn2;
				}
				if (SelectedPawn3 == null)
				{
					firstParentForPawn2 = SelectedPawn.GetFirstParentForPawn(selectedGender3);
					if (firstParentForPawn2 != null)
					{
						SelectedPawn3 = firstParentForPawn2;
					}
				}
				if (SelectedPawn2 == null)
				{
					firstParentForPawn = SelectedPawn.GetFirstParentForPawn(selectedGender2);
					if (firstParentForPawn != null)
					{
						SelectedPawn2 = firstParentForPawn;
					}
				}
				if (SelectedPawn == API.Pawn || SelectedPawn == SelectedPawn2 || SelectedPawn == SelectedPawn3)
				{
					SelectedPawn = null;
				}
				if (SelectedPawn3 == SelectedPawn2)
				{
					SelectedPawn3 = null;
				}
				canAddRelation = SelectedPawn != null && SelectedPawn2 != null && SelectedPawn3 != null;
			}

			private void HandleHalfSibling()
			{
				showPawn3 = true;
				showPawn4 = true;
				allowGender2 = false;
				allowGender3 = true;
				allowGender4 = false;
				selRelation2 = PawnRelationDefOf.Parent;
				selRelation3 = PawnRelationDefOf.Parent;
				selRelation4 = PawnRelationDefOf.Parent;
				if (selectedGender3 == Gender.Male)
				{
					selectedGender2 = Gender.Female;
					selectedGender4 = Gender.Female;
				}
				else
				{
					selectedGender2 = Gender.Male;
					selectedGender4 = Gender.Male;
				}
				Pawn commonParent = RelationTool.GetCommonParent(API.Pawn, SelectedPawn, selectedGender3);
				if (commonParent != null)
				{
					SelectedPawn3 = commonParent;
				}
				if (commonParent == null)
				{
					Pawn firstParentForPawn = API.Pawn.GetFirstParentForPawn(selectedGender3);
					if (firstParentForPawn != null)
					{
						SelectedPawn3 = firstParentForPawn;
					}
					if (SelectedPawn3 == null)
					{
						firstParentForPawn = SelectedPawn.GetFirstParentForPawn(selectedGender3);
						if (firstParentForPawn != null)
						{
							SelectedPawn3 = firstParentForPawn;
						}
					}
				}
				Pawn firstParentForPawn2 = API.Pawn.GetFirstParentForPawn(selectedGender2);
				if (firstParentForPawn2 != null)
				{
					SelectedPawn2 = firstParentForPawn2;
				}
				Pawn firstParentForPawn3 = SelectedPawn.GetFirstParentForPawn(selectedGender4);
				if (firstParentForPawn3 != null)
				{
					SelectedPawn4 = firstParentForPawn3;
				}
				if (SelectedPawn == API.Pawn || SelectedPawn == SelectedPawn2 || SelectedPawn == SelectedPawn3 || SelectedPawn == SelectedPawn4)
				{
					SelectedPawn = null;
				}
				if (RelationTool.AreThosePawnSisBro(API.Pawn, SelectedPawn))
				{
					SelectedPawn = null;
				}
				if (SelectedPawn2 == SelectedPawn4)
				{
					SelectedPawn4 = null;
				}
				canAddRelation = SelectedPawn != null && SelectedPawn2 != null && SelectedPawn3 != null && SelectedPawn4 != null;
			}

			private void HandleGrandparent()
			{
				showPawn3 = false;
				showPawn4 = false;
				allowGender2 = true;
				allowGender3 = false;
				allowGender4 = false;
				selRelation2 = PawnRelationDefOf.Parent;
				Pawn firstParentForPawn = API.Pawn.GetFirstParentForPawn(selectedGender2);
				if (firstParentForPawn != null)
				{
					SelectedPawn2 = firstParentForPawn;
				}
				if (SelectedPawn == API.Pawn || SelectedPawn == SelectedPawn2)
				{
					SelectedPawn = null;
				}
				canAddRelation = SelectedPawn != null && SelectedPawn2 != null;
			}

			private void DrawRelations(int x, ref int y, int w, int h)
			{
				Widgets.DrawBoxSolid(new Rect(x, y, 400f, h), ColorTool.colAsche);
				DrawUpper(x, y, w, h);
				SZWidgets.FloatMenuOnLabelAndImage(new Rect(x + 10, y, 110f, 100f), ColorTool.colDarkDimGray, "bplus2", SelectedPawn, ColorTool.colDimGray, lOfRelations, FGetRelationLabel, selRelation1, AOnRelationSelected, AOnImgAction);
				SZWidgets.ButtonImage(new Rect(x + 100, y - 20, 20f, 20f), (selectedGender == Gender.Female) ? "bfemale" : "bmale", AOnGenderChange);
				int num = 0;
				if (selRelation1 != null)
				{
					if (selRelation1.implied)
					{
						if (selRelation1 == PawnRelationDefOf.Child)
						{
							HandleChild();
						}
						else if (selRelation1 == PawnRelationDefOf.Sibling)
						{
							HandleSibling();
						}
						else if (selRelation1 == PawnRelationDefOf.HalfSibling)
						{
							HandleHalfSibling();
						}
						else if (selRelation1 == PawnRelationDefOf.Grandparent)
						{
							HandleGrandparent();
						}
						else if (selRelation1 == PawnRelationDefOf.UncleOrAunt)
						{
							HandleUncle();
						}
						num += 130;
						Text.WordWrap = true;
						SZWidgets.LabelBackground(new Rect(x + 10, y + num - 20, 110f, 20f), GetLabelSelectedPawn2(selRelation2), ColorTool.colDarkDimGray);
						SZWidgets.FloatMenuOnLabelAndImage(new Rect(x + 10, y + num, 110f, 100f), ColorTool.colDarkDimGray, "bplus2", SelectedPawn2, ColorTool.colDimGray, lOfRelations, FGetRelationLabel2, selRelation2, null, AOnParent2Selected, allowGender2);
						if (allowGender2)
						{
							SZWidgets.ButtonImage(new Rect(x + 100, y - 20 + num, 20f, 20f), (selectedGender2 == Gender.Female) ? "bfemale" : "bmale", AOnGenderChange2);
						}
						if (showPawn3)
						{
							SZWidgets.LabelBackground(new Rect(x + 150, y + num - 20, 110f, 20f), GetLabelSelectedPawn3(selRelation3), ColorTool.colDarkDimGray);
							SZWidgets.FloatMenuOnLabelAndImage(new Rect(x + 150, y + num, 110f, 100f), ColorTool.colDarkDimGray, "bplus2", SelectedPawn3, ColorTool.colDimGray, lOfRelations, FGetRelationLabel3, selRelation3, null, AOnParent3Selected, allowGender3);
							if (allowGender3)
							{
								SZWidgets.ButtonImage(new Rect(x + 240, y - 20 + num, 20f, 20f), (selectedGender3 == Gender.Female) ? "bfemale" : "bmale", AOnGenderChange3);
							}
						}
						if (showPawn4)
						{
							SZWidgets.LabelBackground(new Rect(x + 290, y + num - 20, 110f, 20f), GetLabelSelectedPawn4(selRelation4), ColorTool.colDarkDimGray);
							SZWidgets.FloatMenuOnLabelAndImage(new Rect(x + 290, y + num, 110f, 100f), ColorTool.colDarkDimGray, "bplus2", SelectedPawn4, ColorTool.colDimGray, lOfRelations, FGetRelationLabel4, selRelation4, null, AOnParent4Selected, allowGender4);
							if (allowGender4)
							{
								SZWidgets.ButtonImage(new Rect(x + 380, y - 20 + num, 20f, 20f), (selectedGender4 == Gender.Female) ? "bfemale" : "bmale", AOnGenderChange4);
							}
						}
						if (canAddRelation)
						{
							SZWidgets.ButtonImage(new Rect(x + 120, y + 35, 20f, 20f), "bplus2", AAddIndirectRelation);
						}
					}
					else
					{
						if (API.Pawn == SelectedPawn)
						{
							SelectedPawn = null;
						}
						if (SelectedPawn != null)
						{
							SZWidgets.ButtonImage(new Rect(x + 120, y + 35, 20f, 20f), "bplus2", AAddRelation);
						}
					}
				}
				CreateDirect(x + 5, y + num + 110, 190, h - 130);
				CreateIndirect(x + 200, y + num + 110, 190, h - 130);
			}

			private void AOnParent2Selected()
			{
				WindowTool.Open(new DialogChoosePawn(this, 2, selectedGender2));
			}

			private void AOnParent3Selected()
			{
				WindowTool.Open(new DialogChoosePawn(this, 3, selectedGender3));
			}

			private void AOnParent4Selected()
			{
				WindowTool.Open(new DialogChoosePawn(this, 4, selectedGender4));
			}

			private void DrawUpper(int x, int y, int w, int h)
			{
				if (!InStartingScreen)
				{
					if (API.Pawn.MentalState == null)
					{
						selectedMentalState = null;
					}
					else if (API.Pawn.MentalState != null && API.Pawn.MentalState.def != selectedMentalState)
					{
						selectedMentalState = API.Pawn.MentalState.def;
					}
					if (API.Pawn.Inspiration == null)
					{
						selectedInspiration = null;
					}
					else if (API.Pawn.Inspiration != null && API.Pawn.Inspiration.def != selectedInspiration)
					{
						selectedInspiration = API.Pawn.Inspiration.def;
					}
					SZWidgets.FloatMenuOnButtonImage(new Rect(x + 400 - 35, y, 30f, 30f), ContentFinder<Texture2D>.Get("bmental"), lOfMentalStates, FGetMentalLabel, AChangeMentalState, API.Pawn.GetMentalStateTooltip());
					SZWidgets.FloatMenuOnLabel(new Rect(x + 400 - 195, y, 160f, 30f), ColorTool.colDarkDimGray, lOfMentalStates, FGetMentalLabel, selectedMentalState, AChangeMentalState, API.Pawn.GetMentalStateTooltip());
					SZWidgets.FloatMenuOnButtonImage(new Rect(x + 400 - 35, y + 33, 30f, 30f), ContentFinder<Texture2D>.Get("binspire"), lOfInspirations, FGetInspirationLabel, AChangeInspiration, API.Pawn.GetMentalStateTooltip());
					SZWidgets.FloatMenuOnLabel(new Rect(x + 400 - 195, y + 33, 160f, 30f), ColorTool.colDarkDimGray, lOfInspirations, FGetInspirationLabel, selectedInspiration, AChangeInspiration, API.Pawn.GetInspirationTooltip());
				}
				SZWidgets.ButtonImage(new Rect(x + 400 - 70, y + 66, 30f, 30f), "bminus2", AToggleRemove);
				if (selectedPawnToSwap != null)
				{
					SZWidgets.ButtonImage(new Rect(x + 400 - 105, y + 66, 30f, 30f), "UI/Buttons/DragHash", ASelectOtherPawn, Label.SWAP_TO_PAWN);
				}
				SZWidgets.ButtonImage(new Rect(x + 400 - 35, y + 66, 30f, 30f), "bmemory", AAddThought);
			}
		}

		private class BlockPerson
		{
			private Vector2 scrollPos;

			private bool isPrimaryColor;

			private bool isPrimarySkinColor;

			private Apparel apparelCurrent;

			private bool bIsBodyOpen;

			private bool bIsHairOpen;

			private bool bIsHeadOpen;

			private bool bShowClothes;

			private bool bShowHat;

			private object deftest;

			private bool isAlien;

			private bool isApparelConfigOpen;

			private bool isBeardConfigOpen;

			private bool isBodyTattooConfigOpen;

			private bool isEyeConfigOpen;

			private bool isEyeConfigOpen2;

			private bool isFaceTattooConfigOpen;

			private bool isHaircolorConfigOpen;

			private bool isHairConfigOpen;

			private bool isSkinConfigOpen;

			private bool isWeaponConfigOpen;

			private int iTickRemoveDeadLogo;

			private int iTickTool;

			private HashSet<BeardDef> lOfBeardDefs;

			private HashSet<HairDef> lOfHairDefs;

			private Color randomDeadColor;

			private string selectedModName;

			private ThingWithComps weaponCurrent;

			private int x;

			private int y;

			private int w;

			private int h;

			private int iRemSlot = 0;

			private Rect RectFullSolid => new Rect(x, y, w, 24f);

			internal BlockPerson()
			{
				iTickRemoveDeadLogo = 0;
				iTickTool = 0;
				bIsBodyOpen = false;
				bIsHairOpen = false;
				IsRaceSpecificHead = false;
				bIsHeadOpen = false;
				bShowClothes = true;
				bShowHat = true;
				isAlien = false;
				isApparelConfigOpen = false;
				isBeardConfigOpen = false;
				isBodyTattooConfigOpen = false;
				isEyeConfigOpen = false;
				isEyeConfigOpen2 = false;
				isFaceTattooConfigOpen = false;
				isHaircolorConfigOpen = false;
				isHairConfigOpen = false;
				isPrimaryColor = true;
				isPrimarySkinColor = true;
				isSkinConfigOpen = false;
				isWeaponConfigOpen = false;
				randomDeadColor = Color.clear;
				apparelCurrent = null;
				weaponCurrent = null;
				selectedModName = null;
				lOfHairDefs = HairTool.GetHairList(selectedModName);
				lOfBeardDefs = StyleTool.GetBeardList(selectedModName);
				TattooDefOf.NoTattoo_Face.noGraphic = true;
				TattooDefOf.NoTattoo_Face.texPath = "bclear";
				TattooDefOf.NoTattoo_Body.noGraphic = true;
				TattooDefOf.NoTattoo_Body.texPath = "bclear";
				BeardDefOf.NoBeard.noGraphic = true;
				BeardDefOf.NoBeard.texPath = "bclear";
			}

			internal void Draw(coord c)
			{
				x = c.x;
				y = c.y;
				w = c.w;
				h = c.h;
				if (API.Pawn != null)
				{
					isAlien = API.Pawn.IsAlienRace();
					apparelCurrent = API.Pawn.ThisOrFirstWornApparel(apparelCurrent);
					weaponCurrent = API.Pawn.ThisOrFirstWeapon(weaponCurrent);
					DrawTop();
					DrawImage();
					DrawMainIcons();
					y += 300;
					DrawHaare();
					DrawHead();
					DrawBody();
					DrawWeaponSelector();
					DrawApparelSelector();
					DrawDeadLogo();
				}
			}

			private void DrawApparelSelector()
			{
				if (!API.Pawn.HasApparelTracker() || API.Pawn.apparel.WornApparel.NullOrEmpty())
				{
					return;
				}
				try
				{
					List<Apparel> list = API.Pawn.apparel.WornApparel.OrderByDescending((Apparel ap) => GetDrawOrder(ap)).ToList();
					for (int num = 0; num < list.Count; num++)
					{
						Apparel apparel = list[num];
						SZWidgets.NavSelectorImageBox2(RectFullSolid, apparel, AChangeApparelUI, ARandomApparel, ASetPrevApparel, ASetNextApparel, AOnTextureApparel, AConfigApparelcolor, apparel.def.label.CapitalizeFirst(), apparel.GetTooltip().text, Label.TIP_RANDOM_APPAREL);
						y += 30;
						if (isApparelConfigOpen && apparelCurrent == apparel && apparel.TryGetComp<CompColorable>() != null)
						{
							SZWidgets.ColorBox(new Rect(x + 25, y, w - 50, 95f), apparel.DrawColor, AOnApparelColorChanged, halfAlfa: true);
							y += 95;
						}
					}
				}
				catch
				{
				}
			}

			private void DrawBody()
			{
				if (API.Pawn.HasStoryTracker())
				{
					SZWidgets.NavSelectorImageBox(RectFullSolid, AOnBodyOpen, null, null, null, null, AOnBodyOpen, BodyPartDefOf.Torso.label.CapitalizeFirst(), null, null, null, null, ColorTool.colBeige);
					y += 30;
					if (bIsBodyOpen)
					{
						DrawBodySelector();
						DrawSkinColorSelector();
						DrawBodyTattooSelector();
					}
				}
			}

			private void DrawDeadLogo()
			{
				if (API.GetO(OptionB.SHOWDEADLOGO))
				{
					if (API.Pawn != null && API.Pawn.Dead && iTickRemoveDeadLogo == 0)
					{
						GUI.color = randomDeadColor;
						SZWidgets.ButtonImage(new Rect(x + 45, 60f, 150f, 150f), "bdead", ARemoveDeadLogo);
					}
					else if (iTickRemoveDeadLogo > 0)
					{
						iTickRemoveDeadLogo--;
					}
				}
			}

			private void DrawHaare()
			{
				if (API.Pawn.HasStoryTracker())
				{
					SZWidgets.NavSelectorImageBox(RectFullSolid, AOnHairOpen, null, null, null, null, AOnHairOpen, Label.HAIR, null, null, null, null, ColorTool.colBeige);
					y += 30;
					if (bIsHairOpen)
					{
						DrawHairSelector();
						DrawBeardSelector();
						DrawHairColorSelector();
						DrawGradientSelector();
					}
				}
			}

			private void DrawHead()
			{
				if (!API.Pawn.HasStoryTracker())
				{
					return;
				}
				SZWidgets.NavSelectorImageBox(RectFullSolid, AOnHeadOpen, null, null, null, null, AOnHeadOpen, BodyPartDefOf.Head.label.CapitalizeFirst(), null, null, null, null, ColorTool.colBeige);
				y += 30;
				if (bIsHeadOpen)
				{
					DrawFaceTattooSelector();
					deftest = API.Pawn.FA_GetCurrentDef("HeadControllerComp");
					if (deftest == null)
					{
						SZWidgets.NavSelectorImageBox(RectFullSolid, AChooseHeadCustom, ARandomHead, ASetPrevHead, ASetNextHead, null, null, BodyPartDefOf.Head.label.CapitalizeFirst() + ": " + API.Pawn.GetHeadName(), null, Label.TIP_RANDOM_HEAD);
						y += 30;
						return;
					}
					SZWidgets.NavSelectorImageBox(RectFullSolid, AFACustomHead, AFARandomHead, AFASetPrevHead, AFASetNextHead, null, null, BodyPartDefOf.Head.label.CapitalizeFirst() + ": " + API.Pawn.FA_GetCurrentDefName("HeadControllerComp"), null, Label.TIP_RANDOM_HEAD);
					y += 30;
					SZWidgets.NavSelectorImageBox(RectFullSolid, AFACustomEye, AFARandomEye, AFASetPrevEye, AFASetNextEye, null, null, Label.HB_Eye + ": " + API.Pawn.FA_GetCurrentDefName("EyeballControllerComp"));
					y += 30;
					DrawEyeColorSelection();
					SZWidgets.NavSelectorImageBox(RectFullSolid, AFACustomLid, AFARandomLid, AFASetPrevLid, AFASetNextLid, null, null, Label.LID + ": " + API.Pawn.FA_GetCurrentDefName("LidControllerComp"));
					y += 30;
					SZWidgets.NavSelectorImageBox(RectFullSolid, AFACustomBrow, AFARandomBrow, AFASetPrevBrow, AFASetNextBrow, null, null, Label.BROW + ": " + API.Pawn.FA_GetCurrentDefName("BrowControllerComp"));
					y += 30;
					SZWidgets.NavSelectorImageBox(RectFullSolid, AFACustomMouth, AFARandomMouth, AFASetPrevMouth, AFASetNextMouth, null, null, Label.MOUTH + ": " + API.Pawn.FA_GetCurrentDefName("MouthControllerComp"));
					y += 30;
					SZWidgets.NavSelectorImageBox(RectFullSolid, AFACustomSkin, AFARandomSkin, AFASetPrevSkin, AFASetNextSkin, null, null, Label.SKIN + API.Pawn.FA_GetCurrentDefName("SkinControllerComp"));
					y += 30;
				}
			}

			private void DrawImage()
			{
				SZWidgets.ButtonImage(x, y + 200, 22f, 22f, "bbackward", AChoosePrevPawn);
				SZWidgets.ButtonImage(x + w - 22, y + 200, 22f, 22f, "bforward", AChooseNextPawn);
				SZWidgets.ButtonInvisible(new Rect(x + 30, 60f, w - 60, 85f), AChangeHairUI);
				SZWidgets.ButtonInvisibleVar<Apparel>(new Rect(x + 30, 145f, w - 60, 100f), AChangeApparelUI, null);
				GUI.color = Color.white;
				API.Get<Capturer>(EType.Capturer).DrawPawnImage(API.Pawn, x + 21, y + 20);
			}

			private void DrawMainIcons()
			{
				SZWidgets.ButtonImage(x + 2, 260f, 24f, 24f, "UI/Buttons/Dev/Reload", ALoadPawn, Label.TIP_LOAD_PAWN_FROM_SLOT);
				SZWidgets.ButtonImage(x + 28, 260f, 24f, 24f, "bsave", ASavePawn, Label.TIP_SAVE_PAWN_TO_SLOT);
				if (!InStartingScreen)
				{
					SZWidgets.ButtonImage(x + 56, 260f, 24f, 24f, "UI/Buttons/DragHash", AJumpToPawn);
				}
				if (!InStartingScreen)
				{
					SZWidgets.ButtonImage(x + 84, 260f, 24f, 24f, "bteleport", ABeginTeleportSelectPawn, Label.TIP_TELEPORT);
				}
				if (isAlien)
				{
					SZWidgets.ButtonImage(x + 109, 260f, 26f, 26f, "bheadaddon", AChangeHeadAddons);
				}
				Pawn pawn = API.Pawn;
				if (pawn != null && pawn.gender == Gender.Male)
				{
					if (Widgets.ButtonImage(new Rect(x + 132, 259f, 28f, 28f), ContentFinder<Texture2D>.Get("bmale")))
					{
						API.Pawn.SetPawnGender(Gender.Female);
					}
				}
				else
				{
					Pawn pawn2 = API.Pawn;
					if (pawn2 != null && pawn2.gender == Gender.Female)
					{
						if (Widgets.ButtonImage(new Rect(x + 132, 259f, 28f, 28f), ContentFinder<Texture2D>.Get("bfemale")))
						{
							Pawn pawn3 = API.Pawn;
							Pawn pawn4 = API.Pawn;
							pawn3.SetPawnGender((pawn4 == null || pawn4.RaceProps?.hasGenders != false) ? Gender.Male : Gender.None);
						}
					}
					else
					{
						Pawn pawn5 = API.Pawn;
						if (pawn5 != null && pawn5.gender == Gender.None && Widgets.ButtonImage(new Rect(x + 132, 259f, 28f, 28f), ContentFinder<Texture2D>.Get("bnone")))
						{
							API.Pawn.SetPawnGender(Gender.Male);
						}
					}
				}
				SZWidgets.ButtonImageCol(x + 160, 262f, 24f, 24f, "bnoclothes", AToggleNude, bShowClothes ? Color.white : Color.grey);
				SZWidgets.ButtonImageCol(x + 184, 260f, 24f, 24f, "bnohats", AToggleHats, bShowHat ? Color.white : Color.grey);
				SZWidgets.ButtonImageCol(x + 210, 262f, 24f, 24f, "brotate", ARotate, Color.white);
				if (IsPsychologyActive)
				{
					SZWidgets.ButtonImage(x + 210, 232f, 24f, 24f, "bpsychology", AStartPsychologyUI);
				}
				if (IsFacialStuffActive)
				{
					SZWidgets.ButtonImage(x + 2, 232f, 24f, 24f, "bfacial", AStartFacialEditorUI);
				}
			}

			private void DrawTop()
			{
				Text.Font = GameFont.Small;
				Widgets.Label(new Rect(x + 5, y + 3, w, 24f), API.Pawn.GetPawnName());
				y += 2;
				if (IsRandom)
				{
					Widgets.DrawBoxSolid(new Rect(x, y - 2, x + w - 10, 26f), new Color(0.2f, 0.2f, 0.2f));
					Rect rect = new Rect(x + w - 238, y + 25, 24f, 24f);
					SZWidgets.ButtonImageCol(rect, "bconfig", delegate
					{
						API.ConfigEditor();
					}, Color.white);
					Rect rect2 = new Rect(x + w - 23, y + 25, 24f, 24f);
					SZWidgets.ButtonImageCol(rect2, "bplus2", AAddPawn, Color.white, Label.TIP_ADD_PAWN);
					Rect rect3 = new Rect(x + w - 23, y + 52, 24f, 24f);
					SZWidgets.ButtonImageCol(rect3, "bminus2", ARemovePawn, Color.white, Label.TIP_DELETE_PAWN);
					Rect rect4 = new Rect(x + w - 49, y, 22f, 22f);
					SZWidgets.ButtonImageCol(rect4, "brandom", ARandomizePawn, Color.white, (iTickTool <= 0) ? Label.TIP_RANDOMIZE_PAWN : "");
					Rect rect5 = new Rect(x + w - 76, y, 22f, 22f);
					SZWidgets.ButtonImageCol(rect5, "breplace", ARandomizePawnKeepRace, Color.white, (iTickTool <= 0) ? Label.TIP_RANDOMIZE_PAWNKEEPRACE : "");
					Rect rect6 = new Rect(x + w - 103, y, 22f, 22f);
					SZWidgets.ButtonImageCol(rect6, "bgender", ARandomizeBodyParts, Color.white, (iTickTool <= 0) ? Label.TIP_RANDOMIZE_BODYPARTS : "");
					Rect rect7 = new Rect(x + w - 130, y, 22f, 22f);
					SZWidgets.ButtonImageCol(rect7, "barmory", ARandomizeEquip, Color.white, (iTickTool <= 0) ? Label.TIP_RANDOMIZE_EQUIP : "");
					Rect rect8 = new Rect(x + w - 157, y, 22f, 22f);
					SZWidgets.ButtonImageCol(rect8, "bskills", ARandomizeBio, Color.white, (iTickTool <= 0) ? Label.TIP_RANDOMIZE_BIO : "");
					Rect rect9 = new Rect(x + w - 184, y, 22f, 22f);
					SZWidgets.ButtonImageCol(rect9, "bresurrect", AQuickResurrect, Color.white, (iTickTool <= 0) ? Label.TIP_QUICKHEAL : "");
					Rect rect10 = new Rect(x + w - 211, y, 22f, 22f);
					SZWidgets.ButtonImage(rect10, "bclone", AClonePawn, InStartingScreen ? Label.TIP_CLONE.SubstringTo("\n") : Label.TIP_CLONE);
					Rect rect11 = new Rect(x + w - 238, y, 22f, 22f);
					SZWidgets.ButtonImage(rect11, "UI/Buttons/DevRoot/OpenInspector", AFindPawn);
					if (iTickTool > 0)
					{
						iTickTool--;
					}
				}
				SZWidgets.ButtonImage(x + w - 24, y, 24f, 24f, IsRandom ? "bractive" : "brinactive", AToggleR, Label.SHOWCREATION);
				if (InStartingScreen)
				{
					SZWidgets.ButtonImage(x, y + 50, 24f, 24f, "bmoveup", AMoveUp);
					SZWidgets.ButtonImage(x, y + 75, 24f, 24f, "bmovedown", AMoveDown);
				}
			}

			private void AAddPawn()
			{
				Faction value = API.DicFactions.GetValue(ListName);
				string raceDefName = ((RACE != null) ? RACE.defName : null);
				List<PawnKindDef> lpkd = PawnKindTool.ListOfPawnKindDefByRaceName(value, ListName, selectedModName, raceDefName);
				PawnxTool.AddOrCreateNewPawn(PKD.ThisOrFromList(lpkd), value, RACE, null);
			}

			private void ARemovePawn()
			{
				API.Pawn.Delete();
			}

			private void ABeginTeleportSelectPawn()
			{
				API.EditorMoveRight();
				if (Event.current.control)
				{
					PlacingTool.BeginTeleportCustomPawn();
				}
				else
				{
					PlacingTool.BeginTeleportPawn(API.Pawn);
				}
			}

			private void AClonePawn()
			{
				PresetPawn ppn = API.Pawn.ClonePawn();
				PawnxTool.AddOrCreateExistingPawn(ppn);
			}

			private void AMoveDown()
			{
				int num = API.ListOf<Pawn>(EType.Pawns).IndexOf(API.Pawn);
				int num2 = API.ListOf<Pawn>(EType.Pawns).Count - 1;
				num = ((num < num2) ? (num + 1) : num2);
				API.ListOf<Pawn>(EType.Pawns).Remove(API.Pawn);
				API.ListOf<Pawn>(EType.Pawns).Insert(num, API.Pawn);
				PawnxTool.Notify_CheckStartPawnsListChanged();
			}

			private void AMoveUp()
			{
				int num = API.ListOf<Pawn>(EType.Pawns).IndexOf(API.Pawn);
				num = ((num > 0) ? (num - 1) : 0);
				API.ListOf<Pawn>(EType.Pawns).Remove(API.Pawn);
				API.ListOf<Pawn>(EType.Pawns).Insert(num, API.Pawn);
				PawnxTool.Notify_CheckStartPawnsListChanged();
			}

			private void AQuickResurrect()
			{
				if (Event.current.alt)
				{
					HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(API.Pawn);
				}
				if (Event.current.shift)
				{
					API.Pawn.Medicate();
				}
				if (Event.current.control)
				{
					API.Pawn.Anaesthetize();
				}
				if (Event.current.capsLock)
				{
					API.Pawn.DamageUntilDeath();
				}
				if (!Event.current.alt && !Event.current.shift && !Event.current.control && !Event.current.capsLock)
				{
					API.Pawn.ResurrectAndHeal();
				}
				API.UpdateGraphics();
			}

			private void ARandomizeBio()
			{
				iTickTool = 100;
				API.Pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(API.Pawn);
				API.Pawn.SetBackstory(next: true, random: true, isChildhood: true, notDisabled: false);
				API.Pawn.SetBackstory(next: true, random: true, isChildhood: false, notDisabled: false);
				API.Pawn.SetAge(zufallswert.Next(API.Pawn.RaceProps.Animal ? 14 : 70));
				API.Pawn.SetChronoAge(zufallswert.Next(2000));
				if (API.Pawn.skills != null)
				{
					foreach (SkillRecord skill in API.Pawn.skills.skills)
					{
						if (!skill.TotallyDisabled)
						{
							int maxValue = zufallswert.Next(0, 21);
							skill.Level = zufallswert.Next(0, maxValue);
							skill.passion = (Passion)zufallswert.Next(0, 3);
						}
					}
				}
				if (API.Pawn.story != null)
				{
					API.Pawn.story.traits.allTraits.Clear();
					int num = zufallswert.Next(0, 11);
					for (int i = 0; i < num; i++)
					{
						API.Pawn.AddTrait(null, null, random: true, doChangeSkillValue: true);
					}
				}
			}

			private void ARandomizeBodyParts()
			{
				iTickTool = 100;
				if (Event.current.alt)
				{
					API.Pawn.gender = Gender.Female;
				}
				else if (Event.current.capsLock)
				{
					API.Pawn.gender = Gender.Male;
				}
				else
				{
					API.Pawn.gender = ((zufallswert.Next(100) > 50) ? Gender.Male : Gender.Female);
				}
				API.Pawn.SetHead(next: true, random: true);
				API.Pawn.SetBody(next: true, random: true);
				if (API.Pawn.story != null)
				{
					if (Event.current.alt || Event.current.capsLock)
					{
						PawnxTool.ForceGenderizedBodyTypeDef(API.Pawn.gender);
					}
					if (!API.Pawn.story.bodyType.IsFromMod("Alien Vs Predator"))
					{
						API.Pawn.SetHair(next: true, random: true);
						ARandomHairColor();
					}
				}
				if (API.Pawn.HasStyleTracker())
				{
					int num = zufallswert.Next(100);
					if (num > 50 || API.Pawn.gender == Gender.Female)
					{
						API.Pawn.SetBeard(BeardDefOf.NoBeard);
					}
					else
					{
						API.Pawn.SetBeard(next: true, random: true);
					}
					API.Pawn.SetFaceTattoo(next: true, random: true);
					API.Pawn.SetBodyTattoo(next: true, random: true);
				}
				API.UpdateGraphics();
			}

			private void ARandomizeEquip()
			{
				iTickTool = 100;
				bool flag = !Event.current.alt && !Event.current.shift;
				if (Event.current.alt || flag)
				{
					API.Pawn.Redress(null, originalColors: false, -1);
				}
				if (Event.current.shift || flag)
				{
					API.Pawn.Reequip(null, -1);
				}
				if (Event.current.control)
				{
					Pawn_ApparelTracker apparel = API.Pawn.apparel;
					if (apparel != null && apparel.WornApparelCount > 0)
					{
						foreach (Apparel item in API.Pawn.apparel.WornApparel)
						{
							item.DrawColor = ColorTool.RandomAlphaColor;
						}
					}
				}
				API.UpdateGraphics();
			}

			private void ARandomizePawn()
			{
				iTickTool = 100;
				Faction value = API.DicFactions.GetValue(ListName);
				IntVec3 position = API.Pawn.Position;
				API.Pawn.Delete();
				PawnxTool.AddOrCreateNewPawn(PKD.ThisOrFromList(GetPawnKindDefs()), value, RACE, null, position);
			}

			private void ARandomizePawnKeepRace()
			{
				iTickTool = 100;
				Faction value = API.DicFactions.GetValue(ListName);
				if (Event.current.alt)
				{
					PawnxTool.ReplacePawnWithPawnOfSameRace(Gender.Female, value);
				}
				else if (Event.current.capsLock)
				{
					PawnxTool.ReplacePawnWithPawnOfSameRace(Gender.Male, value);
				}
				else
				{
					PawnxTool.ReplacePawnWithPawnOfSameRace(Gender.None, value);
				}
			}

			private void AToggleR()
			{
				IsRandom = !IsRandom;
			}

			private List<PawnKindDef> GetPawnKindDefs()
			{
				return PawnKindTool.ListOfPawnKindDef(API.DicFactions.GetValue(ListName), ListName, selectedModName);
			}

			private void AChooseNextPawn()
			{
				int index = API.ListOf<Pawn>(EType.Pawns).IndexOf(API.Pawn);
				index = API.ListOf<Pawn>(EType.Pawns).NextOrPrevIndex(index, next: true, random: false);
				API.Pawn = API.ListOf<Pawn>(EType.Pawns)[index];
			}

			private void AChoosePrevPawn()
			{
				int index = API.ListOf<Pawn>(EType.Pawns).IndexOf(API.Pawn);
				index = API.ListOf<Pawn>(EType.Pawns).NextOrPrevIndex(index, next: false, random: false);
				API.Pawn = API.ListOf<Pawn>(EType.Pawns)[index];
			}

			private void AChangeHeadAddons()
			{
				WindowTool.Open(new DialogChangeHeadAddons());
			}

			private void AFindPawn()
			{
				WindowTool.Open(new DialogFindPawn());
			}

			private void AJumpToPawn()
			{
				API.Get<EditorUI>(EType.EditorUI).windowRect.position = new Vector2(UI.screenWidth / 2 + 200, (float)(UI.screenHeight / 2) - API.Get<EditorUI>(EType.EditorUI).InitialSize.y / 2f);
				CameraJumper.TryJumpAndSelect(API.Pawn);
			}

			private void ALoadPawn()
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				int numSlots = API.NumSlots;
				for (int i = 0; i < numSlots; i++)
				{
					int currentSlot = i;
					FloatMenuOption item = new FloatMenuOption(SlotLabel(currentSlot, isSave: false), delegate
					{
						if (Event.current.alt)
						{
							PresetPawn presetPawn = new PresetPawn();
							presetPawn.Load(currentSlot, "");
							MessageTool.Show(presetPawn.ShowPawnMods());
						}
						else
						{
							int slot = currentSlot;
							PresetPawn presetPawn2 = new PresetPawn();
							presetPawn2.LoadPawn(slot, choosePlace: true);
						}
					});
					list.Add(item);
				}
				WindowTool.Open(new FloatMenu(list));
			}

			private void ARotate(Color col)
			{
				API.Get<Capturer>(EType.Capturer).RotateAndCapture(API.Pawn);
			}

			private void ASavePawn()
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				int numSlots = API.NumSlots;
				for (int i = 0; i < numSlots; i++)
				{
					int currentSlot = i;
					list.Add(new FloatMenuOption(SlotLabel(currentSlot, isSave: true), delegate
					{
						int i2 = currentSlot;
						if (Event.current.control)
						{
							API.SetSlot(i2, "", andSave: true);
						}
						else
						{
							iRemSlot = i2;
							string slot = API.GetSlot(i2);
							if (!slot.NullOrEmpty())
							{
								MessageTool.ShowCustomDialog("Data: " + slot, Label.OVERWRITE_EXISTING, null, AConfirmSavePawn, null);
							}
							else
							{
								AConfirmSavePawn();
							}
						}
					}));
				}
				WindowTool.Open(new FloatMenu(list));
			}

			private void AConfirmSavePawn()
			{
				PresetPawn presetPawn = new PresetPawn();
				presetPawn.SavePawn(API.Pawn, iRemSlot);
			}

			private void AStartFacialEditorUI()
			{
				WindowTool.Open(new DialogFacialStuff());
			}

			private void AStartPsychologyUI()
			{
				WindowTool.Open(new DialogPsychology());
			}

			private void AToggleHats(Color col)
			{
				Capturer capturer = API.Get<Capturer>(EType.Capturer);
				if (capturer.bHats)
				{
					List<ApparelLayerDef> source = ApparelTool.ListOfApparelLayerDefs(insertNull: false);
					source = source.Where((ApparelLayerDef td) => !td.defName.NullOrEmpty() && td.defName.ToLower().Contains("head")).ToList();
					foreach (ApparelLayerDef item in source)
					{
						API.Pawn.MoveDressToInv(item);
					}
				}
				else
				{
					List<ApparelLayerDef> source2 = ApparelTool.ListOfApparelLayerDefs(insertNull: false);
					source2 = source2.Where((ApparelLayerDef td) => !td.defName.NullOrEmpty() && td.defName.ToLower().Contains("head")).ToList();
					foreach (ApparelLayerDef item2 in source2)
					{
						API.Pawn.MoveDressFromInv(item2);
					}
				}
				capturer.ToggleHatAndCapture(API.Pawn);
				bShowHat = capturer.bHats;
			}

			private void AToggleNude(Color col)
			{
				Capturer capturer = API.Get<Capturer>(EType.Capturer);
				if (capturer.bNude)
				{
					API.Pawn.MoveDressToInv(null);
				}
				else
				{
					API.Pawn.MoveDressFromInv(null);
				}
				capturer.ToggleNudeAndCapture(API.Pawn);
				bShowClothes = capturer.bNude;
			}

			private string SlotLabel(int slot, bool isSave)
			{
				string text = (isSave ? Label.SAVESLOT : Label.LOADSLOT) + slot + " ";
				string slot2 = API.GetSlot(slot);
				if (slot2.Contains("*"))
				{
					return text + slot2.SubstringTo("*");
				}
				return text + slot2.SubstringTo(",");
			}

			private void AOnHeadOpen()
			{
				bIsHeadOpen = !bIsHeadOpen;
			}

			private void AOnHeadOpen2()
			{
				IsRaceSpecificHead = !IsRaceSpecificHead;
			}

			private void AFACustomSkin()
			{
				SZWidgets.FloatMenuOnRect(API.Pawn.FA_GetDefStringList("SkinControllerComp"), (string s) => s, AFASetCustomSkin);
			}

			private void AFARandomSkin()
			{
				API.Pawn.FA_SetDef("SkinControllerComp", next: true, random: true);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetCustomSkin(string val)
			{
				API.Pawn.FA_SetDefByName("SkinControllerComp", val);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetNextSkin()
			{
				API.Pawn.FA_SetDef("SkinControllerComp", next: true, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetPrevSkin()
			{
				API.Pawn.FA_SetDef("SkinControllerComp", next: false, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFACustomMouth()
			{
				SZWidgets.FloatMenuOnRect(API.Pawn.FA_GetDefStringList("MouthControllerComp"), (string s) => s, AFASetCustomMouth);
			}

			private void AFARandomMouth()
			{
				API.Pawn.FA_SetDef("MouthControllerComp", next: true, random: true);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetCustomMouth(string val)
			{
				API.Pawn.FA_SetDefByName("MouthControllerComp", val);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetNextMouth()
			{
				API.Pawn.FA_SetDef("MouthControllerComp", next: true, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetPrevMouth()
			{
				API.Pawn.FA_SetDef("MouthControllerComp", next: false, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFACustomBrow()
			{
				SZWidgets.FloatMenuOnRect(API.Pawn.FA_GetDefStringList("BrowControllerComp"), (string s) => s, AFASetCustomBrow);
			}

			private void AFARandomBrow()
			{
				API.Pawn.FA_SetDef("BrowControllerComp", next: true, random: true);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetCustomBrow(string val)
			{
				API.Pawn.FA_SetDefByName("BrowControllerComp", val);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetNextBrow()
			{
				API.Pawn.FA_SetDef("BrowControllerComp", next: true, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetPrevBrow()
			{
				API.Pawn.FA_SetDef("BrowControllerComp", next: false, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFACustomLid()
			{
				SZWidgets.FloatMenuOnRect(API.Pawn.FA_GetDefStringList("LidControllerComp"), (string s) => s, AFASetCustomLid);
			}

			private void AFARandomLid()
			{
				API.Pawn.FA_SetDef("LidControllerComp", next: true, random: true);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetCustomLid(string val)
			{
				API.Pawn.FA_SetDefByName("LidControllerComp", val);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetNextLid()
			{
				API.Pawn.FA_SetDef("LidControllerComp", next: true, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetPrevLid()
			{
				API.Pawn.FA_SetDef("LidControllerComp", next: false, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFACustomEye()
			{
				SZWidgets.FloatMenuOnRect(API.Pawn.FA_GetDefStringList("EyeballControllerComp"), (string s) => s, AFASetCustomEye);
			}

			private void AFARandomEye()
			{
				API.Pawn.FA_SetDef("EyeballControllerComp", next: true, random: true);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetCustomEye(string val)
			{
				API.Pawn.FA_SetDefByName("EyeballControllerComp", val);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetNextEye()
			{
				API.Pawn.FA_SetDef("EyeballControllerComp", next: true, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetPrevEye()
			{
				API.Pawn.FA_SetDef("EyeballControllerComp", next: false, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFACustomHead()
			{
				SZWidgets.FloatMenuOnRect(API.Pawn.FA_GetDefStringList("HeadControllerComp"), (string s) => s, AFASetCustomFace);
			}

			private void AFARandomHead()
			{
				API.Pawn.FA_SetDef("HeadControllerComp", next: true, random: true);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetCustomFace(string val)
			{
				API.Pawn.FA_SetDefByName("HeadControllerComp", val);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetNextHead()
			{
				API.Pawn.FA_SetDef("HeadControllerComp", next: true, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AFASetPrevHead()
			{
				API.Pawn.FA_SetDef("HeadControllerComp", next: false, random: false);
				API.Pawn.SetEyeColor(API.Pawn.GetEyeColor());
				API.UpdateGraphics();
			}

			private void AChooseHeadCustom()
			{
				SZWidgets.FloatMenuOnRect(API.Pawn.GetHeadDefList(), (HeadTypeDef s) => s.label.NullOrEmpty() ? s.defName : s.label.ToString(), ASetHeadDefCustom);
			}

			private void ARandomHead()
			{
				API.Pawn.SetHead(next: true, random: true);
				API.UpdateGraphics();
			}

			private void ASetHeadDefCustom(HeadTypeDef val)
			{
				API.Pawn.SetHeadTypeDef(val);
				API.UpdateGraphics();
			}

			private void ASetNextHead()
			{
				API.Pawn.SetHead(next: true, random: false);
				API.UpdateGraphics();
			}

			private void ASetPrevHead()
			{
				API.Pawn.SetHead(next: false, random: false);
				API.UpdateGraphics();
			}

			private void DrawEyeColorSelection()
			{
				SZWidgets.NavSelectorImageBox(RectFullSolid, AChangeEyeUI, ARandomEyeColor, null, null, null, AConfigEyeColor, Label.EYECOLOR);
				y += 30;
				if (isEyeConfigOpen)
				{
					SZWidgets.ColorBox(new Rect(x + 25, y, w - 50, 95f), API.Pawn.GetEyeColor(), AOnEyeColorChanged);
					y += 95;
				}
				SZWidgets.NavSelectorImageBox(RectFullSolid, AChangeEyeUI2, ARandomEyeColor2, null, null, null, AConfigEyeColor2, Label.EYECOLOR + "2");
				y += 30;
				if (isEyeConfigOpen2)
				{
					SZWidgets.ColorBox(new Rect(x + 25, y, w - 50, 95f), API.Pawn.GetEyeColor2(), AOnEyeColorChanged2);
					y += 95;
				}
			}

			private void AChangeEyeUI()
			{
				WindowTool.Open(new DialogColorPicker(ColorType.EyeColor));
			}

			private void AChangeEyeUI2()
			{
				WindowTool.Open(new DialogColorPicker(ColorType.EyeColor, _primaryColor: false));
			}

			private void AConfigEyeColor()
			{
				isEyeConfigOpen = !isEyeConfigOpen;
			}

			private void AConfigEyeColor2()
			{
				isEyeConfigOpen2 = !isEyeConfigOpen2;
			}

			private void AOnEyeColorChanged(Color col)
			{
				API.Pawn.SetEyeColor(col);
				API.UpdateGraphics();
			}

			private void AOnEyeColorChanged2(Color col)
			{
				API.Pawn.SetEyeColor2(col);
				API.UpdateGraphics();
			}

			private void ARandomEyeColor()
			{
				API.Pawn.SetEyeColor(ColorTool.RandomColor);
				API.UpdateGraphics();
			}

			private void ARandomEyeColor2()
			{
				API.Pawn.SetEyeColor2(ColorTool.RandomColor);
				API.UpdateGraphics();
			}

			private void DrawFaceTattooSelector()
			{
				if (API.Pawn.HasStyleTracker())
				{
					SZWidgets.NavSelectorImageBox(RectFullSolid, AChooseFaceTattooCustom, ARandomFaceTattoo, ASetPrevFaceTattoo, ASetNextFaceTattoo, null, null, Label.TATTOO + API.Pawn.GetFaceTattooName(), null, Label.TIP_RANDOM_FACETATTOO);
					y += 30;
				}
			}

			private void AChooseFaceTattooCustom()
			{
				SZWidgets.FloatMenuOnRect(StyleTool.GetFaceTattooList(null), (TattooDef s) => s.LabelCap, ASetFaceTattooCustom);
			}

			private void AConfigFaceTattoo()
			{
				isFaceTattooConfigOpen = !isFaceTattooConfigOpen;
			}

			private void AFaceTattooSelected(TattooDef tattooDef)
			{
				API.Pawn.SetFaceTattoo(tattooDef);
				API.UpdateGraphics();
			}

			private void ARandomFaceTattoo()
			{
				API.Pawn.SetFaceTattoo(next: true, random: true);
				API.UpdateGraphics();
			}

			private void ASetFaceTattooCustom(TattooDef tattooDef)
			{
				API.Pawn.SetFaceTattoo(tattooDef);
				API.UpdateGraphics();
			}

			private void ASetNextFaceTattoo()
			{
				API.Pawn.SetFaceTattoo(next: true, random: false);
				API.UpdateGraphics();
			}

			private void ASetPrevFaceTattoo()
			{
				API.Pawn.SetFaceTattoo(next: false, random: false);
				API.UpdateGraphics();
			}

			private void AOnHairOpen()
			{
				bIsHairOpen = !bIsHairOpen;
			}

			private void DrawBeardSelector()
			{
				if (!API.Pawn.HasStyleTracker())
				{
					return;
				}
				SZWidgets.NavSelectorImageBox(RectFullSolid, StyleTool.AChooseBeardCustom, StyleTool.ARandomBeard, ASetPrevBeard, ASetNextBeard, null, AConfigBeard, Label.BEARD + " - " + API.Pawn.GetBeardName(), null, Label.TIP_RANDOM_BEARD);
				y += 30;
				if (isBeardConfigOpen)
				{
					SZWidgets.FloatMenuOnButtonText(new Rect(x + 16, y, w - 32, 25f), selectedModName ?? Label.ALL, API.Get<HashSet<string>>(EType.ModsBeardDef), (string s) => s ?? Label.ALL, ASelectedModName);
					SZWidgets.ListView(new Rect(x + 16, y + 25, w - 32, 330f), lOfBeardDefs, (BeardDef hd) => hd.LabelCap, (BeardDef hd) => hd.description, (BeardDef beardA, BeardDef beardB) => beardA == beardB, ref API.Pawn.style.beardDef, ref scrollPos, withRemove: false, ABeardSelected);
					y += 357;
				}
			}

			private void ABeardSelected(BeardDef b)
			{
				API.Pawn.SetBeard(b);
				API.UpdateGraphics();
			}

			private void AConfigBeard()
			{
				isBeardConfigOpen = !isBeardConfigOpen;
			}

			private void ASetNextBeard()
			{
				API.Pawn.SetBeard(next: true, random: false);
				API.UpdateGraphics();
			}

			private void ASetPrevBeard()
			{
				API.Pawn.SetBeard(next: false, random: false);
				API.UpdateGraphics();
			}

			private void DrawGradientSelector()
			{
				if (IsGradientHairActive)
				{
					SZWidgets.NavSelectorImageBox(RectFullSolid, AChooseGradientCustom, ARandomGradient, ASetPrevGradient, ASetNextGradient, null, null, API.Pawn.GetGradientMask());
					y += 30;
				}
			}

			private void DrawHairColorSelector()
			{
				SZWidgets.NavSelectorImageBox(RectFullSolid, AChangeHairUI, ARandomHairColor, AToggleColor, AToggleColor, null, AConfigHaircolor, Label.HAIRCOLOR + " - " + (isPrimaryColor ? Label.COLORA : Label.COLORB), null, Label.TIP_RANDOM_HAIRCOLOR);
				y += 30;
				if (isHaircolorConfigOpen)
				{
					SZWidgets.ColorBox(new Rect(x + 25, y, w - 50, 95f), API.Pawn.GetHairColor(isPrimaryColor), AOnHairColorChanged);
					y += 95;
				}
			}

			private void DrawHairSelector()
			{
				SZWidgets.NavSelectorImageBox(RectFullSolid, HairTool.AChooseHairCustom, HairTool.ARandomHair, ASetPrevHair, ASetNextHair, null, AConfigHair, Label.FRISUR + " - " + API.Pawn.GetHairName(), null, Label.TIP_RANDOM_HAIR);
				y += 30;
				if (isHairConfigOpen)
				{
					SZWidgets.FloatMenuOnButtonText(new Rect(x + 16, y, w - 32, 25f), selectedModName ?? Label.ALL, API.Get<HashSet<string>>(EType.ModsHairDef), (string s) => s ?? Label.ALL, ASelectedModName);
					SZWidgets.ListView(new Rect(x + 16, y + 25, w - 32, 360f), lOfHairDefs, (HairDef hd) => hd.LabelCap, (HairDef hd) => hd.description, (HairDef hairA, HairDef hairB) => hairA == hairB, ref API.Pawn.story.hairDef, ref scrollPos, withRemove: false, AHairSelected);
					y += 387;
				}
			}

			private void AConfigHair()
			{
				isHairConfigOpen = !isHairConfigOpen;
			}

			private void AHairSelected(HairDef h)
			{
				API.Pawn.SetHair(h);
				API.UpdateGraphics();
			}

			private void ASelectedModName(string val)
			{
				selectedModName = val;
				lOfHairDefs = HairTool.GetHairList(selectedModName);
				lOfBeardDefs = StyleTool.GetBeardList(selectedModName);
			}

			private void ASetNextHair()
			{
				WindowTool.TryRemove<DialogColorPicker>();
				API.Pawn.SetHair(next: true, random: false);
				API.UpdateGraphics();
			}

			private void ASetPrevHair()
			{
				WindowTool.TryRemove<DialogColorPicker>();
				API.Pawn.SetHair(next: false, random: false);
				API.UpdateGraphics();
			}

			private void AChangeHairUI()
			{
				WindowTool.Open(new DialogColorPicker(ColorType.HairColor, isPrimaryColor));
			}

			private void AConfigHaircolor()
			{
				isHaircolorConfigOpen = !isHaircolorConfigOpen;
			}

			private void AOnHairColorChanged(Color col)
			{
				API.Pawn.SetHairColor(isPrimaryColor, col);
				API.UpdateGraphics();
			}

			private void ARandomHairColor()
			{
				bool flag = !Event.current.alt && !Event.current.shift;
				if (flag || Event.current.shift)
				{
					API.Pawn.SetHairColor(primary: false, Event.current.control ? ColorTool.RandomAlphaColor : ColorTool.RandomColor);
				}
				if (flag || Event.current.alt)
				{
					API.Pawn.SetHairColor(primary: true, Event.current.control ? ColorTool.RandomAlphaColor : ColorTool.RandomColor);
				}
				API.UpdateGraphics();
			}

			private void AToggleColor()
			{
				isPrimaryColor = !isPrimaryColor;
			}

			private void AChooseGradientCustom()
			{
				SZWidgets.FloatMenuOnRect(HairTool.GetAllGradientHairs(), (string s) => s, ASetHairCustom);
			}

			private void ARandomGradient()
			{
				API.Pawn.RandomizeGradientMask();
			}

			private void ASetHairCustom(string val)
			{
				API.Pawn.SetGradientMask(val);
			}

			private void ASetNextGradient()
			{
				List<string> allGradientHairs = HairTool.GetAllGradientHairs();
				int index = allGradientHairs.IndexOf(API.Pawn.GetGradientMask());
				index = allGradientHairs.NextOrPrevIndex(index, next: true, random: false);
				API.Pawn.SetGradientMask(allGradientHairs[index]);
			}

			private void ASetPrevGradient()
			{
				List<string> allGradientHairs = HairTool.GetAllGradientHairs();
				int index = allGradientHairs.IndexOf(API.Pawn.GetGradientMask());
				index = allGradientHairs.NextOrPrevIndex(index, next: false, random: false);
				API.Pawn.SetGradientMask(allGradientHairs[index]);
			}

			private void AOnBodyOpen()
			{
				bIsBodyOpen = !bIsBodyOpen;
			}

			private void DrawBodySelector()
			{
				SZWidgets.NavSelectorImageBox(RectFullSolid, AChooseBodyCustom, ARandomBody, ASetPrevBody, ASetNextBody, null, null, Label.FORM + ": " + API.Pawn.GetBodyTypeName(), null, Label.TIP_RANDOM_BODY);
				y += 30;
			}

			private void AChooseBodyCustom()
			{
				SZWidgets.FloatMenuOnRect(API.Pawn.GetBodyDefList(), (BodyTypeDef s) => s.defName.Translate(), ASetBodyCustom);
			}

			private void ARandomBody()
			{
				API.Pawn.SetBody(next: true, random: true);
				API.UpdateGraphics();
			}

			private void ASetBodyCustom(BodyTypeDef b)
			{
				API.Pawn.SetBody(b);
				API.UpdateGraphics();
			}

			private void ASetNextBody()
			{
				API.Pawn.SetBody(next: true, random: false);
				API.UpdateGraphics();
			}

			private void ASetPrevBody()
			{
				API.Pawn.SetBody(next: false, random: false);
				API.UpdateGraphics();
			}

			private void DrawBodyTattooSelector()
			{
				if (API.Pawn.HasStyleTracker())
				{
					SZWidgets.NavSelectorImageBox(RectFullSolid, AChooseBodyTattooCustom, ARandomBodyTattoo, ASetPrevBodyTattoo, ASetNextBodyTattoo, null, null, Label.TATTOO + API.Pawn.GetBodyTattooName(), null, Label.TIP_RANDOM_BODYTATTOO);
					y += 30;
				}
			}

			private void ABodyTattooSelected(TattooDef tattooDef)
			{
				API.Pawn.SetBodyTattoo(tattooDef);
				API.UpdateGraphics();
			}

			private void AChooseBodyTattooCustom()
			{
				SZWidgets.FloatMenuOnRect(StyleTool.GetBodyTattooList(null), (TattooDef s) => s.LabelCap, ASetBodyTattooCustom);
			}

			private void AConfigBodyTattoo()
			{
				isBodyTattooConfigOpen = !isBodyTattooConfigOpen;
			}

			private void ARandomBodyTattoo()
			{
				API.Pawn.SetBodyTattoo(next: true, random: true);
				API.UpdateGraphics();
			}

			private void ASetBodyTattooCustom(TattooDef tattooDef)
			{
				API.Pawn.SetBodyTattoo(tattooDef);
				API.UpdateGraphics();
			}

			private void ASetNextBodyTattoo()
			{
				API.Pawn.SetBodyTattoo(next: true, random: false);
				API.UpdateGraphics();
			}

			private void ASetPrevBodyTattoo()
			{
				API.Pawn.SetBodyTattoo(next: false, random: false);
				API.UpdateGraphics();
			}

			private void DrawSkinColorSelector()
			{
				SZWidgets.NavSelectorImageBox(RectFullSolid, AChangeSkinUI, ARandomSkinColor, AToggleSkinColor, AToggleSkinColor, null, AConfigSkincolor, Label.SKIN + (isPrimarySkinColor ? Label.COLORA : Label.COLORB), null, Label.TIP_RANDOM_SKINCOLOR);
				y += 30;
				if (isSkinConfigOpen)
				{
					SZWidgets.ColorBox(new Rect(x + 25, y, w - 50, 95f), API.Pawn.GetSkinColor(isPrimarySkinColor), AOnSkinColorChanged);
					y += 95;
				}
			}

			private void AChangeSkinUI()
			{
				WindowTool.Open(new DialogColorPicker(ColorType.SkinColor, isPrimaryColor));
			}

			private void AConfigSkincolor()
			{
				isSkinConfigOpen = !isSkinConfigOpen;
			}

			private void AOnMelaninChanged(float val)
			{
			}

			private void AOnSkinColorChanged(Color col)
			{
				API.Pawn.SetSkinColor(isPrimarySkinColor, col);
				API.UpdateGraphics();
			}

			private void AOnSkinColorChanged2(Color col)
			{
				API.Pawn.story.SkinColorBase = col;
				API.UpdateGraphics();
			}

			private void ARandomSkinColor()
			{
				bool flag = !Event.current.alt && !Event.current.shift;
				if (flag || Event.current.alt)
				{
					if (API.Pawn.story != null)
					{
						API.Pawn.story.SkinColorBase = ColorTool.RandomColor;
					}
					if (isAlien)
					{
						if (Event.current.control)
						{
							API.Pawn.SetSkinColor(primary: true, ColorTool.RandomAlphaColor);
						}
						else
						{
							API.Pawn.SetSkinColor(primary: true, ColorTool.RandomColor);
						}
					}
				}
				if ((flag || Event.current.shift) && isAlien)
				{
					if (Event.current.control)
					{
						API.Pawn.SetSkinColor(primary: false, ColorTool.RandomAlphaColor);
					}
					else
					{
						API.Pawn.SetSkinColor(primary: false, ColorTool.RandomColor);
					}
				}
				API.UpdateGraphics();
			}

			private void AToggleSkinColor()
			{
				isPrimarySkinColor = !isPrimarySkinColor;
			}

			private void AChangeApparelUI(Apparel a)
			{
				WindowTool.Open(new DialogColorPicker(ColorType.ApparelColor, _primaryColor: true, a));
			}

			private void AConfigApparelcolor(Apparel a)
			{
				isApparelConfigOpen = !isApparelConfigOpen;
				apparelCurrent = a;
			}

			private void AOnApparelColorChanged(Color col)
			{
				apparelCurrent.DrawColor = col;
				API.UpdateGraphics();
			}

			private void AOnTextureApparel(Apparel a)
			{
				WindowTool.Open(new DialogObjects(DialogType.Apparel, null, a));
			}

			private void ARandomApparel(Apparel a)
			{
				NextOrRandomApparel(a, next: true, random: true);
			}

			private void ASetNextApparel(Apparel a)
			{
				NextOrRandomApparel(a, next: true, random: false);
			}

			private void ASetPrevApparel(Apparel a)
			{
				NextOrRandomApparel(a, next: false, random: false);
			}

			private ApparelLayerDef GetBestLayer(List<ApparelLayerDef> l)
			{
				return l.NullOrEmpty() ? null : (l.Contains(ApparelLayerDefOf.Shell) ? ApparelLayerDefOf.Shell : (l.Contains(ApparelLayerDefOf.Middle) ? ApparelLayerDefOf.Middle : l.LastOrDefault()));
			}

			private int GetDrawOrder(Apparel ap)
			{
				return GetBestLayer(ap.def.apparel.layers)?.drawOrder ?? 0;
			}

			private ApparelLayerDef GetFreeLayer(List<ApparelLayerDef> lUsedLayer, Apparel a)
			{
				ApparelLayerDef apparelLayerDef = GetBestLayer(a.def.apparel.layers);
				if (lUsedLayer.Contains(apparelLayerDef))
				{
					foreach (ApparelLayerDef layer in a.def.apparel.layers)
					{
						if (!lUsedLayer.Contains(layer))
						{
							apparelLayerDef = layer;
						}
					}
				}
				return apparelLayerDef;
			}

			private List<ApparelLayerDef> GetUsedLayers(Apparel a)
			{
				List<ApparelLayerDef> list = new List<ApparelLayerDef>();
				foreach (Apparel item in API.Pawn.apparel.WornApparel)
				{
					if (item != a)
					{
						ApparelLayerDef bestLayer = GetBestLayer(item.def.apparel.layers);
						if (!list.Contains(bestLayer))
						{
							list.Add(bestLayer);
						}
					}
				}
				return list;
			}

			private void NextOrRandomApparel(Apparel a, bool next, bool random, bool doSkip = false)
			{
				List<ApparelLayerDef> usedLayers = GetUsedLayers(a);
				ApparelLayerDef freeLayer = GetFreeLayer(usedLayers, a);
				bool flag = a.IsForNeck();
				bool flag2 = a.IsForLegs();
				bool flag3 = a.IsForEyes();
				bool flag4 = false;
				if (usedLayers.Contains(ApparelLayerDefOf.Middle) && flag2)
				{
					flag4 = true;
				}
				if (usedLayers.Contains(ApparelLayerDefOf.Overhead))
				{
					flag4 = true;
				}
				BodyPartGroupDef bodyPartGroupDef = (flag2 ? BodyPartGroupDefOf.Legs : (flag ? DefTool.BodyPartGroupDef("Neck") : (flag3 ? DefTool.BodyPartGroupDef("Eyes") : a.def.apparel.bodyPartGroups.FirstOrDefault())));
				HashSet<ThingDef> hashSet = ApparelTool.ListOfApparel(null, freeLayer, flag4 ? bodyPartGroupDef : null);
				int num = 0;
				foreach (ThingDef item in hashSet)
				{
					if (item.defName == a.def.defName)
					{
						break;
					}
					num++;
				}
				num = hashSet.NextOrPrevIndex(num, next, random);
				API.Pawn.apparel.Remove(a);
				int count = hashSet.Count;
				for (int i = 0; i < count; i++)
				{
					if (API.Pawn.apparel.CanWearWithoutDroppingAnything(hashSet.At(num)) && API.Pawn.ApparalGraphicTest2(hashSet.At(num), showError: false))
					{
						break;
					}
					num = hashSet.NextOrPrevIndex(num, next, random);
				}
				ThingDef thingDef = hashSet.At(num);
				if (Prefs.DevMode)
				{
					MessageTool.Show("wear... " + thingDef.defName + " " + (num + 1) + "/" + hashSet.Count);
				}
				WearSelectedApparel(Selected.ByThingDef(thingDef), a.DrawColor);
			}

			private void WearSelectedApparel(Selected s, Color oldColor)
			{
				Apparel apparel = ApparelTool.GenerateApparel(s);
				if (Event.current.alt)
				{
					apparel.DrawColor = oldColor;
				}
				else if (Event.current.shift)
				{
					apparel.DrawColor = ColorTool.RandomColor;
				}
				else if (Event.current.control)
				{
					apparel.DrawColor = ColorTool.RandomAlphaColor;
				}
				API.Pawn.WearThatApparel(apparel);
				API.UpdateGraphics();
			}

			private void DrawWeaponSelector()
			{
				if (!API.Pawn.HasEquipmentTracker() || weaponCurrent == null)
				{
					return;
				}
				try
				{
					List<ThingWithComps> allEquipmentListForReading = API.Pawn.equipment.AllEquipmentListForReading;
					for (int i = 0; i < allEquipmentListForReading.Count; i++)
					{
						ThingWithComps thingWithComps = allEquipmentListForReading[i];
						SZWidgets.NavSelectorImageBox2(RectFullSolid, thingWithComps, AChangeWeaponUI, ARandomWeapon, ASetPrevWeapon, ASetNextWeapon, AOnTextureWeapon, AConfigWeaponcolor, thingWithComps.def.label.CapitalizeFirst(), thingWithComps.GetTooltip().text, Label.TIP_RANDOMIZE_EQUIP);
						y += 30;
						if (isWeaponConfigOpen && weaponCurrent == thingWithComps)
						{
							if (thingWithComps.TryGetComp<CompColorable>() != null)
							{
								SZWidgets.ColorBox(new Rect(x + 25, y, w - 50, 95f), thingWithComps.DrawColor, AOnWeaponColorChanged, halfAlfa: true);
								y += 95;
							}
							else
							{
								thingWithComps.def.AddCompColorable();
								thingWithComps.InitializeComps();
							}
						}
					}
				}
				catch
				{
				}
			}

			private void AChangeWeaponUI(ThingWithComps wp)
			{
				if (wp.def.colorGenerator == null || !wp.def.HasComp(typeof(CompColorable)))
				{
					MessageTool.ShowActionDialog(Label.INFOD_WEAPON, delegate
					{
						ApparelTool.MakeThingwColorable(wp);
						WindowTool.Open(new DialogColorPicker(ColorType.WeaponColor, _primaryColor: true, null, wp));
					}, Label.INFOT_MAKECOLORABLE);
				}
				else
				{
					WindowTool.Open(new DialogColorPicker(ColorType.WeaponColor, _primaryColor: true, null, wp));
				}
			}

			private void AConfigWeaponcolor(ThingWithComps wp)
			{
				isWeaponConfigOpen = !isWeaponConfigOpen;
				weaponCurrent = wp;
			}

			private void AOnTextureWeapon(ThingWithComps wp)
			{
				WindowTool.Open(new DialogObjects(DialogType.Weapon, null, wp));
			}

			private void AOnWeaponColorChanged(Color col)
			{
				weaponCurrent.DrawColor = col;
				API.UpdateGraphics();
			}

			private void ARandomWeapon(ThingWithComps wp)
			{
				NextOrRandomWeapon(wp, next: true, random: true);
			}

			private void ASetNextWeapon(ThingWithComps wp)
			{
				NextOrRandomWeapon(wp, next: true, random: false);
			}

			private void ASetPrevWeapon(ThingWithComps wp)
			{
				NextOrRandomWeapon(wp, next: false, random: false);
			}

			private void EquipSelectedWeapon(Selected selectedWeapon, bool primary)
			{
				API.Pawn.Reequip(selectedWeapon, (!primary) ? 1 : 0);
				API.UpdateGraphics();
			}

			private void NextOrRandomWeapon(ThingWithComps wp, bool next, bool random)
			{
				HashSet<ThingDef> hashSet = WeaponTool.ListOfWeapons(null, WeaponType.Melee);
				HashSet<ThingDef> other = WeaponTool.ListOfWeapons(null, WeaponType.Ranged);
				hashSet.AddRange(other);
				int num = 0;
				foreach (ThingDef item in hashSet)
				{
					if (item.defName == wp.def.defName)
					{
						break;
					}
					num++;
				}
				num = hashSet.NextOrPrevIndex(num, next, random);
				ThingDef thingDef = hashSet.At(num);
				if (Prefs.DevMode)
				{
					MessageTool.Show("equip... " + thingDef.defName + " " + (num + 1) + "/" + hashSet.Count);
				}
				EquipSelectedWeapon(Selected.ByThingDef(thingDef), wp == API.Pawn.equipment.Primary);
			}

			private void ARemoveDeadLogo()
			{
				randomDeadColor = ColorTool.RandomColor;
				iTickRemoveDeadLogo = 180;
			}
		}

		private class BlockPawnList
		{
			private Vector2 scrollPos;

			private bool bShowRemovePawn;

			private string colParam;

			private Capturer capturer;

			private HashSet<ThingDef> lraces;

			private List<PawnKindDef> ListOfPawnKinds => API.ListOf<PawnKindDef>(EType.PawnKindListed);

			internal List<Pawn> ListOfPawns => API.ListOf<Pawn>(EType.Pawns);

			private Dictionary<string, Faction> DicFactions => API.Get<Dictionary<string, Faction>>(EType.Factions);

			private bool IsPlayerFaction => ListName == Label.COLONISTS;

			internal BlockPawnList()
			{
				bShowRemovePawn = false;
				colParam = "n.a.";
				capturer = API.Get<Capturer>(EType.Capturer);
				lraces = new HashSet<ThingDef>();
				lraces.Add(null);
			}

			internal void Draw(coord c)
			{
				int x = c.x;
				int y = c.y;
				int w = c.w;
				int h = c.h;
				if (w > 0)
				{
					CheckPawnFromSelector();
					DrawListSelector(x, ref y, w);
					DrawOnMap(x, y, w);
					DrawListCount(x, y, w);
					DrawTopBottons(x, ref y, w);
					DrawList(x, y, w, h);
				}
			}

			private void DrawListSelector(int x, ref int y, int w)
			{
				Text.Font = GameFont.Tiny;
				SZWidgets.FloatMenuOnButtonText(new Rect(x, y, w, 25f), ListName, DicFactions.Keys.ToList(), (string s) => s, delegate(string listname)
				{
					ChangeList(listname);
				});
				y += 25;
				if (IsRandom)
				{
					SZWidgets.FloatMenuOnButtonText(new Rect(x, y, w, 25f), RACENAME(RACE), lraces, RACENAME, ARaceChanged);
					y += 25;
					SZWidgets.FloatMenuOnButtonText(new Rect(x, y, w, 25f), PKDNAME(PKD), ListOfPawnKinds, PKDNAME, APKDChanged);
					y += 25;
				}
			}

			private void DrawOnMap(int x, int y, int w)
			{
				if (!InStartingScreen)
				{
					SZWidgets.ButtonImageCol(new Rect(x + 2, y + 4, 16f, 16f), "bworld", AChangedOnMap, OnMap ? Color.grey : Color.white, Label.ONMAP);
				}
			}

			private void DrawListCount(int x, int y, int w)
			{
				try
				{
					Listing_X listing_X = new Listing_X();
					listing_X.Begin(new Rect(InStartingScreen ? x : (x + 24), y + 2, 88f, InStartingScreen ? 40 : 20));
					if (InStartingScreen && IsPlayerFaction && !Find.GameInitData.startingAndOptionalPawns.NullOrEmpty())
					{
						listing_X.AddIntSection("", "max" + Find.GameInitData.startingAndOptionalPawns.Count, ref colParam, ref Find.GameInitData.startingPawnCount, 1, Find.GameInitData.startingAndOptionalPawns.Count, small: true);
					}
					else if (!ListOfPawns.NullOrEmpty())
					{
						listing_X.Label(0f, 0f, 80f, 20f, ListOfPawns.Count.ToString(), GameFont.Tiny);
					}
					listing_X.End();
				}
				catch
				{
					bGamePlus = false;
					bStartNewGame = false;
					bStartNewGame2 = false;
					ChangeList(ListName);
				}
			}

			private void DrawTopBottons(int x, ref int y, int w)
			{
				if (IsRandom || ListOfPawns.NullOrEmpty())
				{
					x += 25;
					SZWidgets.ButtonImage(w - x, y, 25f, 25f, "UI/Buttons/Dev/Add", AAddStartPawn);
					x += 25;
					SZWidgets.ButtonImage(w - x, y, 25f, 25f, "bminus", AAllowRemovePawn, InStartingScreen ? Label.TIP_REMOVE_PAWN.SubstringTo("\n") : Label.TIP_REMOVE_PAWN);
					x += 25;
				}
				y += (InStartingScreen ? 40 : 26);
			}

			private void DrawList(int x, int y, int w, int h)
			{
				if (ListOfPawns.NullOrEmpty())
				{
					return;
				}
				Text.Font = GameFont.Small;
				float num = 112f;
				Rect outRect = new Rect(0f, y, w, h - y);
				Rect rect = new Rect(0f, y, outRect.width - 16f, (float)ListOfPawns.Count * num);
				Widgets.BeginScrollView(outRect, ref scrollPos, rect);
				Rect rect2 = rect.ContractedBy(4f);
				rect2.height = rect.height;
				Listing_X listing_X = new Listing_X();
				listing_X.Begin(rect2);
				try
				{
					int num2 = 0;
					int num3 = 0;
					for (int i = 0; i < ListOfPawns.Count; i++)
					{
						if (listing_X.CurY + num > scrollPos.y && listing_X.CurY - 700f < scrollPos.y)
						{
							Pawn pawn = ListOfPawns[i];
							Color backColor = ((pawn.Faction == null) ? Color.white : pawn.Faction.Color);
							if (InStartingScreen && IsPlayerFaction)
							{
								num3++;
								backColor = ((num3 > Find.GameInitData.startingPawnCount) ? Color.white : pawn.Faction.Color);
							}
							num2 = listing_X.Selectable(pawn.LabelShort, selected: false, "", capturer.GetRenderTexture(pawn, fromCache: true), null, null, default(Vector2), bShowRemovePawn, 100f, backColor, ColorTool.colLightGray, autoincrement: false);
							if (pawn.Dead && API.GetO(OptionB.SHOWDEADLOGO))
							{
								Rect rect3 = new Rect(0f, listing_X.CurY - 100f, rect.width, 50f);
								GUI.DrawTexture(rect3, (Texture)ContentFinder<Texture2D>.Get("bdead"));
							}
							switch (num2)
							{
							case 1:
								API.Pawn = pawn;
								break;
							case 2:
								pawn.Delete();
								break;
							}
						}
						listing_X.CurY += num;
					}
				}
				catch
				{
				}
				listing_X.End();
				Widgets.EndScrollView();
			}

			private void CheckPawnFromSelector()
			{
				if (!InStartingScreen)
				{
					Pawn pawn = Find.Selector?.FirstPawnFromSelector();
					if (pawn != null && API.Pawn != pawn)
					{
						API.Pawn = pawn;
					}
				}
				if (API.Pawn == null)
				{
					API.Pawn = ListOfPawns.FirstOrFallback();
				}
			}

			private void AChangedOnMap()
			{
				SoundTool.PlayThis(SoundDefOf.Click);
				OnMap = !OnMap;
				ReloadList();
			}

			internal void ChangeList(string listname)
			{
				ListName = (listname.NullOrEmpty() ? Label.COLONISTS : listname);
				DicFactions.Clear();
				DicFactions.Merge(FactionTool.GetDicOfFactions());
				ListOfPawns.Clear();
				List<Pawn> pawnList = PawnxTool.GetPawnList(listname, OnMap, DicFactions.GetValue(listname));
				if (pawnList != null)
				{
					ListOfPawns.AddRange(pawnList);
				}
				UpdateRaceList();
				if (API.Pawn == null)
				{
					API.Pawn = ListOfPawns.FirstOrFallback();
				}
			}

			internal void ReloadList()
			{
				ChangeList(ListName);
			}

			private void UpdateRaceList()
			{
				bool nonhumanlike = ListName == Label.COLONYANIMALS || ListName == Label.WILDANIMALS;
				bool humanlike = ListName == Label.HUMANOID || ListName == Label.COLONISTS;
				lraces.Clear();
				lraces.Add(null);
				lraces.AddRange(PawnKindTool.ListOfRaces(humanlike, nonhumanlike));
				ARaceChanged(null);
			}

			private void ARaceChanged(ThingDef race)
			{
				RACE = race;
				bool nonhumanlike = ListName == Label.COLONYANIMALS || ListName == Label.WILDANIMALS;
				bool humanlike = ListName == Label.HUMANOID || ListName == Label.COLONISTS;
				ListOfPawnKinds.Clear();
				ListOfPawnKinds.Add(null);
				ListOfPawnKinds.AddRange(PawnKindTool.ListOfPawnKindDefByRace(race, humanlike, nonhumanlike));
				APKDChanged(null);
			}

			private void APKDChanged(PawnKindDef pkd)
			{
				PKD = pkd;
			}

			private void AAllowRemovePawn()
			{
				if (!InStartingScreen && Event.current.alt)
				{
					API.EditorMoveRight();
					PlacingTool.DeletePawnFromCustomPosition();
				}
				else
				{
					bShowRemovePawn = !bShowRemovePawn;
				}
			}

			private void AAddStartPawn()
			{
				Faction value = DicFactions.GetValue(ListName);
				PawnxTool.AddOrCreateNewPawn(PKD.ThisOrFromList(ListOfPawnKinds), value, RACE, null);
				if (value.IsZombie())
				{
					ReloadList();
				}
			}
		}

		private bool doOnce;

		private TabType currentTab;

		private readonly Dictionary<TabType, ServiceContainer> dicData;

		public override Vector2 InitialSize
		{
			get
			{
				int num = SizeRand + SizeW0 + SizeW1 + SizeW2 + SizeRand;
				int num2 = (API.GetO(OptionB.SHOWMINI) ? 320 : WindowTool.MaxH);
				return new Vector2(num, num2);
			}
		}

		public int SizeRand => 18;

		public int SizeH => (int)(InitialSize.y - 36f);

		public int SizeW0 => API.GetO(OptionB.SHOWPAWNLIST) ? 100 : 0;

		public int SizeW1 => 250;

		public int SizeW2 => API.GetO(OptionB.SHOWTABS) ? 840 : (-10);

		public int PersonX => SizeW0;

		public int PersonY => 0;

		public int PersonW => SizeW1 - 10;

		public int PersonH => SizeH;

		public int TabX => SizeW0 + SizeW1;

		public int TabY => 30;

		public int TabW => SizeW2;

		public int TabH => SizeH - TabY;

		private T Get<T>(TabType tabType)
		{
			if (!dicData.ContainsKey(tabType))
			{
				CreateType(tabType);
			}
			else if (dicData[tabType] == null)
			{
				dicData.Remove(tabType);
				CreateType(tabType);
			}
			if (dicData.ContainsKey(tabType))
			{
				return (T)dicData[tabType].GetService(typeof(T));
			}
			return default(T);
		}

		private void CreateType(TabType tabType)
		{
			switch (tabType)
			{
			case TabType.BlockBio:
				AddClassContainer(new BlockBio(), tabType);
				break;
			case TabType.BlockHealth:
				AddClassContainer(new BlockHealth(), tabType);
				break;
			case TabType.BlockInfo:
				AddClassContainer(new BlockInfo(), tabType);
				break;
			case TabType.BlockInventory:
				AddClassContainer(new BlockInventory(), tabType);
				break;
			case TabType.BlockLog:
				AddClassContainer(new BlockLog(), tabType);
				break;
			case TabType.BlockNeeds:
				AddClassContainer(new BlockNeeds(), tabType);
				break;
			case TabType.BlockPawnList:
				AddClassContainer(new BlockPawnList(), tabType);
				break;
			case TabType.BlockPerson:
				AddClassContainer(new BlockPerson(), tabType);
				break;
			case TabType.BlockRecords:
				AddClassContainer(new BlockRecords(), tabType);
				break;
			case TabType.BlockSocial:
				AddClassContainer(new BlockSocial(), tabType);
				break;
			}
		}

		private void AddClassContainer<T>(T t, TabType tabType)
		{
			dicData.Add(tabType, new ServiceContainer());
			dicData[tabType].AddService(typeof(T), t);
		}

		internal EditorUI()
		{
			dicData = new Dictionary<TabType, ServiceContainer>();
			currentTab = TabType.BlockBio;
			doOnce = true;
			SearchTool.Update(SearchTool.SIndex.Editor);
			doCloseX = true;
			forcePause = API.GetO(OptionB.PAUSEGAME);
			onlyOneOfTypeAllowed = true;
			resizeable = false;
			preventCameraMotion = false;
			closeOnAccept = false;
			draggable = true;
			layer = Layer;
			SoundTool.PlayThis(SoundDefOf.Tick_Low);
		}

		~EditorUI()
		{
			foreach (TabType key in dicData.Keys)
			{
				dicData[key].Dispose();
			}
			dicData.Clear();
		}

		private void PreselectPawn(Pawn pawn)
		{
			if (pawn == null)
			{
				object obj = (InStartingScreen ? null : Find.Selector?.FirstSelectedObject);
				if (obj != null)
				{
					Pawn pawn2 = ((obj.GetType() == typeof(Pawn)) ? (obj as Pawn) : null);
					Corpse corpse = ((obj.GetType() == typeof(Corpse)) ? (obj as Corpse) : null);
					Building_AncientCryptosleepCasket building_AncientCryptosleepCasket = ((obj.GetType() == typeof(Building_AncientCryptosleepCasket)) ? (obj as Building_AncientCryptosleepCasket) : null);
					Building_CryptosleepCasket building_CryptosleepCasket = ((obj.GetType() == typeof(Building_CryptosleepCasket)) ? (obj as Building_CryptosleepCasket) : null);
					CharacterEditorCascet characterEditorCascet = ((obj.GetType() == typeof(CharacterEditorCascet)) ? (obj as CharacterEditorCascet) : null);
					CharacterEditorGrave characterEditorGrave = ((obj.GetType() == typeof(CharacterEditorGrave)) ? (obj as CharacterEditorGrave) : null);
					if (pawn2 != null)
					{
						API.Pawn = pawn2;
					}
					else if (corpse != null)
					{
						API.Pawn = corpse.InnerPawn;
					}
					else if (characterEditorCascet != null && characterEditorCascet.ContainedThing != null)
					{
						API.Pawn = ((characterEditorCascet.ContainedThing.GetType() == typeof(Pawn)) ? (characterEditorCascet.ContainedThing as Pawn) : null);
						API.Get<Dictionary<int, Building_CryptosleepCasket>>(EType.UIContainers)[0] = characterEditorCascet;
					}
					else if (characterEditorGrave != null && characterEditorGrave.ContainedThing != null)
					{
						API.Pawn = ((characterEditorGrave.ContainedThing.GetType() == typeof(Pawn)) ? (characterEditorGrave.ContainedThing as Pawn) : null);
						API.Get<Dictionary<int, Building_CryptosleepCasket>>(EType.UIContainers)[0] = characterEditorGrave;
					}
					else if (building_AncientCryptosleepCasket != null && building_AncientCryptosleepCasket.ContainedThing != null)
					{
						API.Pawn = ((building_AncientCryptosleepCasket.ContainedThing.GetType() == typeof(Pawn)) ? (building_AncientCryptosleepCasket.ContainedThing as Pawn) : null);
					}
					else if (building_CryptosleepCasket != null && building_CryptosleepCasket.ContainedThing != null)
					{
						API.Pawn = ((building_CryptosleepCasket.ContainedThing.GetType() == typeof(Pawn)) ? (building_CryptosleepCasket.ContainedThing as Pawn) : null);
					}
				}
			}
			else
			{
				API.Pawn = pawn;
			}
			Get<BlockPawnList>(TabType.BlockPawnList).ReloadList();
		}

		internal void Start(Pawn pawn)
		{
			if (WindowTool.IsOpen<EditorUI>())
			{
				WindowTool.BringToFront(this, force: true);
				return;
			}
			PreselectPawn(pawn);
			currentTab = TabType.BlockBio;
			WindowTool.Open(this);
		}

		public override void Close(bool doCloseSound = true)
		{
			currentTab = TabType.None;
			API.Pawn = null;
			if (!InStartingScreen && !API.GetO(OptionB.STAYINCASCET))
			{
				try
				{
					API.Get<Dictionary<int, Building_CryptosleepCasket>>(EType.UIContainers)[0]?.EjectContents();
				}
				catch
				{
				}
			}
			WindowTool.TryRemove<DialogColorPicker>();
			WindowTool.TryRemove<DialogChangeHeadAddons>();
			SearchTool.Save(SearchTool.SIndex.Editor, windowRect.position);
			doOnce = true;
			base.Close(doCloseSound);
		}

		private void DrawTabs(int x, int y, int w, int h)
		{
			if (w > 0 && API.Pawn != null)
			{
				int num = w / 8;
				GUI.color = ((currentTab == TabType.BlockBio) ? Color.yellow : Color.white);
				SZWidgets.ButtonTextVar(x, y, num, h, "TabCharacter".Translate(), ATabwechsel, TabType.BlockBio);
				x += num;
				GUI.color = ((currentTab == TabType.BlockInventory) ? Color.yellow : Color.white);
				SZWidgets.ButtonTextVar(x, y, num, h, "Inventory".Translate(), ATabwechsel, TabType.BlockInventory);
				x += num;
				GUI.color = ((currentTab == TabType.BlockHealth) ? Color.yellow : Color.white);
				SZWidgets.ButtonTextVar(x, y, num, h, "Health".Translate(), ATabwechsel, TabType.BlockHealth);
				x += num;
				GUI.color = ((currentTab == TabType.BlockNeeds) ? Color.yellow : Color.white);
				SZWidgets.ButtonTextVar(x, y, num, h, "TabNeeds".Translate(), ATabwechsel, TabType.BlockNeeds);
				x += num;
				GUI.color = ((currentTab == TabType.BlockSocial) ? Color.yellow : Color.white);
				SZWidgets.ButtonTextVar(x, y, num, h, "TabSocial".Translate(), ATabwechsel, TabType.BlockSocial);
				x += num;
				GUI.color = ((currentTab == TabType.BlockLog) ? Color.yellow : Color.white);
				SZWidgets.ButtonTextVar(x, y, num, h, "TabLog".Translate(), ATabwechsel, TabType.BlockLog);
				x += num;
				GUI.color = ((currentTab == TabType.BlockInfo) ? Color.yellow : Color.white);
				SZWidgets.ButtonTextVar(x, y, num, h, Label.INFO, ATabwechsel, TabType.BlockInfo);
				x += num;
				GUI.color = ((currentTab == TabType.BlockRecords) ? Color.yellow : Color.white);
				SZWidgets.ButtonTextVar(x, y, num, h, "TabRecords".Translate(), ATabwechsel, TabType.BlockRecords);
			}
		}

		private void DrawSelectedTab(int x, int y, int w, int h)
		{
			if (w > 0 && API.Pawn != null)
			{
				Rect rect = new Rect(x, y, w, h);
				coord c = default(coord);
				c.x = x;
				c.y = y;
				c.w = w;
				c.h = h;
				GUI.color = Color.white;
				if (currentTab == TabType.BlockBio)
				{
					Get<BlockBio>(TabType.BlockBio).Draw(c);
				}
				else if (currentTab == TabType.BlockInventory)
				{
					Get<BlockInventory>(TabType.BlockInventory).Draw(c);
				}
				else if (currentTab == TabType.BlockHealth)
				{
					Get<BlockHealth>(TabType.BlockHealth).Draw(c);
				}
				else if (currentTab == TabType.BlockNeeds)
				{
					Get<BlockNeeds>(TabType.BlockNeeds).Draw(c);
				}
				else if (currentTab == TabType.BlockSocial)
				{
					Get<BlockSocial>(TabType.BlockSocial).Draw(c);
				}
				else if (currentTab == TabType.BlockLog)
				{
					Get<BlockLog>(TabType.BlockLog).Draw(c);
				}
				else if (currentTab == TabType.BlockInfo)
				{
					Get<BlockInfo>(TabType.BlockInfo).Draw(c);
				}
				else if (currentTab == TabType.BlockRecords)
				{
					Get<BlockRecords>(TabType.BlockRecords).Draw(c);
				}
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			if (currentTab != TabType.None)
			{
				if (!InStartingScreen && doOnce)
				{
					SearchTool.SetPosition(SearchTool.SIndex.Editor, ref windowRect, ref doOnce, 0);
				}
				if (windowRect.height > 320f)
				{
					SZWidgets.ButtonImage(PersonX, PersonH - 22, 22f, 22f, API.GetO(OptionB.SHOWPAWNLIST) ? "bforward" : "bbackward", AShowList);
					SZWidgets.ButtonImage(PersonX + SizeW1 - 32, PersonH - 22, 22f, 22f, API.GetO(OptionB.SHOWTABS) ? "bbackward" : "bforward", AShowTabs);
				}
				coord c = default(coord);
				c.x = 0;
				c.y = 0;
				c.w = SizeW0;
				c.h = SizeH;
				Get<BlockPawnList>(TabType.BlockPawnList).Draw(c);
				coord c2 = default(coord);
				c2.x = PersonX;
				c2.y = PersonY;
				c2.w = PersonW;
				c2.h = PersonH;
				Get<BlockPerson>(TabType.BlockPerson).Draw(c2);
				DrawTabs(TabX, 0, TabW, TabY);
				DrawSelectedTab(TabX, TabY, TabW, TabH);
				int index = WindowTool.GetIndex(this);
				int index2 = WindowTool.GetIndex(WindowTool.GetWindowOf<DialogColorPicker>());
				int index3 = WindowTool.GetIndex(WindowTool.GetWindowOf<DialogChangeHeadAddons>());
				WindowTool.TopLayerForWindowOf<DialogColorPicker>(index > index2);
				WindowTool.TopLayerForWindowOf<DialogChangeHeadAddons>(index > index3);
				if (InStartingScreen && !Find.GameInitData.startingAndOptionalPawns.IsSame(Get<BlockPawnList>(TabType.BlockPawnList).ListOfPawns))
				{
					Get<BlockPawnList>(TabType.BlockPawnList).ReloadList();
				}
				closeOnClickedOutside = InStartingScreen;
			}
		}

		private void AShowList()
		{
			API.Toggle(OptionB.SHOWPAWNLIST);
		}

		private void AShowTabs()
		{
			API.Toggle(OptionB.SHOWTABS);
		}

		private void ATabwechsel(TabType tab)
		{
			currentTab = tab;
			if (currentTab == TabType.BlockInfo)
			{
				StatsReportUtility.Notify_QuickSearchChanged();
			}
		}
	}

	private static ModContentPack pack;

	private ModData data;

	internal static System.Random zufallswert;

	private static bool bOnMap;

	internal const string CO_ALLBUTTONSINORDER = "allButtonsInOrder";

	private static float pDensity = 1f;

	private static float aDensity = 1f;

	private static int selectedTile = -1;

	private static bool bCreateDefaults = true;

	private static bool bUpdateCustoms = true;

	internal static bool bStartNewGame = false;

	internal static bool bStartNewGame2 = false;

	internal static bool bGamePlus = false;

	internal static Scenario oldScenario;

	internal static Storyteller oldStoryteller;

	private static Page_ConfigureStartingPawns pstartInstance = null;

	internal static CEditor API { get; private set; }

	internal static WindowLayer Layer => WindowLayer.Dialog;

	internal static bool IsFacialStuffActive { get; private set; }

	internal static bool IsFacesOfTheRimActive { get; private set; }

	internal static bool IsPsychologyActive { get; private set; }

	internal static bool IsPersonalitiesActive { get; private set; }

	internal static bool IsRJWActive { get; private set; }

	internal static bool IsDualWieldActive { get; private set; }

	internal static bool IsFacialAnimationActive { get; private set; }

	internal static bool IsGradientHairActive { get; private set; }

	internal static bool IsAlienRaceActive { get; private set; }

	internal static bool IsAVPActive { get; private set; }

	internal static bool IsCombatExtendedActive { get; private set; }

	internal static bool IsTerraformRimworldActive { get; private set; }

	internal static bool IsAgeMattersActive { get; private set; }

	internal static bool IsExtendedUI { get; set; }

	internal static bool IsBodysizeActive { get; private set; }

	internal static bool InStartingScreen => Current.ProgramState != ProgramState.Playing || bGamePlus;

	internal static bool OnMap
	{
		get
		{
			return !InStartingScreen && bOnMap;
		}
		set
		{
			bOnMap = value;
		}
	}

	internal static bool IsRandom { get; private set; }

	internal static bool IsRaceSpecificHead { get; private set; }

	internal static bool DontAsk { get; set; }

	internal static PawnKindDef PKD { get; private set; }

	internal static ThingDef RACE { get; private set; }

	internal static Func<ThingDef, string> RACENAME => (ThingDef s) => (s == null) ? Label.ALL : ((s.label == null) ? "" : s.LabelCap.ToString());

	internal static Func<ThingDef, string> RACEDESC => (ThingDef s) => (s != null && s.description != null) ? s.description : "";

	internal static Func<PawnKindDef, string> PKDNAME => (PawnKindDef s) => (s == null) ? Label.ALL : ((s.label == null) ? "" : s.LabelCap.ToString());

	internal static Func<PawnKindDef, string> PKDTOOLTIP => (PawnKindDef s) => (s != null && s.description != null) ? s.description : "";

	internal static string ListName { get; set; }

	internal int NumSlots => data.Get<ModOptions>(EType.Settings).Get(OptionI.NUMPAWNSLOTS);

	internal int NumCapsuleSlots => data.Get<ModOptions>(EType.Settings).Get(OptionI.NUMCAPSULESETS);

	internal int MaxSliderVal => data.Get<ModOptions>(EType.Settings).Get(OptionI.STACKLIMIT);

	internal Pawn Pawn
	{
		get
		{
			return data.p;
		}
		set
		{
			data.p = value;
			if (!InStartingScreen)
			{
				if (value != null && value.Spawned)
				{
					Find.Selector?.ClearSelection();
					Find.Selector?.Select(value);
				}
				else
				{
					Find.Selector?.ClearSelection();
				}
			}
			data.UpdateGraphics();
		}
	}

	internal int EditorPosX => (int)data.Get<EditorUI>(EType.EditorUI).windowRect.position.x;

	internal int EditorPosY => (int)data.Get<EditorUI>(EType.EditorUI).windowRect.position.y;

	internal Dictionary<string, Faction> DicFactions => Get<Dictionary<string, Faction>>(EType.Factions);

	private CEditor()
	{
		Log.Message(Reflect.APP_NAME_AND_VERISON + " initializing...");
		zufallswert = new System.Random(50);
		IsFacialStuffActive = ModLister.HasActiveModWithName("Facial Stuff 1.0") || ModLister.HasActiveModWithName("Facial Stuff 1.1") || ModLister.HasActiveModWithName("Facial Stuff 1.2");
		IsFacesOfTheRimActive = Extension.HasMID("Capi.FacesOfTheRim");
		IsPsychologyActive = Extension.HasMID("Community.Psychology.UnofficialUpdate") || ModLister.HasActiveModWithName("Psychology (unofficial 1.1/1.2) ") || ModLister.HasActiveModWithName("Psychology");
		IsPersonalitiesActive = Extension.HasMID("hahkethomemah.simplepersonalities");
		IsRJWActive = Extension.HasMID("rim.job.world");
		IsDualWieldActive = Extension.HasMID("Roolo.DualWield") || ModLister.HasActiveModWithName("Dual Wield");
		IsFacialAnimationActive = Extension.HasMID("Nals.FacialAnimation") || ModLister.HasActiveModWithName("[NL] Facial Animation - WIP");
		IsGradientHairActive = Extension.HasMID("automatic.gradienthair") || ModLister.HasActiveModWithName("Gradient Hair");
		IsAlienRaceActive = Extension.HasMID("erdelf.HumanoidAlienRaces") || ModLister.HasActiveModWithName("Humanoid Alien Races 2.0") || ModLister.HasActiveModWithName("Humanoid Alien Races") || ModLister.HasActiveModWithName("Humanoid Alien Races ~ Dev");
		IsAVPActive = Extension.HasMID("Ogliss.AlienVsPredator") || ModLister.HasActiveModWithName("Alien Vs Predator");
		IsCombatExtendedActive = Extension.HasMID("CETeam.CombatExtended") || ModLister.HasActiveModWithName("Combat Extended");
		IsTerraformRimworldActive = Extension.HasMID("void.terraformrimworld") || ModLister.HasActiveModWithName("TerraformRimworld");
		IsAgeMattersActive = Extension.HasMID("Troopersmith1.AgeMatters");
		IsExtendedUI = false;
		bOnMap = true;
		IsRandom = false;
		DontAsk = false;
		PKD = null;
		RACE = null;
		ListName = "";
		InjectedDefHasher.PrepareReflection();
		data = new ModData();
		Log.Message("character editor instance created");
	}

	internal void UpdateGraphics()
	{
		data.UpdateGraphics();
	}

	internal void EditorMoveRight()
	{
		data.Get<EditorUI>(EType.EditorUI).windowRect.position = new Vector2(UI.screenWidth / 2 + 200, (float)(UI.screenHeight / 2) - data.Get<EditorUI>(EType.EditorUI).InitialSize.y / 2f);
	}

	internal void OnSettingsChanged(bool updateRender = false, bool updateKeyCode = false)
	{
		data.OnSettingsChanged(updateRender, updateKeyCode);
	}

	internal void ConfigEditor()
	{
		data.Get<ModOptions>(EType.Settings).Configurate();
	}

	internal void CloseEditor()
	{
		data.Get<EditorUI>(EType.EditorUI).Close();
	}

	internal void StartEditor(Pawn pawn = null)
	{
		data.StartEditor(pawn);
	}

	internal void Toggle(OptionB b)
	{
		data.Get<ModOptions>(EType.Settings).Toggle(b);
		data.UpdateUIParameter();
	}

	internal bool Has(EType t)
	{
		return data.Has(t);
	}

	internal T Get<T>(EType t)
	{
		return data.Get<T>(t);
	}

	internal List<T> ListOf<T>(EType t)
	{
		return data.ListOf<T>(t);
	}

	internal bool GetO(OptionB b)
	{
		return data.Get<ModOptions>(EType.Settings).Get(b);
	}

	internal int GetI(OptionI i)
	{
		return data.Get<ModOptions>(EType.Settings).Get(i);
	}

	internal string GetSlot(int i)
	{
		return data.Get<ModOptions>(EType.Settings).GetSlot(i);
	}

	internal void SetSlot(int i, string val, bool andSave)
	{
		data.Get<ModOptions>(EType.Settings).SetSlot(i, val, andSave);
	}

	internal string GetCustom(OptionS s)
	{
		return (s == OptionS.HOTKEYEDITOR || s == OptionS.HOTKEYTELEPORT) ? null : data.Get<ModOptions>(EType.Settings).Get(s);
	}

	internal void SetCustom(OptionS option, string val, string defName)
	{
		data.Get<ModOptions>(EType.Settings).SetCustom(option, val, defName);
	}

	internal List<StatDef> ListOfStatDef(StatCategoryDef s, bool isWeapon, bool isEquip)
	{
		if (s == null)
		{
			if (isEquip)
			{
				return data.ListOf<StatDef>(EType.StatDefOnEquip);
			}
			if (isWeapon)
			{
				return data.ListOf<StatDef>(EType.StatDefWeapon);
			}
			return data.ListOf<StatDef>(EType.StatDefApparel);
		}
		List<StatDef> list = new List<StatDef>();
		foreach (StatDef allDef in DefDatabase<StatDef>.AllDefs)
		{
			if (allDef.category == s)
			{
				list.Add(allDef);
			}
		}
		return list;
	}

	internal static void Initialize(Mod mod)
	{
		pack = mod.Content;
		Harmony harmony = new Harmony("rimworld.mod.charactereditor");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
		harmony.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), "DoWindowContents"), null, new HarmonyMethod(typeof(CEditor), "AddCharacterEditorButton"));
		harmony.Patch(AccessTools.Method(typeof(Page_ConfigureStartingPawns), "PreOpen"), null, new HarmonyMethod(typeof(CEditor), "GamePlusPreOpen"));
		harmony.Patch(AccessTools.Method(typeof(Dialog_AdvancedGameConfig), "DoWindowContents"), null, new HarmonyMethod(typeof(CEditor), "AddMapSizeSlider"));
		harmony.Patch(AccessTools.Method(typeof(MainMenuDrawer), "Init"), null, new HarmonyMethod(typeof(CEditor), "OnMainMenuInit"));
		harmony.Patch(AccessTools.Method(typeof(MainMenuDrawer), "MainMenuOnGUI"), null, new HarmonyMethod(typeof(CEditor), "OnMainMenuOnGUI"));
		harmony.Patch(AccessTools.Method(typeof(UIRoot_Entry), "DoMainMenu"), null, new HarmonyMethod(typeof(CEditor), "OnDoingMainMenu"));
		harmony.Patch(AccessTools.Method(typeof(Map), "FinalizeInit"), null, new HarmonyMethod(typeof(CEditor), "OnMapLoaded"));
		harmony.Patch(AccessTools.Method(typeof(Game), "LoadGame"), null, new HarmonyMethod(typeof(CEditor), "OnSavegameLoaded"));
		harmony.Patch(AccessTools.Method(typeof(Gene), "PostAdd"), null, new HarmonyMethod(typeof(CEditor), "OnPostAddGene"));
		harmony.Patch(AccessTools.Method(typeof(Gene), "PostRemove"), null, new HarmonyMethod(typeof(CEditor), "OnPostRemoveGene"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_AgeTracker), "RecalculateLifeStageIndex"), null, new HarmonyMethod(typeof(CEditor), "OnRecalcIndex"));
		harmony.Patch(AccessTools.Method(typeof(Pawn_AgeTracker), "CalculateInitialGrowth"), null, new HarmonyMethod(typeof(CEditor), "OnPreRecalcIndex"));
		harmony.Patch(AccessTools.Method(typeof(ShaderUtility), "GetSkinShaderAbstract"), new HarmonyMethod(typeof(CEditor), "GetBetterShader"));
	}

	internal static void StartNewGame2()
	{
		bStartNewGame = false;
		bStartNewGame2 = false;
		Current.Game.Scenario = oldScenario;
		Current.Game.storyteller = oldStoryteller;
		Current.ProgramState = ProgramState.MapInitializing;
		if (Current.Game.InitData == null)
		{
			Current.Game.InitData = new GameInitData();
		}
		Current.Game.InitData.startedFromEntry = true;
		Find.GameInitData.startingTile = -1;
		Find.WorldInterface.SelectedTile = -1;
		Page_SelectStartingSite page_SelectStartingSite = new Page_SelectStartingSite();
		Page_ConfigureStartingPawns page_ConfigureStartingPawns = new Page_ConfigureStartingPawns();
		PageStart next = new PageStart();
		page_SelectStartingSite.next = page_ConfigureStartingPawns;
		page_ConfigureStartingPawns.next = next;
		page_ConfigureStartingPawns.prev = page_SelectStartingSite;
		Find.WindowStack.Add(page_SelectStartingSite);
	}

	internal static void StartNewGame()
	{
		bStartNewGame = false;
		bStartNewGame2 = false;
		Find.WindowStack.Add(new Page_SelectScenario());
	}

	private static void OnDoingMainMenu()
	{
		if (Label.currentLanguage != LanguageDatabase.activeLanguage.FriendlyNameEnglish.ToLower())
		{
			API.data.ReInitSettings();
		}
	}

	private static void CheckAddButton()
	{
		try
		{
			List<MainButtonDef> memberValue = Find.MainButtonsRoot.GetMemberValue<List<MainButtonDef>>("allButtonsInOrder", null);
			if (memberValue != null)
			{
				MainButtonDef item = API.data.Get<MainButtonDef>(EType.MainButton);
				MainButtonDef item2 = API.data.Get<MainButtonDef>(EType.TeleButton);
				if (!memberValue.Contains(item))
				{
					memberValue.Insert(memberValue.Count - 1, item);
				}
				if (!memberValue.Contains(item2))
				{
					memberValue.Insert(memberValue.Count - 1, item2);
				}
			}
		}
		catch
		{
			Log.Error("could not add character editor button");
		}
	}

	private static void OnMapLoaded()
	{
		CheckAddButton();
		API.OnSettingsChanged(updateRender: false, updateKeyCode: true);
	}

	private static void OnSavegameLoaded()
	{
		if (IsBodysizeActive)
		{
			RaceTool.CheckAllPawnsOnStartupAndReapplyBodySize();
		}
	}

	public static bool GetBetterShader(ref Shader __result, bool skinColorOverriden, bool dead)
	{
		if (API.GetO(OptionB.USEFIXEDSHADER))
		{
			__result = (dead ? ShaderDatabase.Cutout : ShaderDatabase.CutoutSkin);
			return false;
		}
		__result = (skinColorOverriden ? ShaderDatabase.CutoutSkinColorOverride : ShaderDatabase.CutoutSkin);
		if (dead)
		{
			__result = ShaderDatabase.Cutout;
		}
		return true;
	}

	private static void OnPreRecalcIndex(Pawn_AgeTracker __instance)
	{
		if (IsBodysizeActive)
		{
			Pawn memberValue = __instance.GetMemberValue<Pawn>("pawn", null);
			memberValue.RememberBackstory();
		}
	}

	private static void OnRecalcIndex(Pawn_AgeTracker __instance)
	{
		if (IsBodysizeActive)
		{
			Pawn memberValue = __instance.GetMemberValue<Pawn>("pawn", null);
			memberValue.TryApplyOrKeepBodySize();
		}
	}

	private static void OnPostAddGene(Gene __instance)
	{
		if (IsBodysizeActive && __instance.IsBodySizeGene())
		{
			RaceTool.RemoveOldBodySizeRedundants(__instance);
			__instance.pawn.TryApplyOrKeepBodySize(__instance);
		}
	}

	private static void OnPostRemoveGene(Gene __instance)
	{
		if (IsBodysizeActive && __instance.IsBodySizeGene())
		{
			__instance.pawn.TryApplyOrKeepBodySize();
		}
	}

	private static void OnMainMenuOnGUI()
	{
		if (bCreateDefaults)
		{
			bCreateDefaults = false;
			API.Get<ModOptions>(EType.Settings).CreateDefaultLists();
		}
		if (bUpdateCustoms)
		{
			bUpdateCustoms = false;
			API.Get<ModOptions>(EType.Settings).UpdatingCustoms();
		}
		if (bStartNewGame)
		{
			StartNewGame();
		}
	}

	private static void OnMainMenuInit()
	{
		if (API == null)
		{
			API = new CEditor();
		}
		if (API == null)
		{
			Log.Error("failed to create instance for character editor!");
			return;
		}
		ApparelTool.AllowApparelToBeColorable();
		API.OnSettingsChanged(updateRender: false, updateKeyCode: true);
		Log.Message(Reflect.APP_NAME_AND_VERISON + " ...done");
	}

	private static void GamePlusPreOpen(Page_ConfigureStartingPawns __instance)
	{
		if (bGamePlus)
		{
			bGamePlus = false;
			API.StartEditor();
			if (Find.GameInitData.playerFaction == null)
			{
				Find.GameInitData.playerFaction = Faction.OfPlayer;
			}
			ScenarioTool.LoadCapsuleSetup(DialogCapsuleUI.SLOTPATH(-1));
			PortraitsCache.Clear();
			PortraitsCache.PortraitsCacheUpdate();
			if (Find.CurrentMap != null)
			{
				Find.CurrentMap.MapUpdate();
			}
			__instance.PreOpen();
			API.CloseEditor();
		}
	}

	private static void AddCharacterEditorButton(Page_ConfigureStartingPawns __instance, Rect rect)
	{
		pstartInstance = __instance;
		pstartInstance.absorbInputAroundWindow = false;
		if (API.GetO(OptionB.SHOWICONINSTARTING))
		{
			Vector2 vector = Text.CalcSize(pstartInstance.PageTitle);
			Text.Font = GameFont.Medium;
			Widgets.DrawBoxSolid(new Rect(0f, 0f, vector.x + 140f, 40f), Widgets.WindowBGFillColor);
			SZWidgets.ButtonImage(new Rect(0f, 0f, 40f, 40f), "bastronaut1", delegate
			{
				API.StartEditor();
			}, Label.O_CHARACTEREDITORUI);
			Widgets.Label(new Rect(45f, 0f, vector.x + 100f, 40f), pstartInstance.PageTitle);
			Text.Font = GameFont.Small;
		}
		if (API.GetO(OptionB.SHOWBUTTONINSTARTING))
		{
			SZWidgets.ButtonText(new Rect(rect.x + rect.width - 300f, rect.y + 645f, 150f, 38f), Label.O_CHARACTEREDITORUI, delegate
			{
				API.StartEditor();
			});
		}
	}

	private static void AddMapSizeSlider(Dialog_AdvancedGameConfig __instance, Rect inRect)
	{
		if (!API.GetO(OptionB.SHOWMAPVARS))
		{
			return;
		}
		Listing_X listing_X = new Listing_X();
		listing_X.Begin(new Rect(0f, 280f, 655f, 150f));
		try
		{
			string selectedName = "";
			listing_X.AddIntSection("MapSize".Translate(), "", ref selectedName, ref Find.GameInitData.mapSize, 30, 1000, small: true);
			if ((int)Find.GameInitData.startingTile >= 0)
			{
				if (selectedTile != Find.GameInitData.startingTile)
				{
					selectedTile = Find.GameInitData.startingTile;
					pDensity = Find.WorldGrid[Find.GameInitData.startingTile].PrimaryBiome.plantDensity;
					aDensity = Find.WorldGrid[Find.GameInitData.startingTile].PrimaryBiome.animalDensity;
				}
				listing_X.AddSection(Label.PLANTDENSITY, "", ref selectedName, ref pDensity, 0f, 100f, small: true);
				listing_X.AddSection(Label.ANIMALDENSITY, "", ref selectedName, ref aDensity, 0f, 100f, small: true);
				if (selectedTile == Find.GameInitData.startingTile && selectedTile >= 0)
				{
					Find.WorldGrid[selectedTile].PrimaryBiome.plantDensity = pDensity;
					Find.WorldGrid[selectedTile].PrimaryBiome.animalDensity = aDensity;
				}
			}
		}
		catch
		{
		}
		listing_X.End();
	}
}
