using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;
using System.Linq;

namespace GD3
{
	public class CompProperties_SendCorpse : CompProperties
	{
		public CompProperties_SendCorpse()
		{
			this.compClass = typeof(CompSendCorpse);
		}
	}
	public class CompSendCorpse : ThingComp
	{
		public CompProperties_SendCorpse Props
		{
			get
			{
				return (CompProperties_SendCorpse)this.props;
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Pawn pawn = this.parent as Pawn;
			Quest quest = Find.QuestManager.QuestsListForReading.Find(q => q.root.defName == "GD_Quest_SendCorpse" && q.State == QuestState.Ongoing);
			if (quest != null)
			{
				Command_Action changeButton = new Command_Action
				{
					defaultLabel = "GD.SendCorpse".Translate(),
					defaultDesc = "GD.SendCorpseDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Buttons/ScytherCorpseLend", true),
					action = delegate ()
					{
						Effecter effect = EffecterDefOf.Skip_Entry.Spawn(parent, parent.MapHeld, 1f).Trigger(parent, parent);
						SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(parent.PositionHeld, parent.MapHeld, false));
						GDUtility.SendSignal(quest, "ReceivedCorpse");
						GDUtility.MissionComponent.BlackMechRelationOffset(-200);
						parent.Destroy();
					},
				};
				yield return changeButton;
			}
			yield break;
		}
	}
}
