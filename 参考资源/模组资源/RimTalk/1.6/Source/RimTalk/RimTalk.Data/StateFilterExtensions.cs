using UnityEngine;
using Verse;

namespace RimTalk.Data;

public static class StateFilterExtensions
{
	public static string GetLabel(this ApiLog.State state)
	{
		if (1 == 0)
		{
		}
		string result = state switch
		{
			ApiLog.State.None => "RimTalk.DebugWindow.StateAll".Translate(), 
			ApiLog.State.Pending => "RimTalk.DebugWindow.StatePending".Translate(), 
			ApiLog.State.Ignored => "RimTalk.DebugWindow.StateIgnored".Translate(), 
			ApiLog.State.Spoken => "RimTalk.DebugWindow.StateSpoken".Translate(), 
			ApiLog.State.Failed => "RimTalk.DebugWindow.StateFailed".Translate(), 
			_ => "Unknown", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static Color GetColor(this ApiLog.State state)
	{
		if (1 == 0)
		{
		}
		Color result = state switch
		{
			ApiLog.State.Failed => new Color(1f, 0.5f, 0.5f), 
			ApiLog.State.Pending => Color.yellow, 
			ApiLog.State.Ignored => Color.gray, 
			ApiLog.State.Spoken => Color.green, 
			_ => Color.white, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
