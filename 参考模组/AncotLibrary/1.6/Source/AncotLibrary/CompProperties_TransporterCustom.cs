using RimWorld;

namespace AncotLibrary;

public class CompProperties_TransporterCustom : CompProperties_Transporter
{
	public string selectAllInGroupCommandTexPath;

	public string haulGizmoTexPath;

	public CompProperties_TransporterCustom()
	{
		compClass = typeof(CompTransporterCustom);
	}
}
