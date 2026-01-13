using Verse;

namespace FreeElectronOrbitalLaser;

public class CompProperties_FreeElectronOrbitalLaser : CompProperties
{
	public int cooldownSeconds = 360;

	public int durationTicks = 600;

	public int lavaPoolSize = 75;

	public int lavaExpandIntervalTicks = 1;

	public int lavaExpandCellsPerInterval = 1;

	public IntRange lavaCoolDelay = new IntRange(48000, 72000);

	public ThingDef lavaThingDef;

	public ThingDef beamThingDef;

	public int ticksPerCell = 20;

	public ResearchProjectDef requiredResearch;

	public string disabledReasonKey;

	public CompProperties_FreeElectronOrbitalLaser()
	{
		compClass = typeof(Comp_FreeElectronOrbitalLaser);
	}
}
