using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

public class HurricaneMaker : Thing
{
	private GameCondition caused;

	public Pawn Pawn;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			caused = GameConditionMaker.MakeConditionPermanent(VPE_DefOf.VPE_Hurricane_Condition);
			caused.conditionCauser = this;
			map.GameConditionManager.RegisterCondition(caused);
			base.Map.weatherManager.TransitionTo(VPE_DefOf.VPE_Hurricane_Weather);
			base.Map.weatherDecider.StartNextWeather();
		}
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		caused.End();
		base.Map.weatherDecider.StartNextWeather();
		base.Destroy(mode);
	}

	protected override void Tick()
	{
		if (!Pawn.psychicEntropy.TryAddEntropy(1f, this) || Pawn.Downed)
		{
			Destroy();
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref caused, "caused");
		Scribe_References.Look(ref Pawn, "pawn");
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			caused.conditionCauser = this;
		}
	}
}
