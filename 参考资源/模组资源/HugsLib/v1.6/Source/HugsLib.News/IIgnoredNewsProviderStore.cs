namespace HugsLib.News;

internal interface IIgnoredNewsProviderStore
{
	bool Contains(string providerId);
}
