using System;
using System.Collections.Generic;
using Verse;

namespace VanillaPsycastsExpanded.UI;

public class Command_ActionWithFloat : Command_Action
{
	public Func<IEnumerable<FloatMenuOption>> floatMenuGetter;

	public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions => floatMenuGetter?.Invoke();
}
