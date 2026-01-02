using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class HediffComp_SetWorkPriority : HediffComp
{
	public List<DE_CheckWorkPriority> oldWorkPriorities = new List<DE_CheckWorkPriority>();

	private HediffCompProperties_SetWorkPriority Props => (HediffCompProperties_SetWorkPriority)props;

	public void SetWorkPriority(Pawn p, DE_WorkPriority workPriority)
	{
		if (p == null || workPriority == null)
		{
			return;
		}
		int priority = workPriority.Priority;
		if (priority < 0 || priority > 4)
		{
			return;
		}
		WorkTypeDef workType = workPriority.workType;
		if (!p.WorkTypeIsDisabled(workType))
		{
			int priority2 = p.workSettings.GetPriority(workType);
			if ((priority == 0 || priority < priority2 || !workPriority.IsHigherPriority) && (priority == 0 || priority > priority2 || !workPriority.IsLowerPrioity))
			{
				DE_CheckWorkPriority item = new DE_CheckWorkPriority
				{
					newPriority = priority,
					oldPriority = priority2,
					workType = workType
				};
				oldWorkPriorities.Add(item);
				p.workSettings.SetPriority(workType, workPriority.Priority);
			}
		}
	}

	public void SetWorkPriorities(Pawn p, List<DE_WorkPriority> workPriorities)
	{
		if (p == null || workPriorities.NullOrEmpty())
		{
			return;
		}
		foreach (DE_WorkPriority workPriority in workPriorities)
		{
			SetWorkPriority(p, workPriority);
		}
	}

	public void CheckWorkPriorities(Pawn p, List<DE_CheckWorkPriority> checkWorkPriorities)
	{
		if (p == null || checkWorkPriorities.NullOrEmpty())
		{
			return;
		}
		foreach (DE_CheckWorkPriority checkWorkPriority in checkWorkPriorities)
		{
			CheckWorkPriority(p, checkWorkPriority);
			oldWorkPriorities = new List<DE_CheckWorkPriority>();
		}
	}

	public void CheckWorkPriority(Pawn p, DE_CheckWorkPriority checkWorkPriority)
	{
		WorkTypeDef workType = checkWorkPriority.workType;
		int oldPriority = checkWorkPriority.oldPriority;
		int newPriority = checkWorkPriority.newPriority;
		DE_WorkPriority workPriority = new DE_WorkPriority
		{
			Priority = oldPriority,
			workType = workType
		};
		DE_WorkPriority workPriority2 = new DE_WorkPriority
		{
			Priority = newPriority,
			workType = workType
		};
		if (CheckWork(p, workPriority2))
		{
			SetWorkPriority(p, workPriority);
		}
	}

	public bool CheckWork(Pawn p, DE_WorkPriority workPriority)
	{
		if (p == null || workPriority == null)
		{
			return false;
		}
		int priority = workPriority.Priority;
		if (priority < 0 || priority > 4)
		{
			return false;
		}
		WorkTypeDef workType = workPriority.workType;
		if (p.WorkTypeIsDisabled(workType))
		{
			return false;
		}
		int priority2 = p.workSettings.GetPriority(workType);
		if (priority2 == priority)
		{
			return true;
		}
		return false;
	}

	public override void CompPostPostAdd(DamageInfo? dinfo)
	{
		base.CompPostPostAdd(dinfo);
		List<DE_WorkPriority> workPriorities = Props.workPriorities;
		DE_WorkPriority workPriority = Props.workPriority;
		if (!workPriorities.NullOrEmpty())
		{
			SetWorkPriorities(base.Pawn, workPriorities);
		}
		if (workPriority != null)
		{
			SetWorkPriority(base.Pawn, workPriority);
		}
	}

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		if (!oldWorkPriorities.NullOrEmpty())
		{
			CheckWorkPriorities(base.Pawn, oldWorkPriorities);
		}
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Collections.Look(ref oldWorkPriorities, "oldWorkPriorities", LookMode.Reference);
	}
}
