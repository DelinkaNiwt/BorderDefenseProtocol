using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class CompMilianHairSwitch : ThingComp
{
	public int num = Rand.Range(0, 100);

	public bool drawHair = true;

	public string frontHairPath;

	public string behindHairPath;

	public Color colorOverride;

	protected Pawn Pawn
	{
		get
		{
			if (parent is Pawn result)
			{
				return result;
			}
			return null;
		}
	}

	private CompProperties_MilianHairSwitch Props => (CompProperties_MilianHairSwitch)props;

	public List<string> frontHairPaths => Props.frontHairPaths;

	private List<string> behindHairPaths => Props.behindHairPaths;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref num, "num", 0);
		Scribe_Values.Look(ref frontHairPath, "frontHairPath");
		Scribe_Values.Look(ref behindHairPath, "behindHairPath");
		Scribe_Values.Look(ref drawHair, "drawHair", defaultValue: true);
		Scribe_Values.Look(ref colorOverride, "colorOverride");
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		ChangeGraphic(num);
		DrawHairBool();
	}

	public void DrawHairBool()
	{
		List<Apparel> wornApparel = Pawn.apparel.WornApparel;
		foreach (Apparel item in wornApparel)
		{
			if (item.def.apparel.LastLayer == ApparelLayerDefOf.Overhead && !item.def.apparel.renderSkipFlags.NullOrEmpty() && item.def.apparel.renderSkipFlags.Contains(RenderSkipFlagDefOf.Hair))
			{
				drawHair = false;
				break;
			}
			drawHair = true;
		}
	}

	public void ChangeGraphic(int index)
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			if (index > frontHairPaths.Count - 1)
			{
				index %= frontHairPaths.Count;
			}
			if (Pawn != null && frontHairPaths.Count > 0)
			{
				frontHairPath = frontHairPaths[index];
				behindHairPath = behindHairPaths[index];
				PawnRenderer pawnRenderer = Pawn?.Drawer?.renderer;
				Pawn?.Drawer?.renderer?.renderTree?.SetDirty();
			}
		});
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Pawn.Dead || !Pawn.Awake() || Pawn.Faction != Faction.OfPlayer)
		{
			yield break;
		}
		yield return new Command_Action
		{
			defaultLabel = "Milira.HairChange".Translate(),
			defaultDesc = "Milira.HairChangeDesc".Translate(),
			icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath),
			action = delegate
			{
				Find.WindowStack.Add(new Dialog_MilianHairStyleConfig(Pawn, colorOverride, delegate(Color newColor)
				{
					colorOverride = newColor;
				}, ""));
			}
		};
	}
}
