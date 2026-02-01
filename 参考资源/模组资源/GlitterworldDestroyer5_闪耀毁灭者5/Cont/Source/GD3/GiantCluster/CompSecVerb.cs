using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GD3
{
	internal class CompSecVerb : ThingComp
	{
		public CompProperties_SecVerb Props
		{
			get
			{
				return (CompProperties_SecVerb)this.props;
			}
		}

		public bool IsSecondaryVerbSelected
		{
			get
			{
				return this.isSecondaryVerbSelected;
			}
		}

		private CompEquippable EquipmentSource
		{
			get
			{
				if (this.compEquippableInt != null)
				{
					return this.compEquippableInt;
				}
				this.compEquippableInt = this.parent.TryGetComp<CompEquippable>();
				if (this.compEquippableInt == null)
				{
					Log.ErrorOnce(this.parent.LabelCap + "has no CompEquippable", 50020);
				}
				return this.compEquippableInt;
			}
		}

		public Building CasterBuilding
		{
			get
			{
				return this.Verb.caster as Building;
			}
		}

		private Verb Verb
		{
			get
			{
				if (this.verbInt == null)
				{
					this.verbInt = this.EquipmentSource.PrimaryVerb;
				}
				return this.verbInt;
			}
		}

		public void VerbTick()
        {
			if (CasterBuilding == null || !CasterBuilding.Spawned || CasterBuilding.Map == null || CasterBuilding.Map.mapPawns == null)
            {
				return;
            }
			IEnumerable<Pawn> enumerable = from x in CasterBuilding.Map.mapPawns.AllPawnsSpawned
										   where x.Position.DistanceTo(CasterBuilding.Position) < this.Props.range && (x.Faction != null && x.HostileTo(CasterBuilding) && !x.Downed)
										   select x;
			if (enumerable.Count() > 0)
			{
				this.isSecondaryVerbSelected = true;
				this.SwitchVerb();
			}
			else
			{
				this.isSecondaryVerbSelected = false;
				this.SwitchVerb();
			}
        }

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref this.isSecondaryVerbSelected, "useSecondaryVerb", false, false);
		}

		private void SwitchVerb()
		{
			if (this.IsSecondaryVerbSelected)
			{
				this.EquipmentSource.PrimaryVerb.verbProps = this.Props.verbProps;
				return;
			}
			this.EquipmentSource.PrimaryVerb.verbProps = this.parent.def.Verbs[0];
		}

		private Verb verbInt;

		private CompEquippable compEquippableInt;

		private bool isSecondaryVerbSelected;
	}
}