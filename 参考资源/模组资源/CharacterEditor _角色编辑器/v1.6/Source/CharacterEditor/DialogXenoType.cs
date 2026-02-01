using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CharacterEditor;

internal class DialogXenoType : GeneCreationDialogBase
{
	private List<GeneDef> selectedGenes = new List<GeneDef>();

	private bool inheritable;

	private bool? selectedCollapsed = false;

	private List<GeneCategoryDef> matchingCategories = new List<GeneCategoryDef>();

	private Dictionary<GeneCategoryDef, bool> collapsedCategories = new Dictionary<GeneCategoryDef, bool>();

	private bool hoveredAnyGene;

	private GeneDef hoveredGene;

	private static bool ignoreRestrictionsConfirmationSent;

	private const int MaxCustomXenotypes = 200;

	private static readonly Color OutlineColorSelected = new Color(1f, 1f, 0.7f, 1f);

	private Pawn pawn;

	private XenotypeDef predefinedXenoDef;

	private bool doOnce;

	public override Vector2 InitialSize => new Vector2(1036f, WindowTool.MaxH);

	protected override List<GeneDef> SelectedGenes => selectedGenes;

	protected override string Header => "CreateXenotype".Translate().CapitalizeFirst();

	protected override string AcceptButtonLabel => "SaveAndApply".Translate().CapitalizeFirst();

	internal DialogXenoType(Pawn _pawn)
	{
		predefinedXenoDef = null;
		pawn = _pawn;
		if (!pawn.genes.xenotypeName.NullOrEmpty())
		{
			xenotypeName = pawn.genes.xenotypeName;
		}
		else
		{
			xenotypeName = "";
		}
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.XenoType);
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnAccept = false;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		layer = CEditor.Layer;
		draggable = true;
		alwaysUseFullBiostatsTableHeight = true;
		searchWidgetOffsetX = (float)((double)GeneCreationDialogBase.ButSize.x * 2.0 + 4.0);
		foreach (GeneCategoryDef allDef in DefDatabase<GeneCategoryDef>.AllDefs)
		{
			collapsedCategories.Add(allDef, value: false);
		}
		OnGenesChanged();
	}

	public override void PostOpen()
	{
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.XenoType, ref windowRect, ref doOnce, 0);
		}
		if (!ModsConfig.BiotechActive)
		{
			Close(doCloseSound: false);
		}
		else
		{
			base.PostOpen();
		}
		if ((pawn.genes != null || pawn.genes.Xenotype.IsNullOrEmpty()) && DefDatabase<XenotypeDef>.AllDefs.Contains(pawn.genes.Xenotype) && pawn.genes.Xenotype != XenotypeDefOf.Baseliner)
		{
			ALoadXenotypeDef(pawn.genes.Xenotype);
			return;
		}
		List<GeneDef> list = new List<GeneDef>();
		foreach (Gene xenogene in pawn.genes.Xenogenes)
		{
			list.Add(xenogene.def);
		}
		CustomXenotype customXenotype = new CustomXenotype();
		customXenotype.name = pawn.genes.xenotypeName?.Trim();
		if (customXenotype.name.NullOrEmpty())
		{
			customXenotype.name = "";
		}
		customXenotype.genes.AddRange(list);
		customXenotype.inheritable = pawn.genes.Xenotype.inheritable;
		customXenotype.iconDef = pawn.genes.iconDef;
		if (customXenotype.name.NullOrEmpty())
		{
			ALoadCustomXenotype(customXenotype);
		}
		else
		{
			DoFileInteraction(customXenotype.name);
		}
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.XenoType, windowRect.position);
		base.Close(doCloseSound);
	}

	protected override void DrawGenes(Rect rect)
	{
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Invalid comparison between Unknown and I4
		hoveredAnyGene = false;
		GUI.BeginGroup(rect);
		float curY = 0f;
		DrawSection(new Rect(0f, 0f, rect.width, selectedHeight), selectedGenes, "SelectedGenes".Translate(), ref curY, ref selectedHeight, adding: false, rect, ref selectedCollapsed);
		if (!selectedCollapsed.Value)
		{
			curY += 10f;
		}
		float num = curY;
		Widgets.Label(0f, ref curY, rect.width, "Genes".Translate().CapitalizeFirst());
		float curY2 = curY + 10f;
		float height = (float)((double)curY2 - (double)num - 4.0);
		if (Widgets.ButtonText(new Rect((float)((double)rect.width - 150.0 - 16.0), num, 150f, height), "CollapseAllCategories".Translate()))
		{
			SoundDefOf.TabClose.PlayOneShotOnCamera();
			foreach (GeneCategoryDef allDef in DefDatabase<GeneCategoryDef>.AllDefs)
			{
				collapsedCategories[allDef] = true;
			}
		}
		if (Widgets.ButtonText(new Rect((float)((double)rect.width - 300.0 - 4.0 - 16.0), num, 150f, height), "ExpandAllCategories".Translate()))
		{
			SoundDefOf.TabOpen.PlayOneShotOnCamera();
			foreach (GeneCategoryDef allDef2 in DefDatabase<GeneCategoryDef>.AllDefs)
			{
				collapsedCategories[allDef2] = false;
			}
		}
		float num2 = curY2;
		Rect rect2 = new Rect(0f, curY2, rect.width - 16f, scrollHeight);
		Widgets.BeginScrollView(new Rect(0f, curY2, rect.width, rect.height - curY2), ref scrollPosition, rect2);
		Rect containingRect = rect2;
		containingRect.y = curY2 + scrollPosition.y;
		containingRect.height = rect.height;
		bool? collapsed = null;
		DrawSection(rect, GeneUtility.GenesInOrder, null, ref curY2, ref unselectedHeight, adding: true, containingRect, ref collapsed);
		if ((int)Event.current.type == 8)
		{
			scrollHeight = curY2 - num2;
		}
		Widgets.EndScrollView();
		GUI.EndGroup();
		if (!hoveredAnyGene)
		{
			hoveredGene = null;
		}
	}

	private void DrawSection(Rect rect, List<GeneDef> genes, string label, ref float curY, ref float sectionHeight, bool adding, Rect containingRect, ref bool? collapsed)
	{
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Invalid comparison between Unknown and I4
		//IL_0784: Unknown result type (might be due to invalid IL or missing references)
		//IL_078a: Invalid comparison between Unknown and I4
		//IL_067e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0684: Invalid comparison between Unknown and I4
		float curX = 4f;
		if (!label.NullOrEmpty())
		{
			Rect rect2 = new Rect(0f, curY, rect.width, Text.LineHeight);
			rect2.xMax -= (adding ? 16f : (Text.CalcSize("ClickToAddOrRemove".Translate()).x + 4f));
			if (collapsed.HasValue)
			{
				Rect rect3 = new Rect(rect2.x, rect2.y + (float)(((double)rect2.height - 18.0) / 2.0), 18f, 18f);
				GUI.DrawTexture(rect3, (Texture)(collapsed.Value ? TexButton.Reveal : TexButton.Collapse));
				if (Widgets.ButtonInvisible(rect2))
				{
					bool? flag = !collapsed;
					collapsed = flag;
					if (collapsed.Value)
					{
						SoundDefOf.TabClose.PlayOneShotOnCamera();
					}
					else
					{
						SoundDefOf.TabOpen.PlayOneShotOnCamera();
					}
				}
				if (Mouse.IsOver(rect2))
				{
					Widgets.DrawHighlight(rect2);
				}
				rect2.xMin += rect3.width;
			}
			Widgets.Label(rect2, label);
			if (!adding)
			{
				Text.Anchor = TextAnchor.UpperRight;
				GUI.color = ColoredText.SubtleGrayColor;
				Widgets.Label(new Rect(rect2.xMax - 18f, curY, rect.width - rect2.width, Text.LineHeight), "ClickToAddOrRemove".Translate());
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperLeft;
			}
			curY += Text.LineHeight + 3f;
		}
		if (collapsed == true)
		{
			if ((int)Event.current.type == 8)
			{
				sectionHeight = 0f;
			}
			return;
		}
		float num = curY;
		bool flag2 = false;
		float num2 = (float)(34.0 + (double)GeneCreationDialogBase.GeneSize.x + 8.0);
		float num3 = rect.width - 16f;
		float num4 = num2 + 4f;
		float b = (float)(((double)num3 - (double)num4 * (double)Mathf.Floor(num3 / num4)) / 2.0);
		Rect rect4 = new Rect(0f, curY, rect.width, sectionHeight);
		if (!adding)
		{
			Widgets.DrawRectFast(rect4, Widgets.MenuSectionBGFillColor);
		}
		curY += 4f;
		if (!genes.Any())
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = ColoredText.SubtleGrayColor;
			Widgets.Label(rect4, "(" + "NoneLower".Translate() + ")");
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
		else
		{
			GeneCategoryDef geneCategoryDef = null;
			int num5 = 0;
			for (int i = 0; i < genes.Count; i++)
			{
				GeneDef geneDef = genes[i];
				if ((adding && quickSearchWidget.filter.Active && (!matchingGenes.Contains(geneDef) || selectedGenes.Contains(geneDef)) && !matchingCategories.Contains(geneDef.displayCategory)) || (!ignoreRestrictions && geneDef.biostatArc > 0))
				{
					continue;
				}
				bool flag3 = false;
				if ((double)curX + (double)num2 > (double)num3)
				{
					curX = 4f;
					curY += (float)((double)GeneCreationDialogBase.GeneSize.y + 8.0 + 4.0);
					flag3 = true;
				}
				bool flag4 = quickSearchWidget.filter.Active && (matchingGenes.Contains(geneDef) || matchingCategories.Contains(geneDef.displayCategory));
				bool flag5 = collapsedCategories[geneDef.displayCategory] && !flag4;
				if (adding && geneCategoryDef != geneDef.displayCategory)
				{
					if (!flag3 && flag2)
					{
						curX = 4f;
						curY += (float)((double)GeneCreationDialogBase.GeneSize.y + 8.0 + 4.0);
					}
					geneCategoryDef = geneDef.displayCategory;
					Rect rect5 = new Rect(curX, curY, rect.width - 8f, Text.LineHeight);
					if (!flag4)
					{
						Rect rect6 = new Rect(rect5.x, rect5.y + (float)(((double)rect5.height - 18.0) / 2.0), 18f, 18f);
						GUI.DrawTexture(rect6, (Texture)(flag5 ? TexButton.Reveal : TexButton.Collapse));
						if (Widgets.ButtonInvisible(rect5))
						{
							collapsedCategories[geneDef.displayCategory] = !collapsedCategories[geneDef.displayCategory];
							if (collapsedCategories[geneDef.displayCategory])
							{
								SoundDefOf.TabClose.PlayOneShotOnCamera();
							}
							else
							{
								SoundDefOf.TabOpen.PlayOneShotOnCamera();
							}
						}
						if (num5 % 2 == 1)
						{
							Widgets.DrawLightHighlight(rect5);
						}
						if (Mouse.IsOver(rect5))
						{
							Widgets.DrawHighlight(rect5);
						}
						rect5.xMin += rect6.width;
					}
					Widgets.Label(rect5, geneCategoryDef.LabelCap);
					curY += rect5.height;
					if (!flag5)
					{
						GUI.color = Color.grey;
						Widgets.DrawLineHorizontal(curX, curY, rect.width - 8f);
						GUI.color = Color.white;
						curY += 10f;
					}
					num5++;
				}
				if (adding && flag5)
				{
					flag2 = false;
					if ((int)Event.current.type == 8)
					{
						sectionHeight = curY - num;
					}
					continue;
				}
				curX = Mathf.Max(curX, b);
				flag2 = true;
				if (DrawGene(geneDef, !adding, ref curX, curY, num2, containingRect, flag4))
				{
					if (selectedGenes.Contains(geneDef))
					{
						SoundDefOf.Tick_Low.PlayOneShotOnCamera();
						selectedGenes.Remove(geneDef);
					}
					else
					{
						SoundDefOf.Tick_High.PlayOneShotOnCamera();
						selectedGenes.Add(geneDef);
					}
					if (!xenotypeNameLocked)
					{
						xenotypeName = GeneUtility.GenerateXenotypeNameFromGenes(SelectedGenes);
					}
					OnGenesChanged();
					break;
				}
			}
		}
		if (!adding || flag2)
		{
			curY += GeneCreationDialogBase.GeneSize.y + 12f;
		}
		if ((int)Event.current.type == 8)
		{
			sectionHeight = curY - num;
		}
	}

	private bool DrawGene(GeneDef geneDef, bool selectedSection, ref float curX, float curY, float packWidth, Rect containingRect, bool isMatch)
	{
		bool result = false;
		Rect rect = new Rect(curX, curY, packWidth, GeneCreationDialogBase.GeneSize.y + 8f);
		if (!containingRect.Overlaps(rect))
		{
			curX = rect.xMax + 4f;
			return false;
		}
		bool selected = !selectedSection && selectedGenes.Contains(geneDef);
		bool overridden = leftChosenGroups.Any((GeneLeftChosenGroup x) => x.overriddenGenes.Contains(geneDef));
		Widgets.DrawOptionBackground(rect, selected);
		curX += 4f;
		GeneUIUtility.DrawBiostats(geneDef.biostatCpx, geneDef.biostatMet, geneDef.biostatArc, ref curX, curY, 4f);
		Rect rect2 = new Rect(curX, curY + 4f, GeneCreationDialogBase.GeneSize.x, GeneCreationDialogBase.GeneSize.y);
		if (isMatch)
		{
			Widgets.DrawStrongHighlight(rect2.ExpandedBy(6f));
		}
		GeneUIUtility.DrawGeneDef(geneDef, rect2, (!inheritable) ? GeneType.Xenogene : GeneType.Endogene, () => GeneTip(geneDef, selectedSection), doBackground: false, clickable: false, overridden);
		curX += GeneCreationDialogBase.GeneSize.x + 4f;
		if (Mouse.IsOver(rect))
		{
			hoveredGene = geneDef;
			hoveredAnyGene = true;
		}
		else if (hoveredGene != null && geneDef.ConflictsWith(hoveredGene))
		{
			Widgets.DrawLightHighlight(rect);
		}
		if (Widgets.ButtonInvisible(rect))
		{
			result = true;
		}
		curX = Mathf.Max(curX, rect.xMax + 4f);
		return result;
	}

	private string GeneTip(GeneDef geneDef, bool selectedSection)
	{
		string text = null;
		if (selectedSection)
		{
			if (leftChosenGroups.Any((GeneLeftChosenGroup x) => x.leftChosen == geneDef))
			{
				text = GroupInfo(leftChosenGroups.FirstOrDefault((GeneLeftChosenGroup x) => x.leftChosen == geneDef));
			}
			else if (cachedOverriddenGenes.Contains(geneDef))
			{
				text = GroupInfo(leftChosenGroups.FirstOrDefault((GeneLeftChosenGroup x) => x.overriddenGenes.Contains(geneDef)));
			}
			else if (randomChosenGroups.ContainsKey(geneDef))
			{
				text = ("GeneWillBeRandomChosen".Translate() + ":\n" + randomChosenGroups[geneDef].Select((GeneDef x) => x.label).ToLineList("  - ", capitalizeItems: true)).Colorize(ColoredText.TipSectionTitleColor);
			}
		}
		if (selectedGenes.Contains(geneDef) && geneDef.prerequisite != null && !selectedGenes.Contains(geneDef.prerequisite))
		{
			if (!text.NullOrEmpty())
			{
				text += "\n\n";
			}
			text += ("MessageGeneMissingPrerequisite".Translate(geneDef.label).CapitalizeFirst() + ": " + geneDef.prerequisite.LabelCap).Colorize(ColorLibrary.RedReadable);
		}
		if (!text.NullOrEmpty())
		{
			text += "\n\n";
		}
		return text + (selectedGenes.Contains(geneDef) ? "ClickToRemove" : "ClickToAdd").Translate().Colorize(ColoredText.SubtleGrayColor);
		static string GroupInfo(GeneLeftChosenGroup group)
		{
			return (group == null) ? null : ("GeneLeftmostActive".Translate() + ":\n  - " + group.leftChosen.LabelCap + " (" + "Active".Translate() + ")" + "\n" + group.overriddenGenes.Select((GeneDef x) => (x.label + " (" + "Suppressed".Translate() + ")").Colorize(ColorLibrary.RedReadable)).ToLineList("  - ", capitalizeItems: true)).Colorize(ColoredText.TipSectionTitleColor);
		}
	}

	protected override void PostXenotypeOnGUI(float curX, float curY)
	{
		TaggedString taggedString = "GenesAreInheritable".Translate();
		TaggedString taggedString2 = "IgnoreRestrictions".Translate();
		float width = (float)((double)Mathf.Max(Text.CalcSize(taggedString).x, Text.CalcSize(taggedString2).x) + 4.0 + 24.0);
		Rect rect = new Rect(curX, curY, width, Text.LineHeight);
		Widgets.CheckboxLabeled(rect, taggedString, ref inheritable);
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
			TooltipHandler.TipRegion(rect, "GenesAreInheritableDesc".Translate());
		}
		rect.y += Text.LineHeight;
		int num = (ignoreRestrictions ? 1 : 0);
		Widgets.CheckboxLabeled(rect, taggedString2, ref ignoreRestrictions);
		int num2 = (ignoreRestrictions ? 1 : 0);
		if (num != num2)
		{
			if (ignoreRestrictions)
			{
				if (!ignoreRestrictionsConfirmationSent)
				{
					ignoreRestrictionsConfirmationSent = true;
					WindowTool.Open(new Dialog_MessageBox("IgnoreRestrictionsConfirmation".Translate(), "Yes".Translate(), delegate
					{
					}, "No".Translate(), delegate
					{
						ignoreRestrictions = false;
					}));
				}
			}
			else
			{
				selectedGenes.RemoveAll((GeneDef x) => x.biostatArc > 0);
				OnGenesChanged();
			}
		}
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
			TooltipHandler.TipRegion(rect, "IgnoreRestrictionsDesc".Translate());
		}
		postXenotypeHeight += rect.yMax - curY;
	}

	protected override void OnGenesChanged()
	{
		selectedGenes.SortGeneDefs();
		base.OnGenesChanged();
		if (predefinedXenoDef == null)
		{
			return;
		}
		foreach (GeneDef allGene in predefinedXenoDef.AllGenes)
		{
			if (!selectedGenes.Contains(allGene))
			{
				MessageTool.DebugPrint("predefined unloaded");
				predefinedXenoDef = null;
				break;
			}
		}
		int num = selectedGenes.CountAllowNull();
		int num2 = predefinedXenoDef.AllGenes.CountAllowNull();
		if (num != num2)
		{
			MessageTool.DebugPrint("predefined unloaded");
			predefinedXenoDef = null;
		}
	}

	private void ALoadCustomXenotype(CustomXenotype xenotype)
	{
		MessageTool.DebugPrint("loading custom xenotype " + xenotype.name);
		predefinedXenoDef = null;
		xenotypeName = xenotype.name;
		xenotypeNameLocked = false;
		selectedGenes.Clear();
		selectedGenes.AddRange(xenotype.genes);
		inheritable = xenotype.inheritable;
		iconDef = xenotype.IconDef;
		OnGenesChanged();
		ignoreRestrictions = selectedGenes.Any((GeneDef x) => x.biostatArc > 0) || !WithinAcceptableBiostatLimits(showMessage: false);
	}

	private void ALoadXenotypeDef(XenotypeDef xenotype)
	{
		MessageTool.DebugPrint("loading xenotypeDef " + xenotype.label);
		predefinedXenoDef = xenotype;
		xenotypeName = xenotype.label;
		xenotypeNameLocked = false;
		selectedGenes.Clear();
		selectedGenes.AddRange(xenotype.genes);
		inheritable = xenotype.inheritable;
		iconDef = XenotypeIconDefOf.Basic;
		OnGenesChanged();
		ignoreRestrictions = selectedGenes.Any((GeneDef g) => g.biostatArc > 0) || !WithinAcceptableBiostatLimits(showMessage: false);
	}

	protected void DoFileInteraction(string fileName)
	{
		string filePath = GenFilePaths.AbsFilePathForXenotype(fileName);
		PreLoadUtility.CheckVersionAndLoad(filePath, ScribeMetaHeaderUtility.ScribeHeaderMode.Xenotype, delegate
		{
			if (GameDataSaveLoader.TryLoadXenotype(filePath, out var xenotype))
			{
				ALoadCustomXenotype(xenotype);
			}
		});
	}

	protected override void DrawSearchRect(Rect rect)
	{
		base.DrawSearchRect(rect);
		if (Widgets.ButtonText(new Rect(rect.xMax - GeneCreationDialogBase.ButSize.x, rect.y, GeneCreationDialogBase.ButSize.x, GeneCreationDialogBase.ButSize.y), "LoadCustom".Translate()))
		{
			WindowTool.Open(new Dialog_XenotypeList_Load(delegate(CustomXenotype xenotype2)
			{
				ALoadCustomXenotype(xenotype2);
			}));
		}
		if (!Widgets.ButtonText(new Rect((float)((double)rect.xMax - (double)GeneCreationDialogBase.ButSize.x * 2.0 - 4.0), rect.y, GeneCreationDialogBase.ButSize.x, GeneCreationDialogBase.ButSize.y), "LoadPremade".Translate()))
		{
			return;
		}
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (XenotypeDef item in DefDatabase<XenotypeDef>.AllDefs.OrderBy((XenotypeDef c) => 0f - c.displayPriority))
		{
			XenotypeDef xenotype = item;
			list.Add(new FloatMenuOption(xenotype.LabelCap, delegate
			{
				ALoadXenotypeDef(xenotype);
			}, xenotype.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default, delegate(Rect r)
			{
				TooltipHandler.TipRegion(r, xenotype.descriptionShort ?? xenotype.description);
			}));
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}

	protected override void DoBottomButtons(Rect rect)
	{
		SZWidgets.ButtonText(new Rect(rect.xMax - GeneCreationDialogBase.ButSize.x - 10f, rect.y, GeneCreationDialogBase.ButSize.x + 10f, GeneCreationDialogBase.ButSize.y), AcceptButtonLabel, delegate
		{
			ACheckSaveAnd(apply: true);
		});
		SZWidgets.ButtonText(new Rect(rect.x, rect.y, GeneCreationDialogBase.ButSize.x, GeneCreationDialogBase.ButSize.y), "Close".Translate(), delegate
		{
			Close();
		});
		SZWidgets.ButtonText(new Rect(rect.x + rect.width - 270f, rect.y, 110f, 38f), Label.SAVE, delegate
		{
			ACheckSaveAnd(apply: false);
		});
		if (leftChosenGroups.Any())
		{
			int num = leftChosenGroups.Sum((GeneLeftChosenGroup geneLeftChosenGroup2) => geneLeftChosenGroup2.overriddenGenes.Count);
			GeneLeftChosenGroup geneLeftChosenGroup = leftChosenGroups[0];
			string text = "GenesConflict".Translate() + ": " + "GenesConflictDesc".Translate(geneLeftChosenGroup.leftChosen.Named("FIRST"), geneLeftChosenGroup.overriddenGenes[0].Named("SECOND")).CapitalizeFirst() + ((num > 1) ? (" +" + (num - 1)) : string.Empty);
			float x = Text.CalcSize(text).x;
			GUI.color = ColorLibrary.RedReadable;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect((float)((double)rect.xMax - (double)GeneCreationDialogBase.ButSize.x - (double)x - 4.0), rect.y, x, rect.height), text);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}
	}

	protected override bool CanAccept()
	{
		if (!base.CanAccept())
		{
			return false;
		}
		if (!selectedGenes.Any())
		{
			return true;
		}
		for (int i = 0; i < selectedGenes.Count; i++)
		{
			if (selectedGenes[i].prerequisite != null && !selectedGenes.Contains(selectedGenes[i].prerequisite))
			{
				MessageTool.Show("MessageGeneMissingPrerequisite".Translate(selectedGenes[i].label).CapitalizeFirst() + ": " + selectedGenes[i].prerequisite.LabelCap, MessageTypeDefOf.RejectInput);
				return false;
			}
		}
		if (GenFilePaths.AllCustomXenotypeFiles.EnumerableCount() >= 200)
		{
			MessageTool.Show("MessageTooManyCustomXenotypes".Translate(), MessageTypeDefOf.RejectInput);
			return false;
		}
		if (ignoreRestrictions || !leftChosenGroups.Any())
		{
			return true;
		}
		MessageTool.Show("MessageConflictingGenesPresent".Translate(), MessageTypeDefOf.RejectInput);
		return false;
	}

	protected override void Accept()
	{
		ASaveAnd(use: true);
	}

	private void ACheckSaveAnd(bool apply)
	{
		if (CanAccept())
		{
			ASaveAnd(apply);
		}
	}

	private void ASaveAnd(bool use)
	{
		IEnumerable<string> warnings = GetWarnings();
		if (warnings.Any())
		{
			WindowTool.Open(Dialog_MessageBox.CreateConfirmation("XenotypeBreaksLimits".Translate() + ":\n" + warnings.ToLineList("  - ", capitalizeItems: true) + "\n\n" + "SaveAnyway".Translate(), delegate
			{
				AcceptInner(use);
			}));
		}
		else
		{
			AcceptInner(use);
		}
	}

	private void AcceptInner(bool saveAndUse)
	{
		if (xenotypeName.NullOrEmpty())
		{
			MessageTool.DebugPrint("please choose a xenotype name!");
			return;
		}
		CustomXenotype customXenotype = new CustomXenotype();
		customXenotype.name = xenotypeName?.Trim();
		customXenotype.genes.AddRange(selectedGenes);
		customXenotype.inheritable = inheritable;
		customXenotype.iconDef = iconDef;
		string absPath = GenFilePaths.AbsFilePathForXenotype(GenFile.SanitizedFileName(customXenotype.name));
		LongEventHandler.QueueLongEvent(delegate
		{
			GameDataSaveLoader.SaveXenotype(customXenotype, absPath);
		}, "SavingLongEvent", doAsynchronously: false, null);
		if (saveAndUse)
		{
			pawn.SetPawnXenotype(customXenotype, !inheritable);
		}
		Close();
	}

	private IEnumerable<string> GetWarnings()
	{
		DialogXenoType dialogCreateXenotype = this;
		if (dialogCreateXenotype.ignoreRestrictions)
		{
			if (dialogCreateXenotype.arc > 0 && dialogCreateXenotype.inheritable)
			{
				yield return "XenotypeBreaksLimits_Archites".Translate();
			}
			if (dialogCreateXenotype.met > GeneTuning.BiostatRange.TrueMax)
			{
				yield return "XenotypeBreaksLimits_Exceeds".Translate("Metabolism".Translate().ToLower().Named("STAT"), dialogCreateXenotype.met.Named("VALUE"), GeneTuning.BiostatRange.TrueMax.Named("MAX"));
			}
			else if (dialogCreateXenotype.met < GeneTuning.BiostatRange.TrueMin)
			{
				yield return "XenotypeBreaksLimits_Below".Translate("Metabolism".Translate().ToLower().Named("STAT"), dialogCreateXenotype.met.Named("VALUE"), GeneTuning.BiostatRange.TrueMin.Named("MIN"));
			}
		}
	}

	protected override void UpdateSearchResults()
	{
		quickSearchWidget.noResultsMatched = false;
		matchingGenes.Clear();
		matchingCategories.Clear();
		if (!quickSearchWidget.filter.Active)
		{
			return;
		}
		foreach (GeneDef item in GeneUtility.GenesInOrder)
		{
			if (!selectedGenes.Contains(item))
			{
				if (quickSearchWidget.filter.Matches(item.label))
				{
					matchingGenes.Add(item);
				}
				if (quickSearchWidget.filter.Matches(item.displayCategory.label) && !matchingCategories.Contains(item.displayCategory))
				{
					matchingCategories.Add(item.displayCategory);
				}
			}
		}
		quickSearchWidget.noResultsMatched = !matchingGenes.Any() && !matchingCategories.Any();
	}
}
