using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
    public class IsFlyingUnit : DefModExtension
    {
        public float flyingHeight = 2.0f;

        public bool flightDeterminedByCode = false;
    }
}
