using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VEF.Abilities;
using VEF.Utils;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Dialog_CreatePsyring : Window
{
	private const float ABILITY_HEIGHT = 64f;

	private readonly Thing fuel;

	private readonly List<AbilityDef> possibleAbilities;

	private readonly Dictionary<string, string> truncationCache = new Dictionary<string, string>();

	private float lastHeight;

	private Pawn pawn;

	private Vector2 scrollPos;

	public override Vector2 InitialSize => new Vector2(400f, 800f);

	protected override float Margin => 3f;

	public Dialog_CreatePsyring(Pawn pawn, Thing fuel, List<AbilityDef> excludedAbilities = null)
	{
		this.pawn = pawn;
		this.fuel = fuel;
		forcePause = true;
		doCloseButton = false;
		doCloseX = true;
		closeOnClickedOutside = true;
		closeOnAccept = false;
		closeOnCancel = true;
		optionalTitle = "VPE.CreatePsyringTitle".Translate();
		possibleAbilities = (from ability in ((ThingWithComps)pawn).GetComp<CompAbilities>().LearnedAbilities
			let psycast = ability.def.Psycast()
			where psycast != null
			orderby psycast.path.label, psycast.level descending, psycast.order
			select ability.def).Except(pawn.AllAbilitiesFromPsyrings()).Except(excludedAbilities ?? Enumerable.Empty<AbilityDef>()).ToList();
	}

	private void Create(AbilityDef ability)
	{
		Psyring obj = (Psyring)ThingMaker.MakeThing(VPE_DefOf.VPE_Psyring);
		obj.Init(ability);
		GenPlace.TryPlaceThing(obj, fuel.PositionHeld, fuel.MapHeld, ThingPlaceMode.Near);
		if (fuel.stackCount == 1)
		{
			fuel.Destroy();
		}
		else
		{
			fuel.SplitOff(1).Destroy();
		}
	}

	public override void DoWindowContents(Rect inRect)
	{
		Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, lastHeight);
		float num = 5f;
		Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
		foreach (AbilityDef possibleAbility in possibleAbilities)
		{
			Rect rect = new Rect(5f, num, viewRect.width, 64f);
			Rect rect2 = UIUtility.TakeLeftPart(ref rect, 64f);
			rect.xMin += 5f;
			GUI.DrawTexture(rect2, (Texture)Command.BGTex);
			GUI.DrawTexture(rect2, (Texture)possibleAbility.icon);
			Widgets.Label(UIUtility.TakeTopPart(ref rect, 20f), ((Def)(object)possibleAbility).LabelCap);
			if (Widgets.ButtonText(UIUtility.TakeBottomPart(ref rect, 20f), "VPE.CreatePsyringButton".Translate()))
			{
				Create(possibleAbility);
				Close();
			}
			Text.Font = GameFont.Tiny;
			Widgets.Label(rect, ((Def)(object)possibleAbility).description.Truncate(rect.width, truncationCache));
			Text.Font = GameFont.Small;
			num += 69f;
		}
		lastHeight = num;
		Widgets.EndScrollView();
	}
}
