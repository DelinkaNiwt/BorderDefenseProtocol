using System;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

public class FloatMenuAdvanced : FloatMenuOption
{
	public bool Darken;

	public FloatMenuAdvanced(TaggedString labelCap, Action value, Texture2D texture2D, Color color, bool darken)
		: base(labelCap, value, texture2D, color)
	{
		Darken = darken;
	}

	public FloatMenuAdvanced(TaggedString labelCap, Action value, Thing thng, Color color, bool darken)
		: base(labelCap, value, thng, color)
	{
		Darken = darken;
	}

	public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu)
	{
		GUI.color = (Darken ? Assets.ColorLightGray : Color.white);
		return base.DoGUI(rect, colonistOrdering, floatMenu);
	}
}
