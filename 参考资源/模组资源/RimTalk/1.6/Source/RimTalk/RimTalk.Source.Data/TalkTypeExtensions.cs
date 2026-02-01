namespace RimTalk.Source.Data;

public static class TalkTypeExtensions
{
	public static bool IsFromUser(this TalkType talkType)
	{
		return talkType == TalkType.User;
	}
}
