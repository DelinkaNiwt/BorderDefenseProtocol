using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class CompSwitchResonate : ThingComp
{
	public HediffDef resonateClass;

	private float resonateNum = 0f;

	private CompProperties_SwitchResonate Props => (CompProperties_SwitchResonate)props;

	private Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}

	public bool IsApparel => parent is Apparel;

	private bool IsBuiltIn => !IsApparel;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Defs.Look(ref resonateClass, "resonateClass");
		Scribe_Values.Look(ref resonateNum, "resonateNum", 0f);
	}

	public bool IsPawnAffected(Pawn target, Pawn owner)
	{
		if (!MilianUtility.IsMilian(target))
		{
			return false;
		}
		if (target.Dead || target.health == null)
		{
			return false;
		}
		if (target.Faction != owner.Faction)
		{
			return false;
		}
		return true;
	}

	public override void CompTick()
	{
		if (PawnOwner == null || !PawnOwner.Spawned || !PawnOwner.IsHashIntervalTick(Props.checkInterval))
		{
			return;
		}
		foreach (Pawn item in PawnOwner.Map.mapPawns.AllPawnsSpawned)
		{
			if (IsPawnAffected(item, PawnOwner) && resonateClass != null)
			{
				Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(resonateClass);
				if (firstHediffOfDef != null && firstHediffOfDef.Severity < 0.25f * (float)Props.resonateLevel)
				{
					firstHediffOfDef.Severity = 0.25f * (float)Props.resonateLevel;
				}
			}
		}
	}

	public void SetClassHediffToInitial(Map map, Pawn pawn)
	{
		foreach (Pawn item in map.mapPawns.PawnsInFaction(pawn.Faction))
		{
			if (IsPawnAffected(item, pawn))
			{
				Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Pawn);
				if (firstHediffOfDef != null)
				{
					firstHediffOfDef.Severity = 0.01f;
				}
				Hediff firstHediffOfDef2 = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Knight);
				if (firstHediffOfDef2 != null)
				{
					firstHediffOfDef2.Severity = 0.01f;
				}
				Hediff firstHediffOfDef3 = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Bishop);
				if (firstHediffOfDef3 != null)
				{
					firstHediffOfDef3.Severity = 0.01f;
				}
				Hediff firstHediffOfDef4 = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Rook);
				if (firstHediffOfDef4 != null)
				{
					firstHediffOfDef4.Severity = 0.01f;
				}
			}
		}
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		SetClassHediffToInitial(pawn.Map, pawn);
		base.Notify_Unequipped(pawn);
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		if (!IsApparel)
		{
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (!IsBuiltIn)
		{
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	public IEnumerable<Gizmo> GetGizmos()
	{
		if (PawnOwner == null || !PawnOwner.Faction.IsPlayer || (Props.displayGizmoWhileUndrafted && !PawnOwner.Drafted))
		{
			yield break;
		}
		Command_Action command_Action = new Command_Action
		{
			defaultLabel = "Milira.Resonate".Translate() + ": " + "Milira.None".Translate(),
			defaultDesc = "Milira.ResonateDesc".Translate(),
			icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath_King)
		};
		if (resonateNum == 1f)
		{
			command_Action.defaultLabel = "Milira.Resonate".Translate() + ": " + "Milira.Pawn".Translate();
			command_Action.icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath_Pawn);
		}
		else if (resonateNum == 2f)
		{
			command_Action.defaultLabel = "Milira.Resonate".Translate() + ": " + "Milira.Knight".Translate();
			command_Action.icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath_Knight);
		}
		else if (resonateNum == 3f)
		{
			command_Action.defaultLabel = "Milira.Resonate".Translate() + ": " + "Milira.Bishop".Translate();
			command_Action.icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath_Bishop);
		}
		else if (resonateNum == 4f)
		{
			command_Action.defaultLabel = "Milira.Resonate".Translate() + ": " + "Milira.Rook".Translate();
			command_Action.icon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath_Rook);
		}
		command_Action.action = delegate
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			FloatMenuOption item = new FloatMenuOption("Milira.Pawn".Translate(), delegate
			{
				CompSwitchResonate compSwitchResonate = parent.TryGetComp<CompSwitchResonate>();
				if (compSwitchResonate != null)
				{
					resonateNum = 1f;
					compSwitchResonate.resonateClass = MiliraDefOf.Milian_ClassHediff_Pawn;
					compSwitchResonate.SetClassHediffToInitial(PawnOwner.Map, PawnOwner);
				}
			}, MenuOptionPriority.Default, null, null, 29f, (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, MiliraDefOf.Milian_ClassHediff_Pawn));
			list.Add(item);
			FloatMenuOption item2 = new FloatMenuOption("Milira.Knight".Translate(), delegate
			{
				CompSwitchResonate compSwitchResonate = parent.TryGetComp<CompSwitchResonate>();
				if (compSwitchResonate != null)
				{
					resonateNum = 2f;
					compSwitchResonate.resonateClass = MiliraDefOf.Milian_ClassHediff_Knight;
					compSwitchResonate.SetClassHediffToInitial(PawnOwner.Map, PawnOwner);
				}
			}, MenuOptionPriority.Default, null, null, 29f, (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, MiliraDefOf.Milian_ClassHediff_Knight));
			list.Add(item2);
			FloatMenuOption item3 = new FloatMenuOption("Milira.Bishop".Translate(), delegate
			{
				CompSwitchResonate compSwitchResonate = parent.TryGetComp<CompSwitchResonate>();
				if (compSwitchResonate != null)
				{
					resonateNum = 3f;
					compSwitchResonate.resonateClass = MiliraDefOf.Milian_ClassHediff_Bishop;
					compSwitchResonate.SetClassHediffToInitial(PawnOwner.Map, PawnOwner);
				}
			}, MenuOptionPriority.Default, null, null, 29f, (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, MiliraDefOf.Milian_ClassHediff_Bishop));
			list.Add(item3);
			FloatMenuOption item4 = new FloatMenuOption("Milira.Rook".Translate(), delegate
			{
				CompSwitchResonate compSwitchResonate = parent.TryGetComp<CompSwitchResonate>();
				if (compSwitchResonate != null)
				{
					resonateNum = 4f;
					compSwitchResonate.resonateClass = MiliraDefOf.Milian_ClassHediff_Rook;
					compSwitchResonate.SetClassHediffToInitial(PawnOwner.Map, PawnOwner);
				}
			}, MenuOptionPriority.Default, null, null, 29f, (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, MiliraDefOf.Milian_ClassHediff_Rook));
			list.Add(item4);
			Find.WindowStack.Add(new FloatMenu(list));
		};
		yield return command_Action;
	}
}
