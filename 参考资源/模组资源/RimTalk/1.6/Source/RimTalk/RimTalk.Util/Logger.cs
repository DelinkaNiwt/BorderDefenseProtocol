using Verse;

namespace RimTalk.Util;

public static class Logger
{
	private const string ModTag = "[RimTalk]";

	public static void Message(object message)
	{
		Log.Message(string.Format("{0} {1}\n\n", "[RimTalk]", message));
	}

	public static void Debug(object message)
	{
		if (Prefs.LogVerbose)
		{
			Log.Message(string.Format("{0} {1}\n\n", "[RimTalk]", message));
		}
	}

	public static void Warning(object message)
	{
		Log.Warning(string.Format("{0} {1}\n\n", "[RimTalk]", message));
	}

	public static void Error(object message)
	{
		Log.Error(string.Format("{0} {1}\n\n", "[RimTalk]", message));
	}

	public static void ErrorOnce(object text, int key)
	{
		Log.ErrorOnce(string.Format("{0} {1}\n\n", "[RimTalk]", text), key);
	}
}
