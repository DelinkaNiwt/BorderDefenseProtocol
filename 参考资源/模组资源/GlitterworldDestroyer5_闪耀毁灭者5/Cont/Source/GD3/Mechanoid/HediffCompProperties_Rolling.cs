using System;
using Verse;

namespace GD3
{
    public class HediffCompProperties_Rolling : HediffCompProperties
    {
        public HediffCompProperties_Rolling()
        {
            this.compClass = typeof(HediffComp_Rolling);
        }

        public int interval;

        public float damage;

        public float penetration;

        public float maxBodySize;

        public ResearchProjectDef requireTech;
    }
}
