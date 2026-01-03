using Verse;

namespace AncotLibrary;

public class HediffComp_PhysicalShieldState_Severity : HediffComp_PhysicalShieldState
{
	public new HediffCompProperties_PhysicalShieldState_Severity Props => (HediffCompProperties_PhysicalShieldState_Severity)props;

	public override void Notify_ShieldActive(Pawn pawn)
	{
		parent.Severity = Props.severityActive;
	}

	public override void Notify_ShieldReady(Pawn pawn)
	{
		parent.Severity = Props.severityReady;
	}

	public override void Notify_ShieldDisable(Pawn pawn)
	{
		parent.Severity = Props.severityDisable;
	}

	public override void Notify_ShieldResetting(Pawn pawn)
	{
		parent.Severity = Props.severityResetting;
	}
}
