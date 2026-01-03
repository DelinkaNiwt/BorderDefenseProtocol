using Verse;

namespace AncotLibrary;

public class LimitedStacksThingOwner<T> : ThingOwner<T> where T : Thing
{
	public LimitedStacksThingOwner(IThingHolder owner)
		: this(owner, 0, LookMode.Deep, removeContentsIfDestroyed: true)
	{
	}

	public LimitedStacksThingOwner(IThingHolder owner, int maxStacks, LookMode lookMode = LookMode.Deep, bool removeContentsIfDestroyed = true)
		: base(owner, oneStackOnly: false, lookMode, removeContentsIfDestroyed)
	{
		base.maxStacks = maxStacks;
	}
}
