using System;
using RimTalk.Prompt;
using Verse;

namespace RimTalk;

public static class PresetPreviewGenerator
{
	public static string GeneratePreview(string template)
	{
		if (string.IsNullOrWhiteSpace(template))
		{
			return "";
		}
		if (Current.ProgramState != ProgramState.Playing)
		{
			return "RimTalk.Settings.PromptPreset.PreviewNotAvailable".Translate();
		}
		PromptContext ctx = PromptManager.LastContext;
		if (ctx == null)
		{
			return "RimTalk.Settings.PromptPreset.NoRecentInteraction".Translate();
		}
		try
		{
			ctx.IsPreview = true;
			return ScribanParser.Render(template, ctx, logErrors: false);
		}
		catch (Exception ex)
		{
			return "RimTalk.Settings.PromptPreset.PreviewError".Translate(ex.Message);
		}
	}
}
