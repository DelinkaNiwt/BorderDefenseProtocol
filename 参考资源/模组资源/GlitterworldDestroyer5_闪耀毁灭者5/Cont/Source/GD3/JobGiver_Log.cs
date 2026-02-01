using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GD3
{
    public class JobGiver_Log : ThinkNode_JobGiver
    {
        protected string log;

        protected override Job TryGiveJob(Pawn pawn)
        {
            Log.Message(log);
            return null;
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_Log obj = (JobGiver_Log)base.DeepCopy(resolve);
            obj.log = log;
            return obj;
        }
    }
}
