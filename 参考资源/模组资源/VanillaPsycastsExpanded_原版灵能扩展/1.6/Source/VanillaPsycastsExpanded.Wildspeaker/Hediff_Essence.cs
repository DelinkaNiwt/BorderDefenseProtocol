using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class Hediff_Essence : HediffWithComps
{
	public Pawn EssenceOf;

	public override string Label => base.Label + " " + EssenceOf.NameShortColored;

	public override bool ShouldRemove
	{
		get
		{
			if (EssenceOf != null)
			{
				if (EssenceOf.Dead)
				{
					return !(EssenceOf.Corpse?.Spawned ?? false);
				}
				return false;
			}
			return true;
		}
	}

	public override void Tick()
	{
		base.Tick();
		Pawn essenceOf = EssenceOf;
		if (essenceOf != null && essenceOf.Dead)
		{
			Corpse corpse = essenceOf.Corpse;
			if (corpse != null && corpse.Spawned && pawn.CurJobDef != VPE_DefOf.VPE_EssenceTransfer)
			{
				Job job = JobMaker.MakeJob(VPE_DefOf.VPE_EssenceTransfer, EssenceOf.Corpse);
				job.forceSleep = true;
				pawn.jobs.StartJob(job, JobCondition.InterruptForced);
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref EssenceOf, "essenceOf");
	}
}
