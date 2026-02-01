using System;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using UnityEngine;

namespace GD3
{
	public class CompReceiverSelect : ThingComp
	{
		public CompProperties_ReceiverSelect Props
		{
			get
			{
				return this.props as CompProperties_ReceiverSelect;
			}
		}

		public int Mark
        {
            get
            {
				return this.markReceiver;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			bool flag = this.parent.Faction == Faction.OfPlayer;
			if (flag)
			{
				Command_Action selectType = new Command_Action
				{
					defaultLabel = "ReceiverSelectLabel".Translate(this.markReceiver == 3 ? "GD.markD".Translate() : (this.markReceiver == 2 ? "GD.markC".Translate() : this.markReceiver == 1 ? "GD.markB".Translate() : "GD.markA".Translate())),
					defaultDesc = "ReceiverSelectDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("Buildings/GiantMechTurret/ShortWaveReceiver", true),
					action = delegate ()
					{
						this.markReceiver++;
						this.CheckMark();
					}
				};
				yield return selectType;
			}
			yield break;
		}


		private void CheckMark()
		{
			float proj;
			if (this.markReceiver > 3)
            {
				this.markReceiver = 0;
				return;
            }
			if (this.markReceiver == 3)
            {
				proj = Find.ResearchManager.GetProgress(GDDefOf.GD3_GiantCluster_Large);
				if (proj >= GDDefOf.GD3_GiantCluster_Large.baseCost)
				{
					return;
				}
				this.markReceiver = 0;
			}
			if (this.markReceiver == 2)
			{
				proj = Find.ResearchManager.GetProgress(GDDefOf.GD3_GiantCluster_Medium);
				if (proj >= GDDefOf.GD3_GiantCluster_Medium.baseCost)
				{
					return;
				}
				this.markReceiver = 0;
			}
			if (this.markReceiver == 1)
			{
				proj = Find.ResearchManager.GetProgress(GDDefOf.GD3_GiantCluster_Small);
				if (proj >= GDDefOf.GD3_GiantCluster_Small.baseCost)
				{
					return;
				}
				this.markReceiver = 0;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.markReceiver, "mark", 0, false);
		}

		private int markReceiver;
	}
}