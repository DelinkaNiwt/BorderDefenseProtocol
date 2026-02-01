using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

public class Ability_Hurricane : Ability, IAbilityToggle, IChannelledPsycast, ILoadReferenceable
{
	private HurricaneMaker maker;

	public bool Toggle
	{
		get
		{
			return maker?.Spawned ?? false;
		}
		set
		{
			if (value)
			{
				((Ability)this).DoAction();
			}
			else
			{
				maker?.Destroy();
			}
		}
	}

	public string OffLabel => "VPE.StopHurricane".Translate();

	public bool IsActive => maker?.Spawned ?? false;

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		maker = (HurricaneMaker)ThingMaker.MakeThing(VPE_DefOf.VPE_HurricaneMaker);
		maker.Pawn = base.pawn;
		GenSpawn.Spawn(maker, base.pawn.Position, base.pawn.Map);
	}

	public override void ExposeData()
	{
		((Ability)this).ExposeData();
		Scribe_References.Look(ref maker, "maker");
	}

	public override Gizmo GetGizmo()
	{
		return (Gizmo)(object)new Command_AbilityToggle(base.pawn, (Ability)(object)this);
	}
}
