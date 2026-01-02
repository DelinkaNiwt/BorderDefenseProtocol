using System.Collections.Generic;
using System.Linq;
using AlienRace.ExtendedGraphics;
using UnityEngine;
using Verse;

namespace AlienRace;

public class AnimalBodyAddons : DefModExtension
{
	public List<AlienPartGenerator.BodyAddon> bodyAddons = new List<AlienPartGenerator.BodyAddon>();

	public List<AlienPartGenerator.OffsetNamed> offsetDefaults = new List<AlienPartGenerator.OffsetNamed>();

	public void GenerateAddonData(ThingDef def)
	{
		offsetDefaults.Add(new AlienPartGenerator.OffsetNamed
		{
			name = "Center",
			offsets = new AlienPartGenerator.DirectionalOffset()
		});
		offsetDefaults.Add(new AlienPartGenerator.OffsetNamed
		{
			name = "Tail",
			offsets = new AlienPartGenerator.DirectionalOffset
			{
				south = new AlienPartGenerator.RotationOffset
				{
					offset = new Vector2(0f, -0.15f)
				},
				north = new AlienPartGenerator.RotationOffset
				{
					offset = new Vector2(0f, 0f)
				},
				east = new AlienPartGenerator.RotationOffset
				{
					offset = new Vector2(0.42f, -0.15f)
				},
				west = new AlienPartGenerator.RotationOffset
				{
					offset = new Vector2(0.42f, -0.15f)
				}
			}
		});
		offsetDefaults.Add(new AlienPartGenerator.OffsetNamed
		{
			name = "Head",
			offsets = new AlienPartGenerator.DirectionalOffset
			{
				south = new AlienPartGenerator.RotationOffset
				{
					offset = new Vector2(0f, 0.35f)
				},
				north = new AlienPartGenerator.RotationOffset
				{
					offset = new Vector2(0f, 0.5f)
				},
				east = new AlienPartGenerator.RotationOffset
				{
					offset = new Vector2(-0.47f, 0.35f)
				},
				west = new AlienPartGenerator.RotationOffset
				{
					offset = new Vector2(-0.47f, 0.35f)
				}
			}
		});
		new DefaultGraphicsLoader().LoadAllGraphics(def.defName + " Animal Addons", bodyAddons.Cast<AlienPartGenerator.ExtendedGraphicTop>().ToArray());
	}
}
