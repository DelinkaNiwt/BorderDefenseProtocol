using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
    public class Ext_Arc : DefModExtension
    {
        public float range;

        public IntRange chainCount;

        public float amount;

        public float penetration;

        public DamageDef damage;

        public int EMPdamage;

        public SoundDef sound;
    }
}
