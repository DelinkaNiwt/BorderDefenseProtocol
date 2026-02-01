using System;
using Verse;

namespace GD3
{
    public class HediffCompProperties_Disappear : HediffCompProperties
    {
        public HediffCompProperties_Disappear()
        {
            this.compClass = typeof(HediffCompDisappear);
        }

        public bool check = false;
    }
}
