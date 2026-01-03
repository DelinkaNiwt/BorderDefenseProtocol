using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Dialog_NameWeapon : Window
{
	private class NameContext
	{
		public string current;

		public TaggedString label;

		public float labelWidth;

		public int maximumNameLength;

		public float textboxWidth;

		public string textboxName;

		public int nameIndex;

		public List<string> suggestedNames;

		private List<FloatMenuOption> suggestedOptions;

		public NameContext(string label, string currentName, int maximumNameLength)
		{
			current = currentName;
			this.label = label.Translate().CapitalizeFirst() + ":";
			labelWidth = Mathf.Ceil(this.label.GetWidthCached());
			this.maximumNameLength = maximumNameLength;
			textboxWidth = Mathf.Ceil(Text.CalcSize(new string('W', maximumNameLength + 2)).x);
			textboxName = label;
		}

		public void MakeRow(Thing thing, float randomizeButtonWidth, TaggedString randomizeText, TaggedString suggestedText, ref RectDivider divider, ref string focusControlOverride)
		{
			Widgets.Label(divider.NewCol(labelWidth), label);
			RectDivider rectDivider = divider.NewCol(textboxWidth);
			GUI.SetNextControlName(textboxName);
			CharacterCardUtility.DoNameInputRect(rectDivider, ref current, maximumNameLength);
			Rect rect = divider.NewCol(randomizeButtonWidth);
		}
	}

	private Thing weapon;

	private NameContext name = null;

	private bool firstCall = true;

	private string focusControlOverride;

	private string currentControl;

	private TaggedString descriptionText;

	private float? descriptionHeight;

	private float randomizeButtonWidth;

	private Vector2 size = new Vector2(800f, 800f);

	private float? renameHeight;

	private Rot4 portraitDirection;

	private float cameraZoom = 1f;

	private float portraitSize = 128f;

	private float humanPortraitVerticalOffset = -18f;

	private TaggedString cancelText = "Cancel".Translate().CapitalizeFirst();

	private TaggedString acceptText = "Accept".Translate().CapitalizeFirst();

	private TaggedString randomizeText;

	private TaggedString suggestedText;

	private TaggedString renameText;

	private string genderText;

	private const float ButtonHeight = 35f;

	private const float NameFieldsHeight = 30f;

	private const int MaximumNumberOfNames = 4;

	private const float VerticalMargin = 4f;

	private const float HorizontalMargin = 17f;

	private const float PortraitSize = 128f;

	private const int ContextHash = 136098329;

	private string CurWeaponName
	{
		get
		{
			return weapon.UniqueName();
		}
		set
		{
			name.current = value;
		}
	}

	public override Vector2 InitialSize => size;

	private CompUniqueWeapon comp => weapon.TryGetComp<CompUniqueWeapon>();

	public Dialog_NameWeapon(Thing weapon, string initialFirstNameOverride = null, string initialNickNameOverride = null, string initialLastNameOverride = null, string initialTitleOverride = null)
	{
		this.weapon = weapon;
		descriptionText = weapon.def.label;
		renameText = "Ancot.RenameWeapon".Translate();
		portraitDirection = Rot4.East;
		cameraZoom = 1f;
		portraitSize = 128f;
		string currentName = weapon.UniqueName();
		name = new NameContext("NickName", currentName, 12);
		float labelWidth = name.labelWidth;
		float textboxWidth = name.textboxWidth;
		randomizeText = "Randomize".Translate().CapitalizeFirst();
		suggestedText = "Suggested".Translate().CapitalizeFirst() + "...";
		randomizeButtonWidth = ButtonWidth(randomizeText.GetWidthCached());
		float x = 2f * Margin + labelWidth + textboxWidth + randomizeButtonWidth + 34f;
		size = new Vector2(x, size.y);
		forcePause = true;
		absorbInputAroundWindow = true;
		closeOnClickedOutside = true;
		closeOnAccept = false;
	}

	public override void DoWindowContents(Rect inRect)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		bool flag = false;
		if ((int)Event.current.type == 4 && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
		{
			flag = true;
			Event.current.Use();
		}
		if (!firstCall && (int)Event.current.type == 8)
		{
			currentControl = GUI.GetNameOfFocusedControl();
		}
		RectAggregator rectAggregator = new RectAggregator(new Rect(inRect.x, inRect.y, inRect.width, 0f), 136098329, new Vector2(17f, 4f));
		if (!renameHeight.HasValue)
		{
			Text.Font = GameFont.Medium;
			renameHeight = Mathf.Ceil(renameText.RawText.GetHeightCached());
			Text.Font = GameFont.Small;
		}
		descriptionHeight = descriptionHeight ?? Mathf.Ceil(Text.CalcHeight(descriptionText, rectAggregator.Rect.width - portraitSize - 17f));
		float num = renameHeight.Value + 4f + descriptionHeight.Value;
		if (portraitSize > num)
		{
			num = portraitSize;
		}
		RectDivider rectDivider = rectAggregator.NewRow(num);
		Text.Font = GameFont.Medium;
		Thing thing = weapon;
		Rect rect = rectDivider.NewCol(portraitSize);
		rect.height = portraitSize;
		Widgets.ThingIcon(rect, thing);
		RectDivider rectDivider2 = rectDivider.NewRow(renameHeight.Value);
		Widgets.Label(rectDivider2, renameText);
		Text.Font = GameFont.Small;
		Widgets.Label(rectDivider.NewRow(descriptionHeight.Value), descriptionText);
		Text.Anchor = TextAnchor.MiddleLeft;
		RectDivider divider = rectAggregator.NewRow(30f);
		name.MakeRow(weapon, randomizeButtonWidth, randomizeText, suggestedText, ref divider, ref focusControlOverride);
		Text.Anchor = TextAnchor.UpperLeft;
		rectAggregator.NewRow(17.5f);
		RectDivider rectDivider3 = rectAggregator.NewRow(35f);
		float width = Mathf.Floor((rectDivider3.Rect.width - 17f) / 2f);
		if (Widgets.ButtonText(rectDivider3.NewCol(width), cancelText))
		{
			Close();
		}
		if (Widgets.ButtonText(rectDivider3.NewCol(width), acceptText) || flag)
		{
			string current = name.current;
			if (current == null)
			{
				Messages.Message("NameInvalid".Translate(), weapon, MessageTypeDefOf.NeutralEvent, historical: false);
			}
			else
			{
				weapon.SetUniqueName(current);
				Find.WindowStack.TryRemove(this);
				string text = "Ancot.WeaponGainsName".Translate(CurWeaponName);
				Messages.Message(text, weapon, MessageTypeDefOf.PositiveEvent, historical: false);
			}
		}
		size = new Vector2(size.x, Mathf.Ceil(size.y + (rectAggregator.Rect.height - inRect.height)));
		SetInitialSizeAndPosition();
	}

	private static float ButtonWidth(float textWidth)
	{
		return Math.Max(114f, textWidth + 35f);
	}
}
