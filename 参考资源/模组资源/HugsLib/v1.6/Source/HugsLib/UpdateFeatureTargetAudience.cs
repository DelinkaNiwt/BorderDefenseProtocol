using System;

namespace HugsLib;

[Flags]
public enum UpdateFeatureTargetAudience
{
	ReturningPlayers = 1,
	NewPlayers = 2,
	AllPlayers = 3
}
