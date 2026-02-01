using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class CompProperties_EffecterWakeUp : CompProperties
	{
		public CompProperties_EffecterWakeUp()
		{
			this.compClass = typeof(CompEffecterWakeUp);
		}

		public EffecterDef effecter;

		public Vector3 offset;

		public float scale;
	}

	public class CompEffecterWakeUp : ThingComp
    {
		private Effecter attachedEffecter;

		public CompProperties_EffecterWakeUp Props
		{
			get
			{
				return (CompProperties_EffecterWakeUp)this.props;
			}
		}

		public bool ShouldShowEffecter()
		{
			if (parent.Spawned && parent.MapHeld == Find.CurrentMap)
			{
				CompCanBeDormant comp = parent.TryGetComp<CompCanBeDormant>();
				if (comp == null)
                {
					return true;
                }
				return comp.Awake;
			}
			return false;
		}

        public override void CompTick()
        {
            if (ShouldShowEffecter())
            {
                if (attachedEffecter == null)
                {
                    attachedEffecter = Props.effecter.SpawnAttached(parent, parent.MapHeld, Props.scale);
                    attachedEffecter.offset = Props.offset;
                }
                attachedEffecter?.EffectTick(parent, parent);
            }
            else
            {
                attachedEffecter?.Cleanup();
                attachedEffecter = null;
            }
        }
    }
}
