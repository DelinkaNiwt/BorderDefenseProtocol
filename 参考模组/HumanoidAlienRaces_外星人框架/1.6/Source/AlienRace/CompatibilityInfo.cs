using Verse;

namespace AlienRace;

public class CompatibilityInfo
{
	protected bool isFlesh = true;

	protected bool isSentient = true;

	protected bool hasBlood = true;

	public virtual bool IsFlesh
	{
		get
		{
			return isFlesh;
		}
		set
		{
			isFlesh = value;
		}
	}

	public virtual bool IsSentient
	{
		get
		{
			return isSentient;
		}
		set
		{
			isSentient = value;
		}
	}

	public virtual bool HasBlood
	{
		get
		{
			return hasBlood;
		}
		set
		{
			hasBlood = value;
		}
	}

	public virtual bool IsFleshPawn(Pawn pawn)
	{
		return IsFlesh;
	}

	public virtual bool IsSentientPawn(Pawn pawn)
	{
		return IsSentient;
	}

	public virtual bool HasBloodPawn(Pawn pawn)
	{
		return HasBlood;
	}
}
