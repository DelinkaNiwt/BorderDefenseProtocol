using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimTalk.Util;
using RimWorld.Planet;
using Verse;

namespace RimTalk.Data;

public class RimTalkWorldComponent : WorldComponent
{
	private const int MaxLogEntries = 1000;

	public Dictionary<string, string> RimTalkInteractionTexts = new Dictionary<string, string>();

	private Queue<string> _keyInsertionOrder = new Queue<string>();

	public RimTalkWorldComponent(World world)
		: base(world)
	{
	}

	public override void ExposeData()
	{
		base.ExposeData();
		try
		{
			Scribe_Collections.Look(ref RimTalkInteractionTexts, "rimtalkInteractionTexts", LookMode.Value, LookMode.Value);
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to save/load interaction texts. Resetting data to prevent save corruption. Error: " + ex.Message);
			RimTalkInteractionTexts = new Dictionary<string, string>();
			_keyInsertionOrder = new Queue<string>();
		}
		List<string> keyOrderList = null;
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			keyOrderList = _keyInsertionOrder.ToList();
		}
		Scribe_Collections.Look(ref keyOrderList, "rimtalkKeyOrder", LookMode.Undefined);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (RimTalkInteractionTexts == null)
			{
				RimTalkInteractionTexts = new Dictionary<string, string>();
			}
			_keyInsertionOrder = ((keyOrderList != null) ? new Queue<string>(keyOrderList) : new Queue<string>());
		}
	}

	public void SetTextFor(LogEntry entry, string text)
	{
		if (entry == null || text == null)
		{
			return;
		}
		string cleanText = SanitizeXmlString(text);
		string key = entry.GetUniqueLoadID();
		if (RimTalkInteractionTexts.ContainsKey(key))
		{
			RimTalkInteractionTexts[key] = cleanText;
			return;
		}
		while (_keyInsertionOrder.Count >= 1000)
		{
			string oldestKey = _keyInsertionOrder.Dequeue();
			RimTalkInteractionTexts.Remove(oldestKey);
		}
		_keyInsertionOrder.Enqueue(key);
		RimTalkInteractionTexts[key] = cleanText;
	}

	public bool TryGetTextFor(LogEntry entry, out string text)
	{
		text = null;
		return entry != null && RimTalkInteractionTexts.TryGetValue(entry.GetUniqueLoadID(), out text);
	}

	private static string SanitizeXmlString(string invalidXml)
	{
		if (string.IsNullOrEmpty(invalidXml))
		{
			return invalidXml;
		}
		StringBuilder stringBuilder = new StringBuilder(invalidXml.Length);
		foreach (char c in invalidXml)
		{
			if (c == '\t' || c == '\n' || c == '\r' || (c >= ' ' && c <= '\ud7ff') || (c >= '\ue000' && c <= '\ufffd'))
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}
}
