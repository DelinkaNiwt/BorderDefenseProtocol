using System;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using UnityEngine;

namespace GD3
{
	public class CompProperties_AllDead : CompProperties
	{
		public CompProperties_AllDead()
		{
			this.compClass = typeof(CompAllDead);
		}

		public string toggleLabelKey;

		public string toggleDescKey;

		public string toggleIconPath;

		public string unableKey;

		public ThingDef mechanoidToKill;
	}

	public class CompAllDead : ThingComp
	{
		public CompProperties_AllDead Props
		{
			get
			{
				return this.props as CompProperties_AllDead;
			}
		}

		public Pawn Owner
        {
            get
            {
				return this.parent as Pawn;
            }
        }

		public bool CanApply
        {
            get
            {
				return this.Owner != null && !this.pawns.NullOrEmpty();
            }
        }

        public override void CompTick()
        {
            base.CompTick();
			if (Owner == null || !Owner.Spawned || Owner.Map?.mapPawns == null)
            {
				return;
            }
			ticks++;
			if (ticks > 100)
            {
				ticks = 0;
				IEnumerable<Pawn> list = from x in Owner.Map.mapPawns.AllPawnsSpawned
										  where x.def == this.Props.mechanoidToKill && x.Faction != null && x.Faction == Owner.Faction
										  select x;
				pawns = list.ToList();
			}
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			bool flag = this.Owner.Faction != null && this.Owner.Faction == Faction.OfPlayer;
			if (flag)
			{
				Command_Action allDead = new Command_Action
				{
					defaultLabel = this.Props.toggleLabelKey.Translate(),
					defaultDesc = this.Props.toggleDescKey.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					action = delegate ()
					{
						this.KillAll();
					}
				};
				if (!this.CanApply)
				{
					allDead.Disable(this.Props.unableKey.Translate());
				}
				yield return allDead;
			}
			yield break;
		}

		public void KillAll()
        {
			MoteMaker.ThrowText(this.Owner.DrawPos, Owner.Map, "GD.ClearAllUrchins".Translate(string.Format("{0}", Owner.Name)), 5f);
			for (int i = 0; i < this.pawns.Count; i++)
            {
				Pawn p = pawns[i];
				if (p != null)
                {
					if (!p.Dead)
					{
						p.Kill(null, null);
					}
				}
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
			Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
		}

		public int ticks = 0;

		public List<Pawn> pawns;
    }
}