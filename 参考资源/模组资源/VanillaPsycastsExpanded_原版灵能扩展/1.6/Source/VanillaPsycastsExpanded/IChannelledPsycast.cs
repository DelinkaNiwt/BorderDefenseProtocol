using Verse;

namespace VanillaPsycastsExpanded;

public interface IChannelledPsycast : ILoadReferenceable
{
	bool IsActive { get; }
}
