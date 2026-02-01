using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace NCL;

public class HediffComp_InvisibilityWithEnemyAwareness : HediffComp_Invisibility
{
	private bool hasNearbyHostiles;

	private int lastHostileCheckTick = -9999;

	private PropertyInfo forcedVisibleProperty;

	private TotalWarfareSettings Settings => LoadedModManager.GetMod<TotalWarfareMod>()?.GetSettings<TotalWarfareSettings>() ?? new TotalWarfareSettings();

	public new HediffCompProperties_InvisibilityWithEnemyAwareness Props => (HediffCompProperties_InvisibilityWithEnemyAwareness)props;

	private bool BaseForcedVisible
	{
		get
		{
			if (forcedVisibleProperty == null)
			{
				forcedVisibleProperty = typeof(HediffComp_Invisibility).GetProperty("ForcedVisible", BindingFlags.Instance | BindingFlags.NonPublic);
				if (forcedVisibleProperty == null)
				{
					return false;
				}
			}
			return (bool)forcedVisibleProperty.GetValue(this);
		}
	}

	private bool ForcedVisible => hasNearbyHostiles || BaseForcedVisible;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (Find.TickManager.TicksGame > lastHostileCheckTick + Props.checkInterval)
		{
			lastHostileCheckTick = Find.TickManager.TicksGame;
			bool newHostileStatus = CheckNearbyHostiles();
			if (newHostileStatus && !hasNearbyHostiles)
			{
				BecomeVisible();
			}
			else if (!newHostileStatus && hasNearbyHostiles)
			{
				BecomeInvisible();
			}
			hasNearbyHostiles = newHostileStatus;
		}
	}

	private bool CheckNearbyHostiles()
	{
		if (!base.Pawn.Spawned || base.Pawn.Map == null || base.Pawn.Dead)
		{
			return false;
		}
		return GenRadial.RadialDistinctThingsAround(base.Pawn.Position, base.Pawn.Map, Props.detectionRadius, useCenter: true).Any((Thing thing) => IsValidHostile(thing as Pawn));
	}

	private bool IsValidHostile(Pawn otherPawn)
	{
		return otherPawn != null && !otherPawn.Dead && !otherPawn.Downed && otherPawn.HostileTo(base.Pawn) && GenSight.LineOfSightToThing(base.Pawn.Position, otherPawn, base.Pawn.Map);
	}

	public new virtual float GetAlpha()
	{
		TotalWarfareSettings settings = Settings;
		if (settings == null || settings.InvisibilityVisibleToPlayer)
		{
			return 1f;
		}
		return base.GetAlpha();
	}
}
