using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Mote_ScaleAndRotate : Mote
{
	public float iniscale;

	public float currentscale;

	public int tickimpact;

	public int tickspawned;

	private int lastMaintainTick;

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Graphic.Draw(drawLoc, base.Rotation, this, exactRotation);
	}

	protected override void TimeInterval(float deltaTime)
	{
		if (EndOfLife && !base.Destroyed)
		{
			Destroy();
			return;
		}
		if (def.mote.needsMaintenance && Find.TickManager.TicksGame - 1 > lastMaintainTick)
		{
			int num = def.mote.fadeOutTime.SecondsToTicks();
			if (!def.mote.fadeOutUnmaintained || Find.TickManager.TicksGame - lastMaintainTick > num)
			{
				Destroy();
				return;
			}
		}
		if (def.mote.scalers != null)
		{
			curvedScale = def.mote.scalers.ScaleAtTime(base.AgeSecs);
		}
	}

	public void MaintainMote()
	{
		lastMaintainTick = Find.TickManager.TicksGame;
	}

	protected override void Tick()
	{
		base.Tick();
		exactRotation = (float)Find.TickManager.TicksGame % 360f;
		if (Mathf.Abs(tickimpact - tickspawned) > 0)
		{
			currentscale = iniscale * ((float)(Find.TickManager.TicksGame - tickspawned) / (float)(tickimpact - tickspawned) * 0.5f + 1f);
			linearScale = new Vector3(currentscale, currentscale, currentscale);
			Graphic.drawSize = linearScale;
		}
		if (!link1.Linked)
		{
			return;
		}
		bool flag = detachAfterTicks == -1 || Find.TickManager.TicksGame - spawnTick < detachAfterTicks;
		if (!link1.Target.ThingDestroyed && flag)
		{
			link1.UpdateDrawPos();
			if (link1.rotateWithTarget)
			{
				base.Rotation = link1.Target.Thing.Rotation;
			}
		}
		Vector3 attachedDrawOffset = def.mote.attachedDrawOffset;
		exactPosition = link1.LastDrawPos + attachedDrawOffset;
		IntVec3 intVec = exactPosition.ToIntVec3();
		if (base.Spawned && !intVec.InBounds(base.Map))
		{
			Destroy();
		}
		else
		{
			base.Position = intVec;
		}
	}
}
