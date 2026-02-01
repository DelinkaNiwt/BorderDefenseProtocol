using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
    public class CompProperties_AttackMode : CompProperties
    {
        public CompProperties_AttackMode()
        {
            this.compClass = typeof(CompAttackMode);
        }

        public string label;

        public string description;

        public string UI;
    }

    public class CompAttackMode : ThingComp
    {
        public CompProperties_AttackMode Props
        {
            get
            {
                return (CompProperties_AttackMode)this.props;
            }
        }

        private CompMechCarrier comp;

        public CompMechCarrier Comp => comp ?? (comp = parent.TryGetComp<CompMechCarrier>());

        public bool mode = false;

        public override void CompTick()
        {
            if (mode && Comp.CanSpawn)
            {
                if (((Pawn)parent).IsHashIntervalTick(50))
                {
                    Comp.TrySpawnPawns();
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn;
            if ((pawn = (parent as Pawn)) == null || !pawn.IsColonyMech || pawn.GetOverseer() == null)
            {
                yield break;
            }

            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }

            Command_Toggle act = new Command_Toggle
            {
                toggleAction = delegate ()
                {
                    mode = !mode;
                },
                isActive = (() => mode),
                icon = ContentFinder<Texture2D>.Get(Props.UI),
                defaultLabel = Props.label.Translate(mode ? "On".Translate() : "Off".Translate()),
                defaultDesc = Props.description.Translate(parent.Label)
            };
            yield return act;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref mode, "mode");
        }
    }
}
