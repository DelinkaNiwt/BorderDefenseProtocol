using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Milira;

public class PawnColumnWorker_EquipmentMilian : PawnColumnWorker_Equipment
{
	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		if (Widgets.ButtonInvisible(rect))
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			list.AddRange(GetEquipmentFloatMenu(pawn));
			Find.WindowStack.Add(new FloatMenu(list));
		}
		if (pawn.CurJobDef == MiliraDefOf.Milira_EquipMilian)
		{
			Vector2 iconSize = ((PawnColumnWorker_Icon)(object)this).GetIconSize(pawn);
			int num = (int)((rect.width - iconSize.x) / 2f);
			int num2 = Mathf.Max((int)((30f - iconSize.y) / 2f), 0);
			Rect rect2 = new Rect(rect.x + (float)num, rect.y + (float)num2, iconSize.x, iconSize.y);
			GUI.DrawTexture(rect2, (Texture)AncotLibraryIcon.SwitchA);
		}
		((PawnColumnWorker_Equipment)this).DoCell(rect, pawn, table);
	}

	public List<FloatMenuOption> GetEquipmentFloatMenu(Pawn pawn)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		List<Thing> list2 = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon);
		CompTargetableWeapon compTargetableWeapon = pawn.TryGetComp<CompTargetableWeapon>();
		if (pawn.CurJobDef == MiliraDefOf.Milira_EquipMilian)
		{
			LocalTargetInfo targetB = pawn.CurJob.targetB;
			FloatMenuOption item = new FloatMenuOption("Milira.CancelEquipWith".Translate() + targetB.Label, delegate
			{
				pawn.jobs.StopAll();
			});
			list.Add(item);
		}
		if (!list2.NullOrEmpty())
		{
			foreach (Thing thing in list2)
			{
				if (thing.stackCount > 1)
				{
					continue;
				}
				string label = "Milira.EquipWith".Translate() + thing.Label;
				if (compTargetableWeapon.Available(thing, pawn))
				{
					FloatMenuOption item2 = new FloatMenuOption(label, delegate
					{
						Job job = JobMaker.MakeJob(MiliraDefOf.Milira_EquipMilian, pawn, thing);
						job.count = 1;
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					});
					list.Add(item2);
				}
			}
		}
		if (list.Empty())
		{
			FloatMenuOption floatMenuOption = new FloatMenuOption("Milira.NoAvailableEquipment".Translate(), null);
			floatMenuOption.Disabled = true;
			list.Add(floatMenuOption);
		}
		return list;
	}
}
