using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCLProjectiles;

public class ProjectileStagingTracker : IExposable
{
	private readonly Thing parent;

	public List<ProjectileFlightStage> stages;

	public ProjectileFlightStage stage;

	public ProjectileStageConfiguration stageConfig;

	public int totalFlightDuration;

	public bool impacted;

	public ProjectileStagingTracker(Thing parent)
	{
		this.parent = parent;
	}

	public void PostSpawnSetup(Map map, bool respawningAfterLoad, ModExtension_ProjectileEffects extension, int ticksSinceLaunch)
	{
		if (stages != null)
		{
			int stageIndex = GetStageIndex(ticksSinceLaunch);
			if (stageIndex > -1)
			{
				stageConfig = extension.stages[stageIndex];
				stage = stages[stageIndex];
			}
		}
	}

	public int PostLaunch(ModExtension_ProjectileEffects extension, Vector3 origin, Vector3 destination)
	{
		InitializeStages(extension, origin, destination);
		return totalFlightDuration;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref totalFlightDuration, "totalFlightDuration", 0);
		Scribe_Collections.Look(ref stages, "stages", LookMode.Undefined);
	}

	public int GetStageIndex(int tick = 0)
	{
		if (stages != null)
		{
			for (int i = 0; i < stages.Count; i++)
			{
				if (stages[i].duration < 0)
				{
					return i;
				}
				if (tick < stages[i].duration)
				{
					return i;
				}
				tick -= stages[i].duration;
			}
		}
		return -1;
	}

	public void PreTick(ModExtension_ProjectileEffects extension, ProjectileEffectTracker effectTracker, int ticksSinceLaunch, bool activeTracking = false)
	{
		int num = ticksSinceLaunch - stage.startingTick;
		if (stage.duration > -1 && num > stage.duration)
		{
			int stageIndex = GetStageIndex(ticksSinceLaunch);
			if (stageIndex > -1)
			{
				Vector3 destination = stage.destination;
				stageConfig = extension.stages[stageIndex];
				stage = stages[stageIndex];
				stageConfig.startSound?.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
				if (activeTracking && destination != default(Vector3))
				{
					stage.origin = destination;
				}
			}
		}
		if (activeTracking && stageConfig != null && (stageConfig.type == ProjectileStageType.Cruise || stageConfig.type == ProjectileStageType.Terminal))
		{
			stage.destination = effectTracker.destination + ProjectileUtility.CalculateStagePositionOffset(stageConfig, stage.origin, effectTracker.destination);
		}
	}

	public void Tick(Map map, ModExtension_ProjectileEffects extension)
	{
	}

	private void InitializeStages(ModExtension_ProjectileEffects extension, Vector3 origin, Vector3 destination)
	{
		Vector3 vector = origin;
		float startingHeight = 0f;
		stages = new List<ProjectileFlightStage>(extension.stages.Count);
		for (int i = 0; i < extension.stages.Count; i++)
		{
			ProjectileStageConfiguration projectileStageConfiguration = extension.stages[i];
			Vector3 vector2;
			float num;
			switch (projectileStageConfiguration.type)
			{
			case ProjectileStageType.Launch:
				vector2 = vector;
				vector2 += ProjectileUtility.CalculateStagePositionOffset(projectileStageConfiguration, vector, destination);
				startingHeight = projectileStageConfiguration.initialHeight;
				num = projectileStageConfiguration.heightOffset;
				break;
			case ProjectileStageType.Cruise:
				vector2 = destination + ProjectileUtility.CalculateStagePositionOffset(projectileStageConfiguration, vector, destination);
				num = projectileStageConfiguration.heightOffset;
				break;
			default:
				vector2 = destination;
				num = 0f;
				break;
			}
			int num2 = ((projectileStageConfiguration.duration <= -1) ? Mathf.CeilToInt((vector2 - vector).MagnitudeHorizontal() / projectileStageConfiguration.TilesPerTick) : projectileStageConfiguration.duration);
			stages.Add(new ProjectileFlightStage
			{
				origin = vector,
				destination = vector2,
				startingTick = totalFlightDuration,
				startingHeight = startingHeight,
				endingHeight = num,
				distance = (vector2 - vector).MagnitudeHorizontal(),
				duration = num2
			});
			totalFlightDuration += num2;
			vector = vector2;
			startingHeight = num;
		}
		stageConfig = extension.stages[0];
		stage = stages[0];
		stageConfig.startSound?.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
	}
}
