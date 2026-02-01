using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NiceInventoryTab;

public class Dialog_OptimizeEquipment : Window
{
	private Vector2 size = new Vector2(800f, 490f);

	private Pawn pawn;

	private List<ApparelLayerDef> AllLayers;

	private List<ApparelLayerDef> AvailableLayers;

	private List<ApparelLayerDef> SelectedLayersToOptimize;

	private List<ApparelLayerDef> OccupiedSlots;

	private List<Apparel> FinalApparel;

	private static bool CanTakeOff = false;

	private int Step;

	private static FieldInfo jobsGivenThisTick = typeof(Pawn_JobTracker).GetField("jobsGivenThisTick", BindingFlags.Instance | BindingFlags.NonPublic);

	private Vector2 finalAppScrollPosAll = Vector2.zero;

	private List<StatDef> allStats;

	private List<StatDef> statsToOptimize;

	private StatDef selectedStatInLists;

	private Vector2 statScrollPosAll = Vector2.zero;

	private Vector2 statScrollPosOptimize = Vector2.zero;

	private Vector2 slotsScrollPos = Vector2.zero;

	private Dictionary<Apparel, string> FittedDict = new Dictionary<Apparel, string>();

	public override Vector2 InitialSize => size;

	public Dialog_OptimizeEquipment(Pawn selPawn)
	{
		forcePause = true;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnAccept = false;
		closeOnCancel = true;
		pawn = selPawn;
		FillInitialStats();
		AllLayers = (from x in ApparelSlotUtility.AllPotentialSlots
			where x.possibleApparel.Any()
			select x.layer).Distinct().ToList();
		AvailableLayers = ApparelSlotUtility.GetAvailableLayers(pawn, null);
		SelectedLayersToOptimize = AvailableLayers.ToList();
		OccupiedSlots = pawn.apparel?.WornApparel.Where((Apparel x) => x.def.IsApparel).SelectMany((Apparel x) => x.def.apparel.layers).Distinct()
			.ToList();
	}

	private void FillInitialStats()
	{
		allStats = new List<StatDef>();
		statsToOptimize = new List<StatDef>();
		allStats.Add(StatDefOf.ArmorRating_Sharp);
		allStats.Add(StatDefOf.ArmorRating_Blunt);
		allStats.Add(StatDefOf.ArmorRating_Heat);
		allStats.Add(StatDefOf.MoveSpeed);
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "EquipmentMaster".Translate() + " WORK IN PROGRESS".Colorize(Color.yellow));
		Text.Font = GameFont.Small;
		Rect content = inRect.AtZero().ContractedBy(8f);
		content.yMin = 60f;
		switch (Step)
		{
		case 0:
			StepOne(content);
			break;
		case 1:
			StepTwo(content);
			break;
		}
	}

	private void DecreateJobCounter(Pawn_JobTracker pt)
	{
		jobsGivenThisTick.SetValue(pt, (int)jobsGivenThisTick.GetValue(pt) - 1);
	}

	private void StepTwo(Rect content)
	{
		(Rect top, Rect bottom) tuple = Utils.SplitRectByBottomPart(content, 40f, 8f);
		Rect item = tuple.top;
		Rect item2 = tuple.bottom;
		Rect item3 = Utils.SplitRect(item, 0.3f, 40f).left;
		DrawFinalApparelList(item3);
		(Rect left, Rect right) tuple2 = Utils.SplitRect(item2, 0.5f);
		Rect item4 = tuple2.left;
		Rect item5 = tuple2.right;
		item4.width = 100f;
		item5.xMin = item5.xMax - 100f;
		if (Button(item4, "Back", active: true))
		{
			Step--;
		}
		if (Button(item5, "Wear", active: true))
		{
			CommandWear();
			Close();
		}
	}

	private void CommandWear()
	{
		if (pawn.apparel == null)
		{
			return;
		}
		HashSet<Apparel> hashSet = pawn.apparel.WornApparel.ToHashSet();
		HashSet<Apparel> hashSet2 = FinalApparel.ToHashSet();
		foreach (Apparel item in hashSet2)
		{
			if (!hashSet.Contains(item) && item != null && item.Spawned && pawn.CanReserveAndReach(item, PathEndMode.Touch, Danger.Some))
			{
				CommandWear(item);
			}
		}
		foreach (Apparel item2 in hashSet)
		{
			if (!hashSet2.Contains(item2))
			{
				CommandDrop(item2);
			}
		}
	}

	private void CommandDrop(Thing app)
	{
		Job job = JobMaker.MakeJob(JobDefOf.RemoveApparel, app);
		job.playerForced = true;
		pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
		DecreateJobCounter(pawn.jobs);
	}

	private void CommandWear(Thing app)
	{
		Job job = JobMaker.MakeJob(JobDefOf.Wear, app);
		job.playerForced = true;
		pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
		DecreateJobCounter(pawn.jobs);
	}

	private void DrawFinalApparelList(Rect rect)
	{
		float num = 32f;
		float height = (float)FinalApparel.Count * num;
		rect = rect.ExpandedBy(-4f);
		BlockDescr(rect.ContractedBy(-4f), "Final apparel");
		GUI.color = Color.gray;
		Widgets.DrawBox(rect.ContractedBy(-4f), 2);
		Widgets.BeginScrollView(rect, ref finalAppScrollPosAll, new Rect(0f, 0f, rect.width - 16f, height));
		float num2 = 0f;
		foreach (Apparel item in FinalApparel)
		{
			Rect rect2 = new Rect(0f, num2, rect.width - 16f, num);
			if (Mouse.IsOver(rect2))
			{
				Widgets.DrawLightHighlight(rect2);
			}
			DrawApparel(rect2.ContractedBy(2f), item);
			num2 += num;
		}
		Widgets.EndScrollView();
	}

	private void StepOne(Rect content)
	{
		var (org, rect) = Utils.SplitRectByBottomPart(content, 40f, 8f);
		var (rect2, rect3) = Utils.SplitRect(org, 0.3f, 40f);
		DrawSlotsList(rect2);
		var (rect4, rect5) = Utils.SplitRect(rect3, 0.5f, 40f);
		DrawStatList(rect4, allStats, ref statScrollPosAll, isLeftList: true);
		DrawStatList(rect5, statsToOptimize, ref statScrollPosOptimize, isLeftList: false);
		Rect org2 = rect3;
		org2.xMin = rect4.xMax + 6f;
		org2.xMax = rect5.xMin - 6f;
		org2.yMin = rect3.center.y - 100f;
		org2.yMax = rect3.center.y + 100f;
		(Rect top, Rect bottom) tuple4 = Utils.SplitRectVertical(org2, 0.5f, 6f);
		Rect item = tuple4.top;
		Rect item2 = tuple4.bottom;
		GUI.color = Color.white;
		if (Widgets.ButtonText(item, ">") && selectedStatInLists != null && allStats.Contains(selectedStatInLists))
		{
			statsToOptimize.Add(selectedStatInLists);
			allStats.Remove(selectedStatInLists);
			selectedStatInLists = null;
		}
		if (Widgets.ButtonText(item2, "<") && selectedStatInLists != null && statsToOptimize.Contains(selectedStatInLists))
		{
			allStats.Add(selectedStatInLists);
			statsToOptimize.Remove(selectedStatInLists);
			selectedStatInLists = null;
		}
		(Rect left, Rect right) tuple5 = Utils.SplitRect(rect, 0.5f);
		Rect item3 = tuple5.left;
		Rect item4 = tuple5.right;
		item3.width = 100f;
		item4.xMin = item4.xMax - 100f;
		Button(item3, "Back", active: false);
		if (Button(item4, "Next", statsToOptimize.Any() && SelectedLayersToOptimize.Any()))
		{
			Solve();
		}
		GUI.color = Color.white;
		Rect rect6 = rect;
		rect6.xMax = item4.xMin - 150f;
		rect6.xMin = item3.xMax + 150f;
		Widgets.CheckboxLabeled(rect6, "Can take off apparel", ref CanTakeOff);
	}

	private bool SuitableFor(Pawn p, ThingDef app)
	{
		if (!ApparelUtility.HasPartsToWear(p, app))
		{
			return false;
		}
		return true;
	}

	private bool SuitableFor(Pawn p, Thing app)
	{
		if (p.apparel.WouldReplaceLockedApparel(app as Apparel))
		{
			return false;
		}
		if (!EquipmentUtility.CanEquip(app, p))
		{
			return false;
		}
		return true;
	}

	private bool CanBeWearead(List<Apparel> l, ThingDef app)
	{
		return !l.Any((Apparel x) => !ApparelUtility.CanWearTogether(x.def, app, pawn.RaceProps.body));
	}

	private void Solve()
	{
		FittedDict.Clear();
		Step++;
		List<Apparel> ImmutableApparel = pawn.apparel.WornApparel;
		if (CanTakeOff)
		{
			ImmutableApparel = ImmutableApparel.Where(delegate(Apparel x)
			{
				foreach (ApparelLayerDef layer in x.def.apparel.layers)
				{
					if (SelectedLayersToOptimize.Contains(layer))
					{
						return false;
					}
				}
				return true;
			}).ToList();
		}
		List<ThingDef> possibleApparel = (from x in ApparelSlotUtility.AllPotentialSlots.Where((ApparelSlotUtility.PotentialSlot x) => SelectedLayersToOptimize.Contains(x.layer) ? true : false).SelectMany((ApparelSlotUtility.PotentialSlot x) => x.possibleApparel)
			where CanBeWearead(ImmutableApparel, x) && SuitableFor(pawn, x)
			select x).ToList();
		List<Apparel> apparelList = FindAllApparel(possibleApparel, Deduplicate: false);
		FinalApparel = new List<Apparel>();
		FinalApparel.AddRange(ImmutableApparel);
		List<Apparel> list = new List<Apparel>();
		apparelList = SortApparelByStat(apparelList, statsToOptimize.First(), removeZeroAndDebuffs: true);
		foreach (Apparel item in apparelList)
		{
			if (CanBeWearead(list, item.def))
			{
				list.Add(item);
			}
		}
		FinalApparel.AddRange(list);
	}

	private List<Apparel> SortApparelByStat(List<Apparel> apparelList, StatDef sortByStat, bool removeZeroAndDebuffs, bool reverse = false)
	{
		if (apparelList == null || apparelList.Count <= 1 || sortByStat == null)
		{
			return apparelList;
		}
		List<Apparel> list = apparelList.Where((Apparel x) => !removeZeroAndDebuffs || CalcStat(x, sortByStat) > 0f || IsSkinLayer(x)).ToList();
		list.Sort(delegate(Apparel a, Apparel b)
		{
			float num = CalcStat(a, sortByStat);
			float value = CalcStat(b, sortByStat);
			int num2 = num.CompareTo(value);
			return (!reverse) ? (-num2) : num2;
		});
		return list;
	}

	private bool IsSkinLayer(Apparel x)
	{
		if (x.def.apparel.layers.Count == 1 && x.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin))
		{
			return true;
		}
		return false;
	}

	private float CalcStat(Apparel a, StatDef sortByStat)
	{
		StatRequest req = StatRequest.For(a);
		if (sortByStat.Worker.ShouldShowFor(req))
		{
			return a.GetStatValue(sortByStat);
		}
		return StatWorker.StatOffsetFromGear(a, sortByStat);
	}

	private List<Apparel> FindAllApparel(List<ThingDef> possibleApparel, bool Deduplicate)
	{
		IEnumerable<Thing> allThings = pawn.Map.listerThings.GetAllThings((Thing x) => x.Spawned && possibleApparel.Contains(x.def) && !x.IsForbidden(pawn) && !x.Fogged() && x is Apparel && SuitableFor(pawn, x));
		if (!Deduplicate)
		{
			return allThings.Cast<Apparel>().ToList();
		}
		Dictionary<ThingDef, Apparel> dictionary = new Dictionary<ThingDef, Apparel>();
		foreach (Apparel item in allThings.Cast<Apparel>())
		{
			if (dictionary.TryGetValue(item.def, out var value))
			{
				if (IsBetterApparel(item, value))
				{
					dictionary[item.def] = item;
				}
			}
			else
			{
				dictionary[item.def] = item;
			}
		}
		return dictionary.Values.ToList();
	}

	private static bool IsBetterApparel(Apparel a, Apparel b)
	{
		CompQuality comp;
		bool flag = a.TryGetComp<CompQuality>(out comp);
		CompQuality comp2;
		bool flag2 = b.TryGetComp<CompQuality>(out comp2);
		if (flag && flag2)
		{
			if (comp.Quality != comp2.Quality)
			{
				return (int)comp.Quality > (int)comp2.Quality;
			}
		}
		else if (flag != flag2)
		{
			return flag;
		}
		float num = (float)a.HitPoints / (float)a.MaxHitPoints;
		float num2 = (float)b.HitPoints / (float)b.MaxHitPoints;
		if (Mathf.Abs(num - num2) > 0.001f)
		{
			return num > num2;
		}
		return false;
	}

	private bool Button(Rect rect, string text, bool active)
	{
		GUI.color = (active ? Color.white : Color.gray);
		return Widgets.ButtonText(rect, text) && active;
	}

	private void DrawStatList(Rect rect, List<StatDef> stats, ref Vector2 scrollPos, bool isLeftList)
	{
		if (stats == null)
		{
			return;
		}
		float num = 28f;
		float height = (float)stats.Count * num;
		rect = rect.ExpandedBy(-4f);
		BlockDescr(rect.ContractedBy(-4f), isLeftList ? "Stats" : "Stat to maximize");
		GUI.color = Color.gray;
		Widgets.DrawBox(rect.ContractedBy(-4f), 2);
		Widgets.BeginScrollView(rect, ref scrollPos, new Rect(0f, 0f, rect.width - 16f, height));
		float num2 = 0f;
		for (int i = 0; i < stats.Count; i++)
		{
			Rect rect2 = new Rect(0f, num2, rect.width - 16f, num);
			if (selectedStatInLists == stats[i])
			{
				Widgets.DrawHighlightSelected(rect2);
			}
			else if (Mouse.IsOver(rect2))
			{
				Widgets.DrawHighlight(rect2);
			}
			if (Widgets.ButtonInvisible(rect2))
			{
				selectedStatInLists = stats[i];
			}
			DrawStat(rect2.ContractedBy(2f), stats[i]);
			num2 += num;
		}
		Widgets.EndScrollView();
	}

	private void DrawSlotsList(Rect rect)
	{
		float num = 32f;
		float height = (float)AllLayers.Count * num;
		rect = rect.ExpandedBy(-4f);
		BlockDescr(rect.ContractedBy(-4f), "Apparel layers");
		GUI.color = Color.gray;
		Widgets.DrawBox(rect.ContractedBy(-4f), 2);
		Widgets.BeginScrollView(rect, ref slotsScrollPos, new Rect(0f, 0f, rect.width - 16f, height));
		float num2 = 0f;
		foreach (ApparelLayerDef allLayer in AllLayers)
		{
			Rect rect2 = new Rect(0f, num2, rect.width - 16f, num);
			if (Mouse.IsOver(rect2))
			{
				Widgets.DrawLightHighlight(rect2);
			}
			DrawSlot(rect2.ContractedBy(2f), allLayer);
			num2 += num;
		}
		Widgets.EndScrollView();
	}

	private void BlockDescr(Rect rect, string v)
	{
		Rect rect2 = rect;
		rect2.yMin = rect.yMin - 30f;
		rect2.height = 30f;
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect2, v);
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private Thing GetApparelBySlot(ApparelLayerDef layer)
	{
		return pawn.apparel.WornApparel.FirstOrDefault((Apparel x) => x.def.IsApparel && x.def.apparel.layers.Contains(layer));
	}

	private void DrawApparel(Rect rect, Apparel apparel)
	{
		Widgets.DrawBoxSolid(rect, Assets.ColorBGD);
		var (rect2, rect3) = Utils.SplitRectByLeftPart(rect.ContractedBy(4f), rect.height * 1.2f, 6f);
		Widgets.DrawBoxSolid(rect2, Assets.ColorBG);
		ItemIconHelper.ThingIcon(Utils.RectCentered(rect2.center, rect2.height), apparel);
		if (!FittedDict.TryGetValue(apparel, out var value))
		{
			FittedDict.Add(apparel, Utils.TruncateToFit(apparel.LabelNoParenthesisCap.StripTags(), rect3.width));
		}
		GUI.color = Assets.ColorStat;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect3, value);
		Text.Anchor = TextAnchor.UpperLeft;
		if (Mouse.IsOver(rect))
		{
			TooltipHandler.TipRegion(rect, apparel.GetTooltip());
		}
	}

	private void DrawSlot(Rect rect, ApparelLayerDef layer)
	{
		if (SelectedLayersToOptimize.Contains(layer))
		{
			if (OccupiedSlots.Contains(layer) && !CanTakeOff)
			{
				GUI.color = Assets.ColorBGYellow;
				Assets.DrawTilingTexture(rect, Assets.DiagTiledTex, 64f, Vector2.zero);
			}
			else
			{
				Widgets.DrawBoxSolid(rect, Assets.ColorBGYellow);
			}
		}
		else
		{
			Widgets.DrawBoxSolid(rect, Assets.ColorBGD);
		}
		var (rect2, rect3) = Utils.SplitRectByLeftPart(rect.ContractedBy(4f), rect.height * 1.2f, 6f);
		Widgets.DrawBoxSolid(rect2, Assets.ColorBG);
		Rect rect4 = Utils.RectCentered(rect2.center, rect2.height);
		Thing apparelBySlot = GetApparelBySlot(layer);
		GUI.color = Color.gray;
		if (apparelBySlot != null)
		{
			ItemIconHelper.ThingIcon(rect4, apparelBySlot);
		}
		else
		{
			GUI.DrawTexture(rect4, (Texture)Assets.ApparelSlotTex);
		}
		GUI.color = Assets.ColorStat;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect3, layer.LabelCap);
		Text.Anchor = TextAnchor.UpperLeft;
		if (!Mouse.IsOver(rect))
		{
			return;
		}
		if (Widgets.ButtonInvisible(rect))
		{
			if (SelectedLayersToOptimize.Contains(layer))
			{
				SelectedLayersToOptimize.Remove(layer);
			}
			else
			{
				SelectedLayersToOptimize.Add(layer);
			}
		}
		TooltipHelper.DrawIconTooltip(rect, ApparelSlotUtility.AllPotentialSlots.Where((ApparelSlotUtility.PotentialSlot s) => s.layer == layer).SelectMany((ApparelSlotUtility.PotentialSlot x) => x.possibleApparel.Select((ThingDef a) => new TextureAndColor(Widgets.GetIconFor(a, GenStuff.DefaultStuffFor(a)), Color.white))).ToList(), "Apparel for layer:");
	}

	private void DrawStat(Rect rect, StatDef statDef)
	{
		Widgets.DrawBoxSolid(rect, Assets.ColorBGD);
		Text.Anchor = TextAnchor.MiddleLeft;
		GUI.color = Assets.ColorStat;
		Widgets.Label(rect.ContractedBy(4f), statDef.LabelCap);
		Text.Anchor = TextAnchor.UpperLeft;
	}
}
