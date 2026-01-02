using Verse;

namespace AncotLibrary;

public class HediffComp_PhysicalShieldState : HediffComp
{
	public HediffCompProperties_PhysicalShieldState Props => (HediffCompProperties_PhysicalShieldState)props;

	public virtual void Notify_ShieldStateChange(Pawn pawn, An_ShieldState shieldState)
	{
		switch (shieldState)
		{
		case An_ShieldState.Active:
			Notify_ShieldActive(pawn);
			break;
		case An_ShieldState.Disabled:
			Notify_ShieldDisable(pawn);
			break;
		case An_ShieldState.Resetting:
			Notify_ShieldResetting(pawn);
			break;
		case An_ShieldState.Ready:
			Notify_ShieldReady(pawn);
			break;
		}
	}

	public virtual void Notify_ShieldActive(Pawn pawn)
	{
	}

	public virtual void Notify_ShieldReady(Pawn pawn)
	{
	}

	public virtual void Notify_ShieldDisable(Pawn pawn)
	{
	}

	public virtual void Notify_ShieldResetting(Pawn pawn)
	{
	}
}
