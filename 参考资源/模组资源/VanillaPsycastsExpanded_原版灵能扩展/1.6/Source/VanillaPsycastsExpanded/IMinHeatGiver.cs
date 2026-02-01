using Verse;

namespace VanillaPsycastsExpanded;

public interface IMinHeatGiver : ILoadReferenceable
{
	bool IsActive { get; }

	int MinHeat { get; }
}
