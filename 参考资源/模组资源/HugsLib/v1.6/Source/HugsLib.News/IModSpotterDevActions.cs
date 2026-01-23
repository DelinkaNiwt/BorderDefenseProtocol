namespace HugsLib.News;

internal interface IModSpotterDevActions
{
	bool GetFirstTimeUserStatus(string packageId);

	void ToggleFirstTimeUserStatus(string packageId);
}
