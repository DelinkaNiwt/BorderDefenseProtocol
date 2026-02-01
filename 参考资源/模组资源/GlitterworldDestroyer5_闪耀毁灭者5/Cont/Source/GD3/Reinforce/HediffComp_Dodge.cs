using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace GD3
{
    public class HediffCompProperties_Dodge : HediffCompProperties
    {
        public HediffCompProperties_Dodge()
        {
            this.compClass = typeof(HediffComp_Dodge);
        }

        public float DodgeChanceFacingMechanoids;
    }

    public class HediffComp_Dodge : HediffComp
    {
        public HediffCompProperties_Dodge Props
        {
            get
            {
                return (HediffCompProperties_Dodge)this.props;
            }
        }
    }
}