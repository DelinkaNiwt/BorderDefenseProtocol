using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace NCL;

public class Comp_MechEmployable : ThingComp
{
	public int employmentTicks = -1;

	private Dictionary<Thing, float> enemyRecords = new Dictionary<Thing, float>();

	public CompProperties_MechEmployable Props => (CompProperties_MechEmployable)props;

	private Dictionary<Thing, float> SafeEnemyRecords
	{
		get
		{
			if (enemyRecords == null)
			{
				enemyRecords = new Dictionary<Thing, float>();
			}
			return enemyRecords;
		}
	}

	public void Employ(float silverAmount)
	{
		try
		{
			if (parent is Pawn pawn)
			{
				int num = (int)(silverAmount / Props.silverPerDay * 60000f);
				Log.Message("[NCL] 开始雇佣流程 | 当前阵营: " + (pawn.Faction?.Name ?? "null"));
				if (employmentTicks <= 0)
				{
					pawn.SetFactionDirect(Faction.OfPlayer);
					pawn.playerSettings = new Pawn_PlayerSettings(pawn);
					Log.Message("[NCL] 阵营已设置为: " + (pawn.Faction?.Name ?? "null"));
				}
				employmentTicks += num;
				pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
				pawn.mindState.Reset(clearInspiration: true, clearMentalState: true);
				Log.Message($"[NCL] 雇佣成功 | 剩余ticks: {employmentTicks}");
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[NCL] 雇佣错误: {arg}");
		}
	}

	public override void CompTick()
	{
		if (employmentTicks > 0 && --employmentTicks == 0 && parent is Pawn pawn)
		{
			pawn.SetFaction(null);
			Messages.Message("NCL.EMPLOYMENT_EXPIRED".Translate(pawn.LabelShortCap), MessageTypeDefOf.NeutralEvent);
		}
	}

	public void RecordDamage(DamageInfo dinfo)
	{
		Dictionary<Thing, float> safeEnemyRecords = SafeEnemyRecords;
		if (dinfo.Instigator != null)
		{
			if (safeEnemyRecords.ContainsKey(dinfo.Instigator))
			{
				safeEnemyRecords[dinfo.Instigator] += dinfo.Amount;
			}
			else
			{
				safeEnemyRecords.Add(dinfo.Instigator, dinfo.Amount);
			}
		}
	}

	public override string CompInspectStringExtra()
	{
		if (employmentTicks > 0)
		{
			return "NCL.EMPLOYMENT_REMAINING".Translate(employmentTicks.ToStringTicksToPeriod(), SafeEnemyRecords.Count);
		}
		return null;
	}

	public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		if (dinfo.Def.harmsHealth)
		{
			RecordDamage(dinfo);
		}
		base.PostPostApplyDamage(dinfo, totalDamageDealt);
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref employmentTicks, "employmentTicks", -1);
		List<Thing> keysWorkingList = new List<Thing>();
		List<float> valuesWorkingList = new List<float>();
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			keysWorkingList = enemyRecords.Keys.ToList();
			valuesWorkingList = enemyRecords.Values.ToList();
		}
		Scribe_Collections.Look(ref enemyRecords, "enemyRecords", LookMode.Reference, LookMode.Value, ref keysWorkingList, ref valuesWorkingList);
		if (Scribe.mode == LoadSaveMode.LoadingVars && enemyRecords == null)
		{
			enemyRecords = new Dictionary<Thing, float>();
		}
	}
}
