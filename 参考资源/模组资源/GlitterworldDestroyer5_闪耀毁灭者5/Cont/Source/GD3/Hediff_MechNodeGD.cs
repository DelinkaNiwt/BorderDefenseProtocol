using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GD3
{
	// Token: 0x02000340 RID: 832
	public class Hediff_MechNode : Hediff
	{
		// Token: 0x17000466 RID: 1126
		// (get) Token: 0x06001631 RID: 5681 RVA: 0x0008318A File Offset: 0x0008138A
		public int AdditionalBandwidth
		{
			get
			{
				return this.cachedTunedBandNodesCount;
			}
		}

		// Token: 0x17000467 RID: 1127
		// (get) Token: 0x06001632 RID: 5682 RVA: 0x00083192 File Offset: 0x00081392
		public override bool ShouldRemove
		{
			get
			{
				return this.cachedTunedBandNodesCount == 0;
			}
		}

		// Token: 0x17000468 RID: 1128
		// (get) Token: 0x06001633 RID: 5683 RVA: 0x000831A0 File Offset: 0x000813A0
		public override HediffStage CurStage
		{
			get
			{
				if (this.curStage == null && this.cachedTunedBandNodesCount > 0)
				{
					StatModifier statModifier = new StatModifier();
					statModifier.stat = StatDefOf.MechBandwidth;
					statModifier.value = (float)this.cachedTunedBandNodesCount;
					this.curStage = new HediffStage();
					this.curStage.statOffsets = new List<StatModifier>
					{
						statModifier
					};
				}
				return this.curStage;
			}
		}

		// Token: 0x06001634 RID: 5684 RVA: 0x00083204 File Offset: 0x00081404
		public override void PostTick()
		{
			base.PostTick();
			if (this.pawn.IsHashIntervalTick(60))
			{
				this.RecacheBandNodes();
			}
		}

		// Token: 0x06001635 RID: 5685 RVA: 0x00083221 File Offset: 0x00081421
		public override void PostAdd(DamageInfo? dinfo)
		{
			base.PostAdd(dinfo);
			this.RecacheBandNodes();
		}

		// Token: 0x06001636 RID: 5686 RVA: 0x00083230 File Offset: 0x00081430
		public void RecacheBandNodes()
		{
			int num = this.cachedTunedBandNodesCount;
			this.cachedTunedBandNodesCount = 0;
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				foreach (Building thing in maps[i].listerBuildings.AllBuildingsColonistOfDef(GDDefOf.GD_BandNode))
				{
					if (thing.TryGetComp<CompBandNode>().tunedTo == this.pawn && thing.TryGetComp<CompPowerTrader>().PowerOn)
					{
						this.cachedTunedBandNodesCount+=GDSettings.AdvancedNodeCount;
					}
				}
			}
			if (num != this.cachedTunedBandNodesCount)
			{
				this.curStage = null;
				Pawn_MechanitorTracker mechanitor = this.pawn.mechanitor;
				if (mechanitor == null)
				{
					return;
				}
				mechanitor.Notify_BandwidthChanged();
			}
		}

		// Token: 0x06001637 RID: 5687 RVA: 0x00083304 File Offset: 0x00081504
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref this.cachedTunedBandNodesCount, "cachedTunedBandNodesCount", 0, false);
		}

		// Token: 0x04001185 RID: 4485
		private const int BandNodeCheckInterval = 60;

		// Token: 0x04001186 RID: 4486
		private int cachedTunedBandNodesCount;

		// Token: 0x04001187 RID: 4487
		private HediffStage curStage;
	}
}
