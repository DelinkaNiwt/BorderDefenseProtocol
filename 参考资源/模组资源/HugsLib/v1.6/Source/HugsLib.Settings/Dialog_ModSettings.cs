using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Settings;

/// <summary>
/// An options window for settings exposed by mods using the library
/// </summary>
public class Dialog_ModSettings : Window
{
	private delegate bool SettingsHandleDrawer(SettingHandle handle, Rect inRect, HandleControlInfo info);

	private class HandleControlInfo
	{
		public readonly string ControlName;

		public readonly List<string> EnumNames;

		public bool BadInput;

		public string InputValue;

		public bool ValidationScheduled;

		public CachedLabel HandleTitle;

		public HandleControlInfo(SettingHandle handle)
		{
			ControlName = "control" + handle.GetHashCode();
			InputValue = handle.StringValue;
			EnumNames = TryGetEnumNames(handle);
		}

		private List<string> TryGetEnumNames(SettingHandle handle)
		{
			Type valueType = handle.ValueType;
			if (!valueType.IsEnum)
			{
				return null;
			}
			Array values = Enum.GetValues(valueType);
			List<string> list = new List<string>(values.Length);
			foreach (object item in values)
			{
				list.Add(Enum.GetName(valueType, item));
			}
			return list;
		}
	}

	private const float TitleLabelHeight = 40f;

	private const float ModEntryLabelHeight = 40f;

	private const float ModEntryLabelPadding = 4f;

	private const float HandleEntryPadding = 3f;

	private const float HandleEntryHeight = 34f;

	private const float ScrollBarWidthMargin = 18f;

	private readonly ModSettingsPack currentPack;

	private readonly string currentPackName;

	private readonly Color ModEntryLineColor = new Color(0.3f, 0.3f, 0.3f);

	private readonly Color BadValueOutlineColor = new Color(0.9f, 0.1f, 0.1f, 1f);

	private readonly List<SettingHandle> handles = new List<SettingHandle>();

	private readonly Dictionary<SettingHandle, HandleControlInfo> handleControlInfo = new Dictionary<SettingHandle, HandleControlInfo>();

	private readonly SettingsHandleDrawer defaultHandleDrawer;

	private readonly Dictionary<Type, SettingsHandleDrawer> handleDrawers;

	private Vector2 scrollPosition;

	private float totalContentHeight;

	private bool closingScheduled;

	public override Vector2 InitialSize => new Vector2(650f, 700f);

	public Dialog_ModSettings(ModSettingsPack pack)
	{
		currentPack = pack ?? throw new ArgumentNullException("pack");
		currentPackName = (pack.EntryName.NullOrEmpty() ? "HugsLib_setting_unnamed_mod".Translate().ToString() : pack.EntryName);
		closeOnCancel = true;
		closeOnAccept = false;
		doCloseButton = false;
		doCloseX = true;
		forcePause = true;
		absorbInputAroundWindow = true;
		resizeable = false;
		defaultHandleDrawer = DrawHandleInputText;
		handleDrawers = new Dictionary<Type, SettingsHandleDrawer>
		{
			{
				typeof(int),
				DrawHandleInputSpinner
			},
			{
				typeof(bool),
				DrawHandleInputCheckbox
			},
			{
				typeof(Enum),
				DrawHandleInputEnum
			}
		};
	}

	public override void PreOpen()
	{
		base.PreOpen();
		TryRestoreWindowState();
		RefreshSettingsHandles();
		RefreshSettingsHandles();
		PopulateControlInfo();
	}

	public override void PostClose()
	{
		base.PostClose();
		TrySaveWindowState();
		HugsLibController.Instance.Settings.SaveChanges();
	}

	public override void DoWindowContents(Rect inRect)
	{
		Vector2 closeButSize = Window.CloseButSize;
		Rect rect = new Rect(0f, 0f, inRect.width, inRect.height - (closeButSize.y + 10f)).ContractedBy(10f);
		GUI.BeginGroup(rect);
		Rect rect2 = new Rect(0f, 0f, rect.width, 40f);
		Text.Font = GameFont.Medium;
		GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
		Widgets.Label(rect2, "HugsLib_settings_windowTitle".Translate());
		GenUI.ResetLabelAlign();
		Text.Font = GameFont.Small;
		Rect outRect = new Rect(0f, rect2.height, rect.width, rect.height - rect2.height);
		bool flag = totalContentHeight > outRect.height;
		Rect rect3 = new Rect(0f, 0f, outRect.width - (flag ? 18f : 0f), totalContentHeight);
		Widgets.BeginScrollView(outRect, ref scrollPosition, rect3);
		float curY = 0f;
		DrawModEntryHeader(rect3.width, ref curY);
		for (int i = 0; i < handles.Count; i++)
		{
			SettingHandle settingHandle = handles[i];
			if (settingHandle.VisibilityPredicate != null)
			{
				try
				{
					if (!settingHandle.VisibilityPredicate())
					{
						continue;
					}
				}
				catch (Exception e)
				{
					HugsLibController.Logger.ReportException(e, currentPackName, reportOnceOnly: true, "SettingsHandle.VisibilityPredicate");
				}
			}
			DrawHandleEntry(settingHandle, rect3, ref curY, outRect.height);
		}
		Widgets.EndScrollView();
		totalContentHeight = curY;
		GUI.EndGroup();
		Text.Font = GameFont.Small;
		Rect rect4 = new Rect(0f, inRect.height - closeButSize.y, closeButSize.x, closeButSize.y);
		if (Widgets.ButtonText(rect4, "HugsLib_settings_resetAll".Translate()))
		{
			ShowResetPrompt("HugsLib_settings_resetMod_prompt".Translate(currentPackName), currentPack.Handles);
		}
		Rect rect5 = new Rect(inRect.width - closeButSize.x, inRect.height - closeButSize.y, closeButSize.x, closeButSize.y);
		if (closingScheduled)
		{
			closingScheduled = false;
			Close();
		}
		if (Widgets.ButtonText(rect5, "CloseButton".Translate()))
		{
			GUI.FocusControl((string)null);
			closingScheduled = true;
		}
	}

	private void DrawModEntryHeader(float width, ref float curY)
	{
		Rect rect = new Rect(0f, curY, width, 40f);
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		Rect rect2 = rect.ContractedBy(4f);
		Text.Font = GameFont.Medium;
		Widgets.Label(rect2, currentPackName);
		Text.Font = GameFont.Small;
		Vector2 vector = new Vector2(width, curY);
		DrawFloatMenuButton(new Vector2(vector.x, vector.y));
		curY += 40f;
		Color color = GUI.color;
		GUI.color = ModEntryLineColor;
		Widgets.DrawLineHorizontal(0f, curY, width);
		GUI.color = color;
		curY += 4f;
		void DrawFloatMenuButton(Vector2 topRight)
		{
			if (currentPack.ContextMenuEntries != null)
			{
				Vector2 topRight2 = new Vector2(topRight.x - 4f, topRight.y + (40f - ModSettingsWidgets.HoverMenuHeight) / 2f);
				bool enabled = currentPack.ContextMenuEntries != null;
				if (ModSettingsWidgets.DrawHoverMenuButton(topRight2, enabled, extraMenuOptions: true))
				{
					OpenModEntryContextMenu();
				}
			}
		}
		void OpenModEntryContextMenu()
		{
			ModSettingsWidgets.OpenExtensibleContextMenu(null, delegate
			{
			}, delegate
			{
			}, currentPack.ContextMenuEntries);
		}
	}

	private void DrawHandleEntry(SettingHandle handle, Rect parentRect, ref float curY, float scrollViewHeight)
	{
		float num = 34f;
		SettingHandle.DrawCustomControl drawCustomControl = handle.CustomDrawerFullWidth ?? handle.CustomDrawer;
		if (drawCustomControl != null && handle.CustomDrawerHeight > num)
		{
			num = handle.CustomDrawerHeight + 6f;
		}
		if (!(curY - scrollPosition.y + num < 0f) && !(curY - scrollPosition.y > scrollViewHeight))
		{
			Rect rect = new Rect(parentRect.x, parentRect.y + curY, parentRect.width, num);
			bool flag = Mouse.IsOver(rect);
			if (flag)
			{
				Widgets.DrawHighlight(rect);
			}
			Rect rect2 = rect.ContractedBy(3f);
			bool flag2 = false;
			if (handle.CustomDrawerFullWidth != null)
			{
				try
				{
					flag2 = handle.CustomDrawerFullWidth(rect2);
				}
				catch (Exception e)
				{
					HugsLibController.Logger.ReportException(e, currentPackName, reportOnceOnly: true, "SettingHandle.CustomDrawerFullWidth");
				}
			}
			else
			{
				flag2 = DrawDefaultHandleEntry(handle, rect2, flag);
			}
			if (flag2 && handle.ValueType.IsClass)
			{
				handle.HasUnsavedChanges = true;
			}
		}
		curY += num;
	}

	private bool DrawDefaultHandleEntry(SettingHandle handle, Rect trimmedEntryRect, bool mouseOverEntry)
	{
		Rect rect = new Rect(trimmedEntryRect.x + trimmedEntryRect.width / 2f, trimmedEntryRect.y, trimmedEntryRect.width / 2f, trimmedEntryRect.height);
		GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
		Rect rect2 = new Rect(trimmedEntryRect.x, trimmedEntryRect.y, trimmedEntryRect.width / 2f - 3f, trimmedEntryRect.height);
		Rect rect3 = ((handle.CustomDrawer == null) ? rect2 : trimmedEntryRect);
		HandleControlInfo handleControlInfo = this.handleControlInfo[handle];
		CachedLabel cachedLabel = handleControlInfo.HandleTitle ?? (handleControlInfo.HandleTitle = new CachedLabel(handle.Title));
		if (cachedLabel.GetHeight(rect3.width) > rect3.height)
		{
			Text.Font = GameFont.Tiny;
			rect3 = new Rect(rect3.x, rect3.y - 1f, rect3.width, rect3.height + 2f);
		}
		else
		{
			Text.Font = GameFont.Small;
		}
		Widgets.Label(rect3, cachedLabel.Text);
		Text.Font = GameFont.Small;
		GenUI.ResetLabelAlign();
		bool result = false;
		if (handle.CustomDrawer == null)
		{
			Type type = handle.ValueType;
			if (type.IsEnum)
			{
				type = typeof(Enum);
			}
			handleDrawers.TryGetValue(type, out var value);
			if (value == null)
			{
				value = defaultHandleDrawer;
			}
			result = value(handle, rect, handleControlInfo);
		}
		else
		{
			try
			{
				result = handle.CustomDrawer(rect);
			}
			catch (Exception e)
			{
				HugsLibController.Logger.ReportException(e, currentPackName, reportOnceOnly: true, "SettingHandle.CustomDrawer");
			}
		}
		if (mouseOverEntry)
		{
			DrawEntryHoverMenu(trimmedEntryRect, handle);
		}
		return result;
	}

	private void DrawEntryHoverMenu(Rect entryRect, SettingHandle handle)
	{
		Vector2 topRight = new Vector2(entryRect.x + entryRect.width / 2f - 3f, entryRect.y + entryRect.height / 2f - ModSettingsWidgets.HoverMenuHeight / 2f);
		bool flag = handle.CanBeReset && !handle.Unsaved;
		bool flag2 = handle.ContextMenuEntries != null;
		bool menuEnabled = flag || flag2;
		if (ModSettingsWidgets.DrawHandleHoverMenu(topRight, handle.Description, menuEnabled, flag2))
		{
			OpenHandleContextMenu();
		}
		void OpenHandleContextMenu()
		{
			TaggedString taggedString = (handle.CanBeReset ? "HugsLib_settings_resetValue".Translate() : ((TaggedString)null));
			ModSettingsWidgets.OpenExtensibleContextMenu(taggedString, delegate
			{
				ResetSettingHandles(handle);
			}, delegate
			{
			}, handle.ContextMenuEntries);
		}
	}

	private bool DrawHandleInputText(SettingHandle handle, Rect controlRect, HandleControlInfo info)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		Event current = Event.current;
		GUI.SetNextControlName(info.ControlName);
		info.InputValue = Widgets.TextField(controlRect, info.InputValue);
		bool flag = GUI.GetNameOfFocusedControl() == info.ControlName;
		if (flag)
		{
			info.ValidationScheduled = true;
			if ((int)current.type == 5 && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
			{
				flag = false;
			}
		}
		bool result = false;
		if (info.ValidationScheduled && !flag)
		{
			try
			{
				if (handle.Validator != null && !handle.Validator(info.InputValue))
				{
					info.BadInput = true;
				}
				else
				{
					info.BadInput = false;
					handle.StringValue = info.InputValue;
					result = true;
				}
			}
			catch (Exception e)
			{
				HugsLibController.Logger.ReportException(e, currentPackName, reportOnceOnly: false, "SettingsHandle.Validator");
			}
			info.ValidationScheduled = false;
		}
		if (info.BadInput)
		{
			DrawBadTextValueOutline(controlRect);
		}
		return result;
	}

	private bool DrawHandleInputSpinner(SettingHandle handle, Rect controlRect, HandleControlInfo info)
	{
		float height = controlRect.height;
		Rect rect = new Rect(controlRect.x, controlRect.y, height, height);
		Rect rect2 = new Rect(controlRect.x + controlRect.width - height, controlRect.y, height, height);
		bool result = false;
		if (Widgets.ButtonText(rect, "-"))
		{
			if (int.TryParse(info.InputValue, out var result2))
			{
				info.InputValue = (result2 - handle.SpinnerIncrement).ToString();
			}
			info.ValidationScheduled = true;
			result = true;
		}
		if (Widgets.ButtonText(rect2, "+"))
		{
			if (int.TryParse(info.InputValue, out var result3))
			{
				info.InputValue = (result3 + handle.SpinnerIncrement).ToString();
			}
			info.ValidationScheduled = true;
			result = true;
		}
		Rect controlRect2 = new Rect(controlRect.x + height + 1f, controlRect.y, controlRect.width - height * 2f - 2f, controlRect.height);
		if (DrawHandleInputText(handle, controlRect2, info))
		{
			result = true;
		}
		return result;
	}

	private bool DrawHandleInputCheckbox(SettingHandle handle, Rect controlRect, HandleControlInfo info)
	{
		bool checkOn = bool.Parse(info.InputValue);
		Widgets.Checkbox(controlRect.x, controlRect.y + (controlRect.height - 24f) / 2f, ref checkOn);
		if (checkOn != bool.Parse(info.InputValue))
		{
			handle.StringValue = (info.InputValue = checkOn.ToString());
			return true;
		}
		return false;
	}

	private bool DrawHandleInputEnum(SettingHandle handle, Rect controlRect, HandleControlInfo info)
	{
		if (info.EnumNames == null)
		{
			return false;
		}
		TaggedString taggedString = (handle.EnumStringPrefix + info.InputValue).Translate();
		if (Widgets.ButtonText(controlRect, taggedString))
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			foreach (string enumName in info.EnumNames)
			{
				string name = enumName;
				TaggedString taggedString2 = (handle.EnumStringPrefix + name).Translate();
				list.Add(new FloatMenuOption(taggedString2, delegate
				{
					handle.StringValue = (info.InputValue = name);
					info.ValidationScheduled = true;
				}));
			}
			ModSettingsWidgets.OpenFloatMenu(list);
		}
		if (info.ValidationScheduled)
		{
			info.ValidationScheduled = false;
			return true;
		}
		return false;
	}

	private void DrawBadTextValueOutline(Rect rect)
	{
		Color color = GUI.color;
		GUI.color = BadValueOutlineColor;
		Widgets.DrawBox(rect);
		GUI.color = color;
	}

	private void TryRestoreWindowState()
	{
		ModSettingsWindowState instance = ModSettingsWindowState.Instance;
		if (instance != null)
		{
			scrollPosition = ((instance.LastSettingsPackId == currentPack.ModId) ? new Vector2(0f, instance.VerticalScrollPosition) : Vector2.zero);
		}
	}

	private void TrySaveWindowState()
	{
		ModSettingsWindowState instance = ModSettingsWindowState.Instance;
		if (instance != null)
		{
			instance.VerticalScrollPosition = scrollPosition.y;
			instance.LastSettingsPackId = currentPack.ModId;
		}
	}

	private static void ShowResetPrompt(string message, IEnumerable<SettingHandle> resetHandles)
	{
		SettingHandle[] resetHandlesArr = resetHandles.ToArray();
		Find.WindowStack.Add(new HugsLib.Utils.Dialog_Confirm(message, OnConfirmReset, destructive: true));
		void OnConfirmReset()
		{
			ResetSettingHandles(resetHandlesArr.ToArray());
		}
	}

	private static void ResetSettingHandles(params SettingHandle[] resetHandles)
	{
		int num = 0;
		foreach (SettingHandle settingHandle in resetHandles)
		{
			if (settingHandle != null && settingHandle.CanBeReset && !settingHandle.HasDefaultValue())
			{
				try
				{
					settingHandle.ResetToDefault();
					num++;
				}
				catch (Exception arg)
				{
					HugsLibController.Logger.Error($"Failed to reset handle {settingHandle.ParentPack.ModId}.{settingHandle.Name}: {arg}");
				}
			}
		}
		if (num > 0)
		{
			Messages.Message("HugsLib_settings_resetSuccessMessage".Translate(num), MessageTypeDefOf.TaskCompletion);
		}
	}

	private void ResetHandleControlInfo(SettingHandle handle)
	{
		handleControlInfo[handle] = new HandleControlInfo(handle);
	}

	private static IEnumerable<SettingHandle> GetHiddenResettableHandles(IEnumerable<SettingHandle> handles)
	{
		return handles.Where((SettingHandle h) => h.CanBeReset && h.NeverVisible);
	}

	private void RefreshSettingsHandles()
	{
		handles.Clear();
		handles.AddRange(from h in currentPack.Handles
			where !h.NeverVisible
			orderby h.DisplayOrder
			select h);
		foreach (SettingHandle handle in handles)
		{
			handle.ValueChanged -= OnHandleValueChanged;
			handle.ValueChanged += OnHandleValueChanged;
		}
	}

	private void OnHandleValueChanged(SettingHandle handle)
	{
		ResetHandleControlInfo(handle);
	}

	private void PopulateControlInfo()
	{
		handleControlInfo.Clear();
		foreach (SettingHandle handle in handles)
		{
			handleControlInfo.Add(handle, new HandleControlInfo(handle));
		}
	}
}
