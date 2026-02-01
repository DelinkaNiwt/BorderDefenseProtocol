using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class RecordTool
{
	internal const string CO_RECORDS = "records";

	internal static Vector2 scrollPos;

	internal static int elemH;

	internal static int oldIVal;

	internal static float oldFVal;

	internal static RecordDef selectedRecord;

	internal static string GetAsSeparatedString(this RecordDef r, float val)
	{
		if (r == null)
		{
			return "";
		}
		string text = "";
		text = text + r.defName + "|";
		return text + val;
	}

	internal static string GetAllRecordsAsSeparatedString(this Pawn p)
	{
		if (!p.HasRecordsTracker() || p.GetPawnRecords().EnumerableNullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (KeyValuePair<RecordDef, float> pawnRecord in p.GetPawnRecords())
		{
			text += pawnRecord.Key.GetAsSeparatedString(pawnRecord.Value);
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetRecords(this Pawn p, string s)
	{
		if (s.NullOrEmpty() || !p.HasRecordsTracker())
		{
			return;
		}
		try
		{
			string[] array = s.SplitNo(":");
			DefMap<RecordDef, float> pawnRecords = p.GetPawnRecords();
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				string[] array3 = s2.SplitNo("|");
				if (array3.Length == 2)
				{
					RecordDef recordDef = DefTool.RecordDef(array3[0]);
					if (recordDef != null)
					{
						float value = array3[1].AsFloat();
						pawnRecords[recordDef] = value;
					}
				}
			}
			p.SetPawnRecords(pawnRecords);
			p.records.RecordsTickInterval(0);
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetPawnRecords(this Pawn p, DefMap<RecordDef, float> dic)
	{
		if (p.HasRecordsTracker())
		{
			p.records.SetMemberValue("records", dic);
		}
	}

	internal static DefMap<RecordDef, float> GetPawnRecords(this Pawn p)
	{
		return p.HasRecordsTracker() ? p.records.GetMemberValue<DefMap<RecordDef, float>>("records", null) : null;
	}

	internal static void DrawRecordCard(Rect rect, Pawn p)
	{
		Text.Font = GameFont.Small;
		List<RecordDef> allDefsListForReading = DefDatabase<RecordDef>.AllDefsListForReading;
		List<RecordDef> list = allDefsListForReading.Where((RecordDef td) => td.type == RecordType.Time).ToList();
		List<RecordDef> list2 = allDefsListForReading.Where((RecordDef td) => td.type == RecordType.Int).ToList();
		List<RecordDef> list3 = allDefsListForReading.Where((RecordDef td) => td.type == RecordType.Float).ToList();
		int count = list.Count;
		int count2 = list2.Count;
		int count3 = list3.Count;
		int num = Mathf.Max(count, count2 + count3);
		elemH = 21;
		float height = (float)(num * elemH) + 50f;
		Rect outRect = new Rect(rect);
		Rect rect2 = new Rect(0f, 0f, outRect.width - 16f, height);
		Widgets.BeginScrollView(outRect, ref scrollPos, rect2);
		Rect rect3 = rect2.ContractedBy(4f);
		rect3.height = height;
		Rect rect4 = rect2;
		rect4.width *= 0.5f;
		Rect rect5 = rect2;
		rect5.x = rect4.xMax;
		rect5.width = rect2.width - rect5.x;
		rect4.xMax -= 6f;
		rect5.xMin += 6f;
		DrawLeftRect(rect4, list, p);
		DrawRightRect(rect5, list2, list3, p);
		Widgets.EndScrollView();
	}

	internal static void DrawLeftRect(Rect rect, List<RecordDef> l, Pawn p)
	{
		float curY = 0f;
		Widgets.BeginGroup(rect);
		Widgets.ListSeparator(ref curY, rect.width, "TimeRecordsCategory".Translate());
		foreach (RecordDef item in l)
		{
			curY += DrawRecord(8f, curY, rect.width - 8f, item, p);
		}
		Widgets.EndGroup();
	}

	internal static void DrawRightRect(Rect rect, List<RecordDef> li, List<RecordDef> lf, Pawn p)
	{
		float curY = 0f;
		Widgets.BeginGroup(rect);
		Widgets.ListSeparator(ref curY, rect.width, "MiscRecordsCategory".Translate());
		foreach (RecordDef item in li)
		{
			curY += DrawRecord(8f, curY, rect.width - 8f, item, p);
		}
		foreach (RecordDef item2 in lf)
		{
			curY += DrawRecord(8f, curY, rect.width - 8f, item2, p);
		}
		Widgets.EndGroup();
	}

	internal static float DrawRecord(float x, float y, float w, RecordDef r, Pawn p)
	{
		float num = w * 0.4f;
		string label = ((r.type != RecordType.Time) ? p.records.GetValue(r).ToString("0.##") : p.records.GetAsInt(r).ToStringTicksToPeriod());
		Rect rect = new Rect(8f, y, w, elemH);
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		Rect rect2 = rect;
		rect2.width -= num;
		Widgets.Label(rect2, r.LabelCap);
		SZWidgets.ButtonInvisible(rect2, delegate
		{
			selectedRecord = null;
		});
		Rect rect3 = rect;
		rect3.x = rect2.xMax;
		rect3.width = num;
		if (selectedRecord == r)
		{
			if (r.type == RecordType.Int)
			{
				oldIVal = p.records.GetAsInt(r);
				int num2 = SZWidgets.NumericIntBox(rect3.x, rect3.y, 90f, rect3.height, oldIVal, 0, int.MaxValue);
				if (num2 != oldIVal)
				{
					p.SetRecordValue(r, num2);
				}
			}
			else if (r.type == RecordType.Float)
			{
				oldFVal = p.records.GetValue(r);
				float num3 = SZWidgets.NumericFloatBox(rect3.x, rect3.y, 90f, rect3.height, oldFVal, 0f, 1E+09f);
				if (num3 != oldFVal)
				{
					p.SetRecordValue(r, num3);
				}
			}
			else if (r.type == RecordType.Time)
			{
				oldFVal = p.records.GetValue(r);
				long num4 = (long)(oldFVal / 60f);
				long num5 = SZWidgets.NumericLongBox(rect3.x, rect3.y, 90f, rect3.height, num4, 0L, long.MaxValue);
				if (num5 != num4)
				{
					p.SetRecordValue(r, num5 * 60);
				}
			}
		}
		else
		{
			Widgets.Label(rect3, label);
			SZWidgets.ButtonInvisibleMouseOverVar(rect, delegate(RecordDef record)
			{
				selectedRecord = record;
			}, r, r.description);
		}
		return rect.height;
	}

	internal static void SetRecordValue(this Pawn p, RecordDef r, float val)
	{
		DefMap<RecordDef, float> memberValue = p.records.GetMemberValue<DefMap<RecordDef, float>>("records", null);
		if (memberValue != null)
		{
			memberValue[r] = val;
		}
	}
}
