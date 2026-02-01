using UnityEngine;
using VanillaPsycastsExpanded.UI;
using Verse;

namespace VanillaPsycastsExpanded;

public class Dialog_EditPsysets : Window
{
	private readonly ITab_Pawn_Psycasts parent;

	protected override float Margin => 3f;

	public override Vector2 InitialSize => new Vector2(parent.Size.x * 0.3f, Mathf.Max(300f, NeededHeight));

	private float NeededHeight => parent.RequestedPsysetsHeight + Margin * 2f;

	public Dialog_EditPsysets(ITab_Pawn_Psycasts parent)
	{
		this.parent = parent;
		doCloseX = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		parent.DoPsysets(inRect);
		if (windowRect.height < NeededHeight)
		{
			windowRect.height = NeededHeight;
		}
	}
}
