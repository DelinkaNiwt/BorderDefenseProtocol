using UnityEngine;
using Verse;

namespace AlienRace;

public class LifeStageAgeAlien : LifeStageAge
{
	public BodyDef body;

	public Vector2 headOffset = Vector2.zero;

	public AlienPartGenerator.DirectionalOffset headOffsetDirectional;

	public Vector2 headFemaleOffset = Vector2.negativeInfinity;

	public AlienPartGenerator.DirectionalOffset headFemaleOffsetDirectional;

	public Vector2 customDrawSize = Vector2.zero;

	public Vector2 customPortraitDrawSize = Vector2.zero;

	public Vector2 customHeadDrawSize = Vector2.zero;

	public Vector2 customPortraitHeadDrawSize = Vector2.zero;

	public Vector2 customFemaleDrawSize = Vector2.zero;

	public Vector2 customFemalePortraitDrawSize = Vector2.zero;

	public Vector2 customFemaleHeadDrawSize = Vector2.zero;

	public Vector2 customFemalePortraitHeadDrawSize = Vector2.zero;
}
