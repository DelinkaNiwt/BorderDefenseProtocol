using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class PsychicComet : ShellRandomAngle
    {
        protected override void Tick()
        {
            base.Tick();
            if (ticksToImpact == def.skyfaller.anticipationSoundTicks)
            {

            }
        }
    }
}
