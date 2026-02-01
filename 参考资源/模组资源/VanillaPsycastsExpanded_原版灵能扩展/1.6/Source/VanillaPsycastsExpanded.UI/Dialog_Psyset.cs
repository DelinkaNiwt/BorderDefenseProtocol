using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VanillaPsycastsExpanded.Technomancer;
using VEF.Abilities;
using VEF.Utils;
using Verse;

namespace VanillaPsycastsExpanded.UI;

public class Dialog_Psyset : Window
{
	private readonly Dictionary<AbilityDef, Vector2> abilityPos = new Dictionary<AbilityDef, Vector2>();

	private readonly CompAbilities compAbilities;

	private readonly Hediff_PsycastAbilities hediff;

	private readonly PsySet psyset;

	public List<PsycasterPathDef> paths;

	private int curIdx;

	private Pawn pawn;

	public override Vector2 InitialSize => new Vector2(480f, 520f);

	public Dialog_Psyset(PsySet psyset, Pawn pawn)
	{
		this.psyset = psyset;
		this.pawn = pawn;
		hediff = pawn.Psycasts();
		compAbilities = ((ThingWithComps)pawn).GetComp<CompAbilities>();
		doCloseButton = true;
		doCloseX = true;
		forcePause = true;
		closeOnClickedOutside = true;
		paths = hediff.unlockedPaths.ListFullCopy();
		foreach (PsycasterPathDef item in pawn.AllPathsFromPsyrings())
		{
			if (!paths.Contains(item))
			{
				paths.Add(item);
			}
		}
	}

	public override void DoWindowContents(Rect inRect)
	{
		inRect.yMax -= 50f;
		Text.Font = GameFont.Medium;
		Widgets.Label(UIUtility.TakeTopPart(ref inRect, 40f).LeftHalf(), psyset.Name);
		Text.Font = GameFont.Small;
		int group = DragAndDropWidget.NewGroup();
		Rect rect = inRect.LeftHalf().ContractedBy(3f);
		rect.xMax -= 8f;
		Widgets.Label(UIUtility.TakeTopPart(ref rect, 20f), "VPE.Contents".Translate());
		Widgets.DrawMenuSection(rect);
		DragAndDropWidget.DropArea(group, rect, delegate(object obj2)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			psyset.Abilities.Add((AbilityDef)obj2);
		}, null);
		Vector2 position = rect.position + new Vector2(8f, 8f);
		foreach (AbilityDef def in psyset.Abilities.ToList())
		{
			Rect rect2 = new Rect(position, new Vector2(36f, 36f));
			PsycastsUIUtility.DrawAbility(rect2, def);
			TooltipHandler.TipRegion(rect2, () => string.Format("{0}\n\n{1}\n\n{2}", ((Def)(object)def).LabelCap, ((Def)(object)def).description, "VPE.ClickRemove".Translate().Resolve().ToUpper()), ((object)def).GetHashCode() + 2);
			if (Widgets.ButtonInvisible(rect2))
			{
				psyset.Abilities.Remove(def);
			}
			position.x += 44f;
			if (position.x + 36f >= rect.xMax)
			{
				position.x = rect.xMin + 8f;
				position.y += 44f;
			}
		}
		Rect rect3 = inRect.RightHalf().ContractedBy(3f);
		Rect rect4 = UIUtility.TakeTopPart(ref rect3, 50f);
		Rect rect5 = UIUtility.TakeLeftPart(ref rect4, 40f).ContractedBy(0f, 5f);
		Rect rect6 = UIUtility.TakeRightPart(ref rect4, 40f).ContractedBy(0f, 5f);
		if (curIdx > 0 && Widgets.ButtonText(rect5, "<"))
		{
			curIdx--;
		}
		if (curIdx < paths.Count - 1 && Widgets.ButtonText(rect6, ">"))
		{
			curIdx++;
		}
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect4, $"{((paths.Count > 0) ? (curIdx + 1) : 0)} / {paths.Count}");
		Text.Anchor = TextAnchor.UpperLeft;
		if (paths.Count > 0)
		{
			PsycasterPathDef psycasterPathDef = paths[curIdx];
			PsycastsUIUtility.DrawPathBackground(ref rect3, psycasterPathDef);
			PsycastsUIUtility.DoPathAbilities(rect3, psycasterPathDef, abilityPos, delegate(Rect rect8, AbilityDef val2)
			{
				PsycastsUIUtility.DrawAbility(rect8, val2);
				if (compAbilities.HasAbility(val2))
				{
					DragAndDropWidget.Draggable(group, rect8, val2);
					TooltipHandler.TipRegion(rect8, () => $"{((Def)(object)val2).LabelCap}\n\n{((Def)(object)val2).description}", ((object)val2).GetHashCode() + 1);
				}
				else
				{
					Widgets.DrawRectFast(rect8, new Color(0f, 0f, 0f, 0.6f));
				}
			});
		}
		object obj = DragAndDropWidget.CurrentlyDraggedDraggable();
		AbilityDef val = (AbilityDef)((obj is AbilityDef) ? obj : null);
		if (val != null)
		{
			PsycastsUIUtility.DrawAbility(new Rect(Event.current.mousePosition, new Vector2(36f, 36f)), val);
		}
		Rect? rect7 = DragAndDropWidget.HoveringDropAreaRect(group);
		if (rect7.HasValue)
		{
			Rect valueOrDefault = rect7.GetValueOrDefault();
			Widgets.DrawHighlight(valueOrDefault);
		}
	}
}
