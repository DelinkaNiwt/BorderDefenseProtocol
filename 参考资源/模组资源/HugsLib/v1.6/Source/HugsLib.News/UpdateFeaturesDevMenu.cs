using System;
using System.Collections.Generic;
using HugsLib.Utils;

namespace HugsLib.News;

/// <summary>
/// Provides the options for the dev tools dropdown menu in the extended update news dialog. 
/// </summary>
internal class UpdateFeaturesDevMenu
{
	private readonly IUpdateFeaturesDevActions news;

	private readonly IModSpotterDevActions spotter;

	private readonly IStatusMessageSender messages;

	public event Action<IEnumerable<UpdateFeatureDef>> UpdateFeatureDefsReloaded;

	public UpdateFeaturesDevMenu(IUpdateFeaturesDevActions news, IModSpotterDevActions spotter, IStatusMessageSender messages)
	{
		this.news = news;
		this.spotter = spotter;
		this.messages = messages;
	}

	public IEnumerable<(string label, Action action, bool disabled)> GetMenuOptions(UpdateFeatureDef forDef)
	{
		string modNameReadable = forDef.modNameReadable;
		return new(string, Action, bool)[6]
		{
			(GetNewsProviderStatusMessage(forDef), delegate
			{
			}, true),
			("Reload all news (F5)", ReloadNewsDefs, false),
			("Try show automatic news popup", TryShowAutomaticNewsPopupDialog, false),
			(modNameReadable + ": toggle first time user status", delegate
			{
				ToggleFirstTimeUserStatus(forDef);
			}, false),
			($"{modNameReadable}: set last seen news version to {forDef.Version}", delegate
			{
				SetLastSeenNewsVersion(forDef);
			}, false),
			(modNameReadable + ": reset last seen news version", delegate
			{
				ResetLastSeenNewsVersion(forDef);
			}, false)
		};
	}

	public void ReloadNewsDefs()
	{
		IEnumerable<UpdateFeatureDef> obj = news.ReloadAllUpdateFeatureDefs();
		this.UpdateFeatureDefsReloaded?.Invoke(obj);
	}

	private string GetNewsProviderStatusMessage(UpdateFeatureDef forDef)
	{
		return forDef.modNameReadable + " status:\nLast seen version: " + news.GetLastSeenNewsVersion(forDef.OwningModId).ToSemanticString("none") + ", first time user: " + (spotter.GetFirstTimeUserStatus(forDef.OwningPackageId) ? "Yes" : "No");
	}

	private void TryShowAutomaticNewsPopupDialog()
	{
		if (!news.TryShowAutomaticNewsPopupDialog())
		{
			messages.Send("Found no relevant unread update news to display. Automatic popup will not appear.", success: false);
		}
	}

	private void ToggleFirstTimeUserStatus(UpdateFeatureDef forDef)
	{
		spotter.ToggleFirstTimeUserStatus(forDef.OwningPackageId);
		string message = (spotter.GetFirstTimeUserStatus(forDef.OwningPackageId) ? ("Set player as first time user of " + forDef.modNameReadable + ".") : ("Set player as returning user of " + forDef.modNameReadable + "."));
		messages.Send(message, success: true);
	}

	private void SetLastSeenNewsVersion(UpdateFeatureDef forDef)
	{
		news.SetLastSeenNewsVersion(forDef.OwningModId, forDef.Version);
		messages.Send($"Last seen news version has been set to {forDef.Version} for {forDef.modNameReadable}.", success: true);
	}

	private void ResetLastSeenNewsVersion(UpdateFeatureDef forDef)
	{
		news.SetLastSeenNewsVersion(forDef.OwningModId, null);
		messages.Send("Last seen news version has been cleared for " + forDef.modNameReadable + ".", success: true);
	}
}
