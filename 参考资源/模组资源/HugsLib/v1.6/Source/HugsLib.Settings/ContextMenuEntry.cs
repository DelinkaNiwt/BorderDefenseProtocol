using System;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Settings;

/// <summary>
/// Contains data used for the creation of a <see cref="T:Verse.FloatMenuOption" />.
/// </summary>
public struct ContextMenuEntry
{
	/// <summary>
	/// A name for the entry to show to the player.
	/// </summary>
	public string Label { get; }

	/// <summary>
	/// The delegate that will be called when the menu entry is clicked.
	/// </summary>
	public Action Action { get; }

	/// <summary>
	/// Set to true to make a greyed-out, non-selectable menu entry.
	/// </summary>
	public bool Disabled { get; set; }

	/// <param name="label"><inheritdoc cref="P:HugsLib.Settings.ContextMenuEntry.Label" /></param>
	/// <param name="action"><inheritdoc cref="P:HugsLib.Settings.ContextMenuEntry.Action" /></param>
	public ContextMenuEntry(string label, Action action)
	{
		this = default(ContextMenuEntry);
		Label = label;
		Action = action;
	}

	internal void Validate()
	{
		if (string.IsNullOrEmpty(Label))
		{
			throw new FormatException(string.Format("{0} must have a non-empty label: {1}", "ContextMenuEntry", this));
		}
		if (Action == null)
		{
			throw new FormatException(string.Format("{0} must have non-null action: {1}", "ContextMenuEntry", this));
		}
	}

	public override string ToString()
	{
		return "[Label:" + Label.ToStringSafe() + ", Action:" + HugsLibUtility.DescribeDelegate(Action) + "]";
	}
}
