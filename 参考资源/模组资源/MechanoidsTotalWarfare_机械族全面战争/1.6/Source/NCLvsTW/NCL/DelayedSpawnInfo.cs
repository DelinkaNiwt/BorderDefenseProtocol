using System.Collections.Generic;
using Verse;

namespace NCL;

public struct DelayedSpawnInfo
{
	public Pawn caster;

	public List<PawnKindDef> pawnKinds;

	public int count;

	public int spawnRadius;

	public int minDistance;

	public bool leaveSlag;

	public bool canRoofPunch;

	public int spawnTick;

	public DelayedSpawnInfo(Pawn caster, List<PawnKindDef> pawnKinds, int count, int spawnRadius, int minDistance, bool leaveSlag, bool canRoofPunch, int spawnTick)
	{
		this.caster = caster;
		this.pawnKinds = pawnKinds;
		this.count = count;
		this.spawnRadius = spawnRadius;
		this.minDistance = minDistance;
		this.leaveSlag = leaveSlag;
		this.canRoofPunch = canRoofPunch;
		this.spawnTick = spawnTick;
	}
}
