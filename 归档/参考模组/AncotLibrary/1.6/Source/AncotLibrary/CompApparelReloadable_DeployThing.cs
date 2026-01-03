using RimWorld;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class CompApparelReloadable_DeployThing : CompApparelReloadable
{
	private int ai_DeployCooldown = 0;

	private int deployCooldownRemain = 0;

	public CompProperties_ApparelReloadable_DeployThing Props_Deploy => (CompProperties_ApparelReloadable_DeployThing)props;

	public ThingDef thingToDeploy => Props_Deploy.thingToDeploy;

	public Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				return null;
			}
			return wearer;
		}
	}

	public bool aiCanDeployNow => deployCooldownRemain == 0 && ai_DeployCooldown == 0;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ai_DeployCooldown, "ai_DeployCooldown", 0);
		Scribe_Values.Look(ref deployCooldownRemain, "deployCooldownRemain", 0);
	}

	public override void CompTick()
	{
		base.CompTick();
		if (ai_DeployCooldown > 0)
		{
			ai_DeployCooldown--;
		}
		if (deployCooldownRemain > 0)
		{
			deployCooldownRemain--;
		}
	}

	public override bool CanBeUsed(out string reason)
	{
		reason = "";
		if (deployCooldownRemain > 0)
		{
			reason = "CommandReload_Cooldown".Translate(base.Props.CooldownVerbArgument, deployCooldownRemain.ToStringTicksToPeriod().Named("TIME"));
			return false;
		}
		if (!base.CanBeUsed(out reason))
		{
			return false;
		}
		return true;
	}

	public void Deploy()
	{
		Map map = PawnOwner.Map;
		int num = GenRadial.NumCellsInRadius(4f);
		for (int i = 0; i < num; i++)
		{
			IntVec3 intVec = PawnOwner.Position + GenRadial.RadialPattern[i];
			if (intVec.IsValid && intVec.InBounds(map) && !intVec.Filled(map) && intVec.GetFirstBuilding(map) == null)
			{
				Thing thing = GenSpawn.Spawn(thingToDeploy, intVec, map);
				Props_Deploy.deploySound?.PlayOneShot(new TargetInfo(thing.Position, thing.Map));
				FleckMaker.Static(thing.TrueCenter(), thing.Map, Props_Deploy.deployFleck, 3f);
				if (thing is Building || thing is Pawn)
				{
					thing.SetFaction(PawnOwner.Faction);
				}
				ai_DeployCooldown = Props_Deploy.ai_DeployIntervalTick;
				deployCooldownRemain = Props_Deploy.deployCooldown;
				break;
			}
			Messages.Message("AbilityNotEnoughFreeSpace".Translate(), PawnOwner, MessageTypeDefOf.RejectInput, historical: false);
		}
	}
}
