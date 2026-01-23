using System;
using System.Collections.Generic;
using Verse;

namespace HugsLib.Utils;

/// <summary>
/// A performance-friendly way to execute code at arbitrary tick intervals.
/// Optimized for one-off timed callbacks with variable callback delay. 
/// Use DistributedTickScheduler instead if you have many recipients with recurring callbacks and constant time.
/// Callbacks are called at tick time, which means a game must be loaded and running for them to be processed.
/// </summary>
public class TickDelayScheduler
{
	public class SchedulerEntry
	{
		public readonly Action callback;

		public readonly int interval;

		public readonly bool repeat;

		public readonly Thing owner;

		public int dueAtTick;

		public SchedulerEntry(Action callback, int interval, Thing owner, int dueAtTick, bool repeat)
		{
			this.callback = callback;
			this.interval = interval;
			this.owner = owner;
			this.repeat = repeat;
			this.dueAtTick = dueAtTick;
		}
	}

	private readonly LinkedList<SchedulerEntry> entries = new LinkedList<SchedulerEntry>();

	private int lastProcessedTick = -1;

	internal TickDelayScheduler()
	{
	}

	internal void Initialize(int currentTick)
	{
		lastProcessedTick = currentTick;
		entries.Clear();
	}

	internal void Tick(int currentTick)
	{
		if (lastProcessedTick < 0)
		{
			throw new Exception("Ticking not initialized TickDelayScheduler");
		}
		lastProcessedTick = currentTick;
		while (entries.First != null)
		{
			SchedulerEntry value = entries.First.Value;
			if (value.dueAtTick > currentTick)
			{
				break;
			}
			entries.RemoveFirst();
			bool flag = DoCallback(value);
			if (value.repeat && flag)
			{
				value.dueAtTick = currentTick + value.interval;
				ScheduleEntry(value);
			}
		}
	}

	/// <summary>
	/// Registers a delegate to be called in a given number of ticks.
	/// </summary>
	/// <param name="callback">The delegate to be called</param>
	/// <param name="dueInTicks">The delay in ticks before the delegate is called</param>
	/// <param name="owner">Optional owner of the delegate. Callback will not fire if the Thing is not spawned at call time.</param>
	/// <param name="repeat">If true, the callback will be rescheduled after each call until manually unscheduled</param>
	public void ScheduleCallback(Action callback, int dueInTicks, Thing owner = null, bool repeat = false)
	{
		if (lastProcessedTick < 0)
		{
			throw new Exception("Adding callback to not initialized TickDelayScheduler");
		}
		if (callback == null)
		{
			throw new NullReferenceException("callback cannot be null");
		}
		if (dueInTicks < 0)
		{
			throw new Exception("invalid dueInTicks value: " + dueInTicks);
		}
		if (dueInTicks == 0 && repeat)
		{
			throw new Exception("Cannot schedule repeating callback with 0 delay");
		}
		SchedulerEntry schedulerEntry = new SchedulerEntry(callback, dueInTicks, owner, lastProcessedTick + dueInTicks, repeat);
		if (dueInTicks == 0)
		{
			DoCallback(schedulerEntry);
		}
		else
		{
			ScheduleEntry(schedulerEntry);
		}
	}

	/// <summary>
	/// Manually remove a callback to abort a delay or clear a recurring callback.
	/// Silently fails if the callback is not found.
	/// </summary>
	/// <param name="callback">The scheduled callback</param>
	public void TryUnscheduleCallback(Action callback)
	{
		for (LinkedListNode<SchedulerEntry> linkedListNode = entries.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
		{
			if (linkedListNode.Value.callback == callback)
			{
				entries.Remove(linkedListNode);
				break;
			}
		}
	}

	/// <summary>
	/// Only for debug purposes
	/// </summary>
	public IEnumerable<SchedulerEntry> GetAllPendingCallbacks()
	{
		return entries;
	}

	private bool DoCallback(SchedulerEntry entry)
	{
		if (entry.owner == null || entry.owner.Spawned)
		{
			try
			{
				entry.callback();
				return true;
			}
			catch (Exception ex)
			{
				HugsLibController.Logger.Error("TickDelayScheduler caught an exception while calling {0} registered by {1}: {2}", HugsLibUtility.DescribeDelegate(entry.callback), (entry.owner == null) ? "[null]" : entry.owner.ToString(), ex);
			}
		}
		return false;
	}

	private void ScheduleEntry(SchedulerEntry newEntry)
	{
		for (LinkedListNode<SchedulerEntry> linkedListNode = entries.Last; linkedListNode != null; linkedListNode = linkedListNode.Previous)
		{
			if (linkedListNode.Value.dueAtTick <= newEntry.dueAtTick)
			{
				entries.AddAfter(linkedListNode, newEntry);
				return;
			}
		}
		entries.AddFirst(newEntry);
	}
}
