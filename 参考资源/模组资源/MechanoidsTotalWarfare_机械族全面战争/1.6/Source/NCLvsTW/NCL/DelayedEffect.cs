using System;

namespace NCL;

public struct DelayedEffect
{
	public Action Action;

	public int DelayTicks;

	public int StartTick;
}
