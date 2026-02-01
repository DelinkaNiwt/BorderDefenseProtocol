using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GD3
{
	public class ThinkNode_ConditionalThingDef : ThinkNode_Conditional
	{
		public ThingDef thingDef;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			ThinkNode_ConditionalThingDef obj = (ThinkNode_ConditionalThingDef)base.DeepCopy(resolve);
			obj.thingDef = thingDef;
			return obj;
		}

		protected override bool Satisfied(Pawn pawn)
		{
			return pawn.def == thingDef;
		}
	}

}
