using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GD3
{
    public class BlackMechTradeDef : Def
    {
        public Type workerClass;

        public int intelligenceCost = 1000;

        public ThingDef thing;

        public int count;

        public float rectHeight = 25f;

        public int order = 100;

        [Unsaved(false)]
        private BlackMechTradeWorker workerInt;

        public BlackMechTradeWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (BlackMechTradeWorker)Activator.CreateInstance(workerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }
    }
}
