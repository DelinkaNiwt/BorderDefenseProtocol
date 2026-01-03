namespace Milira;

public class MainButtonWorker_MilianWork : MainButtonWorker_MilianConfig
{
	public override bool Disabled
	{
		get
		{
			if (base.Disabled)
			{
				return true;
			}
			return !MiliraDefOf.Milira_MilianTech_WorkManagement.IsFinished;
		}
	}

	public override bool Visible => !Disabled && (int)MiliraRaceSettings.TabAvailable_MilianWork > 0;
}
