using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogViewXenoGenes : Window
{
	private bool bDoOnce;

	private Pawn target;

	private Vector2 scrollPosition;

	private const float HeaderHeight = 30f;

	private List<GeneDef> lallgenes;

	private List<XenotypeDef> lxenotypes;

	private List<CustomXenotype> lcustomxeontypes;

	private bool bIsXeno = false;

	public override Vector2 InitialSize => new Vector2(736f, WindowTool.MaxHS);

	internal DialogViewXenoGenes(Pawn target)
	{
		this.target = target;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
		bDoOnce = true;
		SearchTool.Update(SearchTool.SIndex.ViewXenoType);
		lallgenes = DefDatabase<GeneDef>.AllDefs.OrderBy((GeneDef x) => x.defName).ToList();
		lxenotypes = (from x in DefDatabase<XenotypeDef>.AllDefs
			where !x.defName.NullOrEmpty()
			orderby 0f - x.displayPriority
			select x).ToList();
		InitCustomList();
	}

	private void InitCustomList()
	{
		lcustomxeontypes = GeneTool.GetAllCustomXenotypes();
	}

	public override void PostOpen()
	{
		if (!ModsConfig.BiotechActive)
		{
			Close(doCloseSound: false);
		}
		else
		{
			base.PostOpen();
		}
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (bDoOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.ViewXenoType, ref windowRect, ref bDoOnce, 370);
		}
		inRect.yMax -= Window.CloseButSize.y;
		GUI.color = Color.white;
		inRect.yMin += 34f;
		Vector2 size = Vector2.zero;
		int num = (int)inRect.x;
		int num2 = (int)inRect.height + 34;
		SZWidgets.ButtonImageCol(new Rect(num, 0f, 30f, 30f), bIsXeno ? "bdnax" : "bdnae", delegate
		{
			bIsXeno = !bIsXeno;
		}, Color.white, Label.TOGGLEENDOXENO);
		num += 32;
		Rect rect = new Rect(num, 0f, 30f, 30f);
		SZWidgets.Image(rect, bIsXeno ? "UI/Icons/Genes/GeneBackground_Xenogene" : "UI/Icons/Genes/GeneBackground_Endogene");
		SZWidgets.ButtonImage(rect, "bplus2", AOpenAddDialog);
		num += 32;
		Rect rect2 = new Rect(num, 0f, 30f, 30f);
		SZWidgets.Image(rect2, bIsXeno ? "UI/Icons/Genes/GeneBackground_Xenogene" : "UI/Icons/Genes/GeneBackground_Endogene");
		SZWidgets.FloatMenuOnButtonImage(rect2, ContentFinder<Texture2D>.Get("bminus2"), bIsXeno ? target.genes.Xenogenes : target.genes.Endogenes, (Gene gene) => gene.LabelCap.ToString(), ARemoveEndoGene);
		num += 32;
		Rect rect3 = new Rect(num, 0f, 30f, 30f);
		SZWidgets.Image(rect3, bIsXeno ? "UI/Icons/Genes/GeneBackground_Xenogene" : "UI/Icons/Genes/GeneBackground_Endogene");
		SZWidgets.ButtonImage(rect3, "breset", AResetGenes, Label.TIPCLEARGENES);
		num += 32;
		Rect rect4 = new Rect(num, 0f, 30f, 30f);
		SZWidgets.Image(rect4, bIsXeno ? "UI/Icons/Genes/GeneBackground_Xenogene" : "UI/Icons/Genes/GeneBackground_Endogene");
		SZWidgets.FloatMixedMenuOnButtonImage(rect4, target.genes.XenotypeIcon, lxenotypes, lcustomxeontypes, (XenotypeDef xt) => xt.LabelCap.ToString(), (CustomXenotype c) => c.name, AChangeXenotype, ALoadCustomXenotype, Label.LOADXENOTYPEKEEP);
		num += 32;
		Text.Font = GameFont.Medium;
		SZWidgets.Label(new Rect(170f, 0f, 400f, 30f), Label.GENETICS + " - " + target.GetPawnNameColored(needFull: true));
		Text.Font = GameFont.Small;
		GeneUIUtility.DrawGenesInfo(inRect, target, InitialSize.y, ref size, ref scrollPosition);
		WindowTool.SimpleCloseButton(this);
		Rect rect5 = WindowTool.RAcceptButton(this);
		rect5.y -= 80f;
		rect5.x -= 50f;
		rect5.width += 50f;
		rect5.height = 50f;
		if (Mouse.IsOver(rect5))
		{
			TooltipHandler.TipRegion(rect5, GeneTool.PrintIfXenotypeIsPrefered(target));
		}
	}

	private void ALoadCustomXenotype(CustomXenotype c)
	{
		target.SetPawnXenotype(c, bIsXeno);
		GeneTool.PrintIfXenotypeIsPrefered(target);
	}

	private void AChangeXenotype(XenotypeDef def)
	{
		target.SetPawnXenotype(def, bIsXeno);
		GeneTool.PrintIfXenotypeIsPrefered(target);
	}

	private void AResetGenes()
	{
		target.ClearGenes(bIsXeno, Event.current.control);
		CEditor.API.UpdateGraphics();
	}

	private void AOpenAddDialog()
	{
		WindowTool.Open(new DialogGenery(bIsXeno));
	}

	private void ARemoveEndoGene(Gene gene)
	{
		target.RemoveGeneKeepFirst(gene);
		GeneTool.PrintIfXenotypeIsPrefered(CEditor.API.Pawn);
		CEditor.API.UpdateGraphics();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.ViewXenoType, windowRect.position);
		base.Close(doCloseSound);
	}
}
