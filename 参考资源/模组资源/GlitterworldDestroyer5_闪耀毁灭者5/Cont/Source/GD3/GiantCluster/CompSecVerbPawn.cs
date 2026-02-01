using System;
using System.Linq;
using System.Collections.Generic;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using Verse;

namespace GD3
{
	public class CompSecVerbPawn : ThingComp
	{
		public CompProperties_SecVerb_Pawn Props
		{
			get
			{
				return (CompProperties_SecVerb_Pawn)this.props;
			}
		}

		public bool IsSecondaryVerbSelected
		{
			get
			{
				return this.isSecondaryVerbSelected;
			}
		}

		private ThingWithComps Equip
        {
            get
            {
				if (this.CasterPawn.equipment.Primary != null)
                {
					return this.CasterPawn.equipment.Primary;
                }
				return null;
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
				this.compEquippableInt = this.Equip.TryGetComp<CompEquippable>();
				return this.compEquippableInt;
			}
		}

		public Pawn CasterPawn
		{
			get
			{
				return this.parent as Pawn;
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Pawn pawn = this.CasterPawn;
			bool flag = pawn.Faction == Faction.OfPlayer;
			if (flag)
            {
				Command_Action toggle = new Command_Action
				{
					action = delegate ()
					{
						this.isSecondaryVerbSelected = !this.isSecondaryVerbSelected;
						this.SwitchVerb();
					},
					Disabled = pawn.Dead || pawn.Downed || pawn.mindState.mentalStateHandler.InMentalState || pawn.Faction != Faction.OfPlayer || pawn.GetOverseer() == null,
					defaultLabel = this.Props.toggleLabelKey.Translate() + (this.IsSecondaryVerbSelected ? this.Props.fireModeB_Desc.Translate() : this.Props.fireModeA_Desc.Translate()),
					defaultDesc = this.Props.toggleDescKey.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, false)
				};
				yield return toggle;
			}
			yield break;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref this.isSecondaryVerbSelected, "useSecondaryVerb", false, false);
		}

		private void SwitchVerb()
		{
			GDDefOf.Interact_ChargeRifle.PlayOneShot(new TargetInfo(this.CasterPawn.PositionHeld, this.CasterPawn.MapHeld, false));
			if (this.IsSecondaryVerbSelected)
			{
				this.EquipmentSource.PrimaryVerb.verbProps = this.Props.verbProps;
			}
            else
            {
				this.EquipmentSource.PrimaryVerb.verbProps = this.Equip.def.Verbs[0];
			}
		}

		private CompEquippable compEquippableInt;

		private bool isSecondaryVerbSelected = false;
	}
}