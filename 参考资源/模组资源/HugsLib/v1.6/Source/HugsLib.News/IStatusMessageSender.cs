namespace HugsLib.News;

internal interface IStatusMessageSender
{
	void Send(string message, bool success);
}
