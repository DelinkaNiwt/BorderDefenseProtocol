using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FloatSubMenus;

public class FloatSubMenu : FloatMenuOption
{
	private enum MouseArea
	{
		Option,
		Menu,
		Outside
	}

	private class OpenMenuSet
	{
		private static readonly Dictionary<FloatMenu, OpenMenuSet> sets = new Dictionary<FloatMenu, OpenMenuSet>();

		private readonly List<FloatMenu> menus = new List<FloatMenu>();

		private readonly List<Rect> rects = new List<Rect>();

		private bool cacheValid;

		private Vector2 cachedPosition;

		private float cachedDistance;

		public float MinDistance
		{
			get
			{
				Vector2 pos = UI.MousePositionOnUIInverted;
				if (!cacheValid || pos != cachedPosition)
				{
					cacheValid = true;
					cachedPosition = pos;
					cachedDistance = rects.Min((Rect r) => GenUI.DistFromRect(r, pos));
				}
				return cachedDistance;
			}
		}

		private OpenMenuSet(FloatMenu parent, FloatSubMenuInner child)
		{
			Add(parent);
			Add(child);
		}

		private void Add(FloatMenu menu)
		{
			if (!menus.Contains(menu))
			{
				menus.Add(menu);
				rects.Add(menu.windowRect.ContractedBy(-5f));
			}
			sets[menu] = this;
			cacheValid = false;
		}

		private void Remove(int i)
		{
			sets.Remove(menus[i]);
			menus.RemoveAt(i);
			rects.RemoveAt(i);
			cacheValid = false;
		}

		private void Remove(FloatMenu menu)
		{
			int num = menus.IndexOf(menu);
			if (num > 0)
			{
				Remove(num);
			}
			if (menus.Count == 1 || num == 0)
			{
				for (int num2 = menus.Count - 1; num2 >= 0; num2--)
				{
					Remove(num2);
				}
			}
		}

		public static OpenMenuSet For(FloatMenu menu)
		{
			if (!sets.TryGetValue(menu, out var value))
			{
				return null;
			}
			return value;
		}

		public static void Open(FloatMenu parent, FloatSubMenuInner child)
		{
			if (sets.TryGetValue(parent, out var value))
			{
				value.Add(child);
			}
			else
			{
				new OpenMenuSet(parent, child);
			}
			sets[parent].menus.Select((FloatMenu m) => m.ID).ToStringSafeEnumerable();
		}

		public static void Close(FloatMenu menu)
		{
			if (sets.TryGetValue(menu, out var value))
			{
				value.Remove(menu);
			}
		}
	}

	private class FloatSubMenuInner : FloatMenu
	{
		public Vector2 mouseOffset;

		public FloatSubMenu parent;

		public FloatSubMenuInner(FloatSubMenu parent, List<FloatMenuOption> options, Vector2 mouseOffset, bool vanish)
			: base(options)
		{
			this.mouseOffset = mouseOffset;
			this.parent = parent;
			onlyOneOfTypeAllowed = false;
			vanishIfMouseDistant = vanish;
			parent.UpdateFilter(this);
		}

		public override void DoWindowContents(Rect rect)
		{
			parent.UpdateFilter(this);
			base.DoWindowContents(rect);
		}

		protected override void SetInitialSizeAndPosition()
		{
			Vector2 vector = UI.MousePositionOnUIInverted + mouseOffset;
			Vector2 initialSize = InitialSize;
			float x = Mathf.Min(vector.x, (float)UI.screenWidth - initialSize.x);
			float y = Mathf.Min(vector.y, (float)UI.screenHeight - initialSize.y);
			windowRect = new Rect(x, y, initialSize.x, initialSize.y);
		}

		public override void PreOptionChosen(FloatMenuOption opt)
		{
			parent.subMenuOptionChosen = true;
			base.PreOptionChosen(opt);
		}

		public override void PreClose()
		{
			foreach (FloatSubMenu item in options.OfType<FloatSubMenu>())
			{
				item.CloseSubMenu();
			}
			OpenMenuSet.Close(this);
		}
	}

	private const string VUIE_ID = "vanillaexpanded.ui";

	private const string Achtung_ID = "brrainz.achtung";

	private static readonly bool VUIE = ModActive("vanillaexpanded.ui");

	private static readonly bool Achtung = ModActive("brrainz.achtung");

	private static readonly bool Compat = false;

	private static readonly bool CompatMMM = Compat || Achtung;

	private readonly List<FloatMenuOption> subOptions;

	private readonly float extraPartWidthOuter;

	private readonly Func<Rect, bool> extraPartOnGUIOuter;

	private FloatSubMenuInner subMenu;

	private FloatMenuFilter filter;

	private Action parentCloseCallback;

	private bool parentSetUp;

	private bool subMenuOptionChosen;

	private bool subOptionsInitialized;

	private Rect extraGUIRect = new Rect(-1f, -1f, 0f, 0f);

	private static readonly Vector2 MenuOffset = new Vector2(-1f, 0f);

	private const float ArrowExtraWidth = 16f;

	private const float ArrowOffset = 4f;

	private const float ArrowAlpha = 0.6f;

	public bool Open
	{
		get
		{
			if (subMenu != null)
			{
				return subMenu.IsOpen;
			}
			return false;
		}
	}

	public List<FloatMenuOption> Options
	{
		get
		{
			if (!subOptionsInitialized)
			{
				FloatMenuSizeMode mode = ((subOptions.Count <= 60) ? FloatMenuSizeMode.Normal : FloatMenuSizeMode.Tiny);
				subOptions.ForEach(delegate(FloatMenuOption o)
				{
					o.SetSizeMode(mode);
				});
				subOptions.Sort(OptionPriorityCmp);
				subOptionsInitialized = true;
			}
			return subOptions;
		}
	}

	private FloatMenuFilter Filter => filter ?? (filter = new FloatMenuFilter());

	private static bool ModActive(string id)
	{
		return LoadedModManager.RunningMods.Any((ModContentPack x) => x.PackageId == id);
	}

	private static Action CompatSub(List<FloatMenuOption> subOption)
	{
		return delegate
		{
			subOption.OpenMenu();
		};
	}

	public static FloatMenuOption CompatCreate(string label, List<FloatMenuOption> subOptions, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0)
	{
		if (Compat)
		{
			return new FloatMenuOption(label, CompatSub(subOptions), priority, null, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority);
		}
		return new FloatSubMenu(label, subOptions, priority, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority);
	}

	public static FloatMenuOption CompatMMMCreate(string label, List<FloatMenuOption> subOptions, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0)
	{
		if (CompatMMM)
		{
			return new FloatMenuOption(label, CompatSub(subOptions), priority, null, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority);
		}
		return new FloatSubMenu(label, subOptions, priority, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority);
	}

	public FloatSubMenu(string label, List<FloatMenuOption> subOptions, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0)
		: base(label, NoAction, priority, null, revalidateClickTarget, extraPartWidth + 16f, null, revalidateWorldClickTarget, playSelectionSound, orderInPriority)
	{
		this.subOptions = subOptions;
		extraPartOnGUIOuter = extraPartOnGUI;
		extraPartWidthOuter = extraPartWidth;
		base.extraPartOnGUI = DrawExtra;
	}

	public static FloatMenuOption CompatCreate(string label, List<FloatMenuOption> subOptions, ThingDef shownItemForIcon, ThingStyleDef thingStyle = null, bool forceBasicStyle = false, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0, int? graphicIndexOverride = null)
	{
		if (Compat)
		{
			return new FloatMenuOption(label, CompatSub(subOptions), shownItemForIcon, thingStyle, forceBasicStyle, priority, null, revalidateClickTarget, extraPartWidth + 16f, null, revalidateWorldClickTarget, playSelectionSound, orderInPriority, graphicIndexOverride);
		}
		return new FloatSubMenu(label, subOptions, shownItemForIcon, thingStyle, forceBasicStyle, priority, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, graphicIndexOverride);
	}

	public static FloatMenuOption CompatMMMCreate(string label, List<FloatMenuOption> subOptions, ThingDef shownItemForIcon, ThingStyleDef thingStyle = null, bool forceBasicStyle = false, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0, int? graphicIndexOverride = null)
	{
		if (CompatMMM)
		{
			return new FloatMenuOption(label, CompatSub(subOptions), shownItemForIcon, thingStyle, forceBasicStyle, priority, null, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, graphicIndexOverride);
		}
		return new FloatSubMenu(label, subOptions, shownItemForIcon, thingStyle, forceBasicStyle, priority, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, graphicIndexOverride);
	}

	public FloatSubMenu(string label, List<FloatMenuOption> subOptions, ThingDef shownItemForIcon, ThingStyleDef thingStyle = null, bool forceBasicStyle = false, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0, int? graphicIndexOverride = null)
		: base(label, NoAction, shownItemForIcon, thingStyle, forceBasicStyle, priority, null, revalidateClickTarget, extraPartWidth + 16f, null, revalidateWorldClickTarget, playSelectionSound, orderInPriority, graphicIndexOverride)
	{
		this.subOptions = subOptions;
		extraPartOnGUIOuter = extraPartOnGUI;
		extraPartWidthOuter = extraPartWidth;
		base.extraPartOnGUI = DrawExtra;
	}

	public static FloatMenuOption CompatCreate(string label, List<FloatMenuOption> subOptions, Texture2D itemIcon, Color iconColor, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0, HorizontalJustification iconJustification = HorizontalJustification.Left, bool extraPartRightJustified = false)
	{
		if (Compat)
		{
			return new FloatMenuOption(label, CompatSub(subOptions), itemIcon, iconColor, priority, null, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, iconJustification, extraPartRightJustified);
		}
		return new FloatSubMenu(label, subOptions, itemIcon, iconColor, priority, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, iconJustification, extraPartRightJustified);
	}

	public static FloatMenuOption CompatMMMCreate(string label, List<FloatMenuOption> subOptions, Texture2D itemIcon, Color iconColor, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0, HorizontalJustification iconJustification = HorizontalJustification.Left, bool extraPartRightJustified = false)
	{
		if (CompatMMM)
		{
			return new FloatMenuOption(label, CompatSub(subOptions), itemIcon, iconColor, priority, null, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, iconJustification, extraPartRightJustified);
		}
		return new FloatSubMenu(label, subOptions, itemIcon, iconColor, priority, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, iconJustification, extraPartRightJustified);
	}

	public FloatSubMenu(string label, List<FloatMenuOption> subOptions, Texture2D itemIcon, Color iconColor, MenuOptionPriority priority = MenuOptionPriority.Default, Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0, HorizontalJustification iconJustification = HorizontalJustification.Left, bool extraPartRightJustified = false)
		: base(label, NoAction, itemIcon, iconColor, priority, null, revalidateClickTarget, extraPartWidth + 16f, null, revalidateWorldClickTarget, playSelectionSound, orderInPriority, iconJustification, extraPartRightJustified)
	{
		this.subOptions = subOptions;
		extraPartOnGUIOuter = extraPartOnGUI;
		extraPartWidthOuter = extraPartWidth;
		base.extraPartOnGUI = DrawExtra;
	}

	private static void NoAction()
	{
	}

	public bool DrawExtra(Rect rect)
	{
		extraGUIRect = rect.RightPartPixels(16f);
		extraPartOnGUIOuter?.Invoke(rect.LeftPartPixels(extraPartWidthOuter));
		return false;
	}

	private static void DrawArrow(Rect rect)
	{
		rect.width -= 4f;
		GameFont font = Text.Font;
		TextAnchor anchor = Text.Anchor;
		Color color = GUI.color;
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.MiddleRight;
		GUI.color = new Color(color.r, color.g, color.b, color.a * 0.6f);
		Widgets.Label(rect, ">");
		Text.Font = font;
		Text.Anchor = anchor;
		GUI.color = color;
	}

	public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
	{
		if (floatMenu == null)
		{
			return base.DoGUI(rect, colonistOrdering, floatMenu);
		}
		SetupParent(floatMenu);
		MouseArea mouseArea = FindMouseArea(rect, floatMenu);
		bool flag = Mouse.IsOver(extraGUIRect);
		if (mouseArea == (MouseArea)(Open ? 1 : 0))
		{
			MouseAction(rect, !Open, floatMenu);
		}
		Vector2 mousePosition = Event.current.mousePosition;
		if ((Open && mouseArea == MouseArea.Outside) || flag)
		{
			Event.current.mousePosition = new Vector2(rect.x + 2f, rect.y + 2f);
		}
		base.DoGUI(rect, colonistOrdering, floatMenu);
		DrawArrow(rect);
		Event.current.mousePosition = mousePosition;
		if (subMenuOptionChosen)
		{
			floatMenu.PreOptionChosen(this);
		}
		return subMenuOptionChosen;
	}

	internal bool AnyMatches(Func<FloatMenuOption, bool> predicate, bool recursive)
	{
		return subOptions.Any((FloatMenuOption x) => predicate(x) || (recursive && SubAnyMatches(x, predicate)));
	}

	private bool SubAnyMatches(FloatMenuOption opt, Func<FloatMenuOption, bool> predicate)
	{
		if (opt is FloatSubMenu floatSubMenu)
		{
			return floatSubMenu.AnyMatches(predicate, recursive: true);
		}
		return false;
	}

	internal void FilterSubMenu(Func<FloatMenuOption, bool> predicate, bool reset, bool recursive)
	{
		Filter.Filter(predicate, reset, recursive);
	}

	internal void UpdateFilter(FloatMenu floatMenu)
	{
		filter?.Update(floatMenu);
	}

	private static int OptionPriorityCmp(FloatMenuOption a, FloatMenuOption b)
	{
		int num = b.Priority - a.Priority;
		if (num == 0)
		{
			return b.orderInPriority - a.orderInPriority;
		}
		return num;
	}

	internal static bool ShouldReplaceDistanceFor(FloatMenu menu, ref float distance)
	{
		OpenMenuSet openMenuSet = OpenMenuSet.For(menu);
		if (openMenuSet != null)
		{
			distance = openMenuSet.MinDistance;
			return true;
		}
		return false;
	}

	private void SetupParent(FloatMenu parent)
	{
		if (!parentSetUp && !(parent is FloatSubMenuInner))
		{
			parentSetUp = true;
			parentCloseCallback = parent.onCloseCallback;
			parent.onCloseCallback = OnParentClose;
		}
	}

	private void OnParentClose()
	{
		CloseSubMenu();
		parentCloseCallback?.Invoke();
	}

	private MouseArea FindMouseArea(Rect option, FloatMenu menu)
	{
		option.height--;
		if (Mouse.IsOver(option))
		{
			return MouseArea.Option;
		}
		if (!Mouse.IsOver(menu.windowRect.AtZero()))
		{
			return MouseArea.Outside;
		}
		return MouseArea.Menu;
	}

	private void MouseAction(Rect rect, bool enter, FloatMenu parentMenu)
	{
		if (enter)
		{
			Vector2 localPos = new Vector2(rect.xMax, rect.yMin) + MenuOffset;
			OpenSubMenu(parentMenu, localPos);
		}
		else
		{
			CloseSubMenu();
		}
	}

	private void OpenSubMenu(FloatMenu parentMenu, Vector2 localPos)
	{
		if (!Open)
		{
			Vector2 mousePosition = Event.current.mousePosition;
			Vector2 mouseOffset = localPos - mousePosition;
			SoundDef floatMenu_Open = SoundDefOf.FloatMenu_Open;
			SoundDefOf.FloatMenu_Open = null;
			subMenu = new FloatSubMenuInner(this, subOptions, mouseOffset, parentMenu.vanishIfMouseDistant);
			SoundDefOf.FloatMenu_Open = floatMenu_Open;
			subOptionsInitialized = true;
			Find.WindowStack.Add(subMenu);
			OpenMenuSet.Open(parentMenu, subMenu);
		}
	}

	private void CloseSubMenu()
	{
		if (Open)
		{
			Find.WindowStack.TryRemove(subMenu, doCloseSound: false);
			subMenu = null;
		}
	}
}
