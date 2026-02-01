using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
	public class CompUseEffect_Key : CompUseEffect
	{
        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            AcceptanceReport result = base.CanBeUsedBy(p);
			if (Find.World.GetComponent<MissionComponent>().keyGained)
            {
				result = "GD.HasKey".Translate();
            }
			return result;
        }

        public override void DoEffect(Pawn user)
		{
			base.DoEffect(user);
			Find.World.GetComponent<MissionComponent>().keyGained = true;
			Messages.Message("GD.KeyGained".Translate(), MessageTypeDefOf.PositiveEvent);
			SoundDefOf.MechChargerStart.PlayOneShot(new TargetInfo(user));
			this.parent.Destroy(DestroyMode.Vanish);
		}
	}
}
