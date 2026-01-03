using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompEmptyUniqueWeapon : CompUniqueWeapon
{
	public string gizmoLabel1 = "Ancot.AddToFavorites".Translate();

	public string gizmoLabel2 = "Ancot.RemoveFromFavorites".Translate();

	public string gizmoDesc1 = "Ancot.AddToFavorites_Desc".Translate();

	public string gizmoDesc2 = "Ancot.RemoveFromFavorites_Desc".Translate();

	public CompProperties_EmptyUniqueWeapon Traits_Props => (CompProperties_EmptyUniqueWeapon)props;

	public bool IsStarWeapon => parent.IsWeaponStar();

	public override void PostPostMake()
	{
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		foreach (StatDrawEntry item in base.SpecialDisplayStats())
		{
			yield return item;
		}
		yield return new StatDrawEntry(parent.def.IsMeleeWeapon ? StatCategoryDefOf.Weapon_Melee : StatCategoryDefOf.Weapon_Ranged, "Ancot.MaxFittingAmounts".Translate(), Traits_Props.max_traits.ToString(), "Ancot.MaxFittingAmounts_Desc".Translate(), 1103);
	}

	public virtual IEnumerable<Gizmo> GetWeaponGizmos()
	{
		yield return new Command_Action
		{
			Order = Traits_Props.gizmoOrder,
			defaultLabel = (IsStarWeapon ? gizmoLabel2 : gizmoLabel1),
			defaultDesc = (IsStarWeapon ? gizmoDesc2 : gizmoDesc1),
			icon = (IsStarWeapon ? AncotLibraryIcon.StarOn : AncotLibraryIcon.StarOff),
			action = delegate
			{
				GameComponent_AncotLibrary gC = GameComponent_AncotLibrary.GC;
				if (IsStarWeapon)
				{
					gC.StarWeapon.Remove(parent);
				}
				else
				{
					gC.StarWeapon.Add(parent);
				}
			}
		};
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		foreach (Gizmo weaponGizmo in GetWeaponGizmos())
		{
			yield return weaponGizmo;
		}
	}
}
