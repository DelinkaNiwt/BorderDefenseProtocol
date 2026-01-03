using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class GameComponent_MechWorkInitialize : GameComponent
{
	public GameComponent_MechWorkInitialize(Game game)
	{
	}

	public override void StartedNewGame()
	{
		base.StartedNewGame();
		MechWorkTableInitialize();
	}

	public override void LoadedGame()
	{
		base.LoadedGame();
		MechWorkTableInitialize();
	}

	public static IEnumerable<PawnColumnDef> ImpliedPawnColumnDefs(bool hotReload = false)
	{
		foreach (PawnTableDef tableDef in DefDatabase<PawnTableDef>.AllDefs.Where((PawnTableDef def) => def.HasModExtension<ModExtension_WorkTypeColumn>()))
		{
			bool moveWorkTypeLabelDown = false;
			foreach (WorkTypeDef workType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.Where((WorkTypeDef d) => d.visible).Reverse())
			{
				moveWorkTypeLabelDown = !moveWorkTypeLabelDown;
				string defName2 = "Ancot_MechWorkPriority_" + workType.defName;
				PawnColumnDef pawnColumnDef2 = (hotReload ? (DefDatabase<PawnColumnDef>.GetNamed(defName2, errorOnFail: false) ?? new PawnColumnDef()) : new PawnColumnDef());
				pawnColumnDef2.defName = defName2;
				pawnColumnDef2.workType = workType;
				pawnColumnDef2.moveWorkTypeLabelDown = moveWorkTypeLabelDown;
				pawnColumnDef2.workerClass = typeof(PawnColumnWorker_MechWorkPriority);
				pawnColumnDef2.sortable = true;
				pawnColumnDef2.modContentPack = workType.modContentPack;
				int insertIndex = tableDef.columns.FindIndex((PawnColumnDef x) => x.Worker is PawnColumnWorker_CopyPasteWorkPriorities) + 1;
				if (insertIndex <= 0)
				{
					insertIndex = tableDef.columns.Count;
				}
				tableDef.columns.Insert(insertIndex, pawnColumnDef2);
				yield return pawnColumnDef2;
			}
		}
	}

	public void MechWorkTableInitialize()
	{
		PawnTableDef ancot_MechWork = AncotDefOf.Ancot_MechWork;
		if (ancot_MechWork.columns.Any((PawnColumnDef x) => x.workerClass == typeof(PawnColumnWorker_MechWorkPriority)))
		{
			return;
		}
		foreach (PawnColumnDef item in ImpliedPawnColumnDefs())
		{
			if (DefDatabase<PawnColumnDef>.GetNamedSilentFail(item.defName) == null)
			{
				DefGenerator.AddImpliedDef(item);
			}
		}
	}
}
