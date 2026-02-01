using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;
using System.Linq;

namespace GD3
{
	public class CompProperties_ChangeWeaponB : CompProperties
	{
		public CompProperties_ChangeWeaponB()
		{
			this.compClass = typeof(CompChangeWeaponB);
		}

		public string toggleLabelKey;

		public string toggleDescKey;

		public string toggleIconPath;
	}

	public class CompChangeWeaponB : ThingComp
	{
		public CompProperties_ChangeWeaponB Props
		{
			get
			{
				return (CompProperties_ChangeWeaponB)this.props;
			}
		}

		/*public override void CompTick()
		{
			base.CompTick();
			Pawn pawn = this.parent as Pawn;
			bool flag = pawn != null && pawn.Spawned && !pawn.Downed;
			if (flag)
			{
				
			}
		}*/

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Pawn pawn = this.parent as Pawn;
			bool flag0 = pawn.Faction == Faction.OfPlayer;
			if (flag0)
			{
				Command_Action changeButton = new Command_Action
				{
					defaultLabel = this.Props.toggleLabelKey.Translate(),
					defaultDesc = this.Props.toggleDescKey.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					action = delegate ()
					{
						bool flag = !pawn.Dead && !pawn.Downed && !pawn.mindState.mentalStateHandler.InMentalState && pawn.Faction == Faction.OfPlayer && pawn.GetOverseer() != null;
						if (flag)
						{
							bool flag2 = pawn.equipment?.Primary != null;
							if (flag2)
							{
								bool flag3 = pawn.equipment?.Primary.def == GDDefOf.CataphractCentipede_SR;
								pawn.equipment.Remove(pawn.equipment.Primary);
								if (flag3)
								{
									ThingWithComps equip = (ThingWithComps)ThingMaker.MakeThing(GDDefOf.CataphractCentipede_FY, null);
									pawn.equipment.AddEquipment(equip);
								}
								else
								{
									ThingWithComps equip = (ThingWithComps)ThingMaker.MakeThing(GDDefOf.CataphractCentipede_SR, null);
									pawn.equipment.AddEquipment(equip);
								}
							}
							GDDefOf.Interact_ChargeRifle.PlayOneShot(new TargetInfo(pawn.PositionHeld, pawn.MapHeld, false));
						}
					},
					Disabled = pawn.Dead || pawn.Downed || pawn.mindState.mentalStateHandler.InMentalState || pawn.Faction != Faction.OfPlayer || pawn.GetOverseer() == null,
				};
				yield return changeButton;
			}
			yield break;
		}
	}
}
